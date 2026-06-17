using System.Buffers;
using System.IO.Pipelines;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usenet.Exceptions;
using Usenet.Extensions;
using Usenet.Nntp.Contracts;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;
using Usenet.Util;

namespace Usenet.Nntp;

/// <summary>
/// A standard implementation of an NNTP connection.
/// Based on Kristian Hellang's NntpLib.Net project https://github.com/khellang/NntpLib.Net.
/// </summary>
/// <remarks>This implementation of the <see cref="INntpConnection"/> interface does support SSL encryption but
/// does not support compressed multi-line results. The transport is built on
/// <see cref="System.IO.Pipelines"/>: lines are framed off the raw byte stream, dot-stuffing is undone and the
/// terminating dot is detected without transcoding the whole stream to <see cref="string"/>.</remarks>
[PublicAPI]
public sealed class NntpConnection : INntpConnection, INntpStreamSource, INntpCompressionControl
{
    private const int StackAllocThreshold = 256;
    private const int InitialDataBlockBufferSize = 4096;
    private const string AuthInfoPass = "AUTHINFO PASS";

    // The literal terminating dot line the server appends after the compressed payload in the
    // TERMINATOR variant, so the transport can find the block boundary without trusting the gzip
    // trailer. See ADR-0005.
    private static ReadOnlySpan<byte> CompressedBlockTerminator => "\r\n.\r\n"u8;

    // NNTP cannot tell the server to stop sending mid-data-block, so reclaiming a connection from a
    // partially-consumed streamed response means either draining to the terminating dot (paying the
    // remaining transfer) or abandoning the connection (paying a reconnect). On early-exit we drain
    // only while this little remains on the wire and otherwise abandon, so `break` stays cheap over
    // large ranges while a small remainder keeps the pooled connection. See ADR-0003.
    private const int StreamDrainBudgetBytes = 64 * 1024;

    private readonly ILogger _log;
    private readonly NntpConnectionOptions _options;
    private readonly TcpClient _client = new();
    private Stream? _stream;
    private PipeReader? _reader;
    private PipeWriter? _writer;
    private CountingBufferWriter? _output;
    private long _bytesRead;
    private long _bytesWritten;
    private bool _streaming;
    private bool _compressionActive;

    // While a compressed data block is in flight this holds an in-memory reader over the inflated
    // bytes; the data-block framing reads from it instead of the wire. Null in plain mode.
    private PipeReader? _dataReader;

    /// <summary>
    /// Creates a new instance of the <see cref="NntpConnection"/> class.
    /// </summary>
    /// <param name="options">
    /// The transport configuration (host, port, SSL, timeouts and compression) used when connecting.
    /// When <see langword="null"/>, a default <see cref="NntpConnectionOptions"/> is used.
    /// </param>
    /// <param name="loggerFactory">
    /// An optional <see cref="ILoggerFactory"/> used to create the connection's logger.
    /// When <see langword="null"/>, logging is disabled via <see cref="NullLoggerFactory"/>.
    /// </param>
    public NntpConnection(
        NntpConnectionOptions? options = null,
        ILoggerFactory? loggerFactory = null
    )
    {
        _options = options ?? new NntpConnectionOptions();
        _log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<NntpConnection>();
    }

    /// <inheritdoc/>
    public IBufferWriter<byte> Output
    {
        get
        {
            ThrowIfNotConnected();
            return _output!;
        }
    }

    /// <inheritdoc/>
    public long BytesRead => _bytesRead;

    /// <inheritdoc/>
    public long BytesWritten => _bytesWritten;

    /// <inheritdoc/>
    public void ResetCounters()
    {
        _bytesRead = 0;
        _bytesWritten = 0;
    }

    /// <inheritdoc/>
    public async Task<TResponse> ConnectAsync<TResponse>(
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken = default
    )
    {
        var hostname = _options.Host;
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);

        var port = _options.Port;
        var useSsl = _options.UseSsl;

        _log.Connecting(hostname, port, useSsl);
        await ConnectClientAsync(hostname, port, cancellationToken).ConfigureAwait(false);
        // Disable Nagle's algorithm: now that a command is sent as a single batched flush, the
        // final sub-MSS segment would otherwise stall ~40ms against the server's delayed ACK
        // before the response can be read. NNTP is a request/response protocol that wants the
        // bytes on the wire immediately.
        _client.NoDelay = true;
        _stream = await GetStreamAsync(hostname, useSsl).ConfigureAwait(false);
        _reader = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));
        _writer = PipeWriter.Create(_stream, new StreamPipeWriterOptions(leaveOpen: true));
        _output = new CountingBufferWriter(_writer, this);
        return await GetResponseAsync(parser, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Establishes the TCP connection, applying <see cref="NntpConnectionOptions.ConnectTimeout"/> when
    /// it is positive. A timeout that elapses before the caller's token is cancelled surfaces as a
    /// <see cref="TimeoutException"/>.
    /// </summary>
    private async Task ConnectClientAsync(
        string hostname,
        int port,
        CancellationToken cancellationToken
    )
    {
        var timeout = _options.ConnectTimeout;
        if (timeout <= TimeSpan.Zero)
        {
            await _client.ConnectAsync(hostname, port, cancellationToken).ConfigureAwait(false);
            return;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        try
        {
            await _client.ConnectAsync(hostname, port, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Timed out connecting to '{hostname}:{port}' after {timeout}."
            );
        }
    }

    /// <inheritdoc/>
    NntpCompression INntpCompressionControl.Compression => _options.Compression;

    /// <inheritdoc/>
    bool INntpCompressionControl.CompressionEnabled => _compressionActive;

    /// <inheritdoc/>
    async Task INntpCompressionControl.EnableCompressionAsync(CancellationToken cancellationToken)
    {
        var mode = _options.Compression;
        if (mode == NntpCompression.None || _compressionActive)
        {
            return;
        }

        var command =
            mode == NntpCompression.GzipWithTerminator
                ? "XFEATURE COMPRESS GZIP TERMINATOR"
                : "XFEATURE COMPRESS GZIP";

        // The status-line response stays clear text; only subsequent multi-line data blocks compress.
        var response = await CommandAsync(command, new ResponseParser(), cancellationToken)
            .ConfigureAwait(false);

        // XFEATURE responses are not standardized to a single code across providers, so accept any
        // 2xx. A rejection must throw rather than silently leave the framer expecting compressed
        // bytes the server will never send.
        if (response.Code is < 200 or >= 300)
        {
            throw new NntpException(
                $"Server rejected '{command}': {response.Code} {response.Message}"
            );
        }

        _compressionActive = true;
    }

    /// <inheritdoc/>
    public async Task<TResponse> CommandAsync<TResponse>(
        string command,
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfNotConnected();
        ThrowIfStreaming();
        ArgumentNullException.ThrowIfNull(command);

        var logCommand = command.StartsWith(AuthInfoPass, StringComparison.Ordinal)
            ? $"{AuthInfoPass} [REDACTED]"
            : command;
        _log.SendingCommand(logCommand);
        await WriteLineAsync(command, cancellationToken).ConfigureAwait(false);
        return await GetResponseAsync(parser, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<TResponse> MultiLineCommandAsync<TResponse>(
        string command,
        IMultiLineResponseParser<TResponse> parser,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(parser);

        var response = await CommandAsync(command, new ResponseParser(), cancellationToken)
            .ConfigureAwait(false);

        if (!parser.IsSuccessResponse(response.Code))
        {
            return parser.Parse(response.Code, response.Message, []);
        }

        if (_compressionActive)
        {
            await BeginCompressedDataBlockAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var dataBlock = await ReadMultiLineDataBlockAsync(cancellationToken)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return parser.Parse(response.Code, response.Message, dataBlock);
        }
        finally
        {
            EndCompressedDataBlock();
        }
    }

    /// <inheritdoc/>
    public async Task<NntpStreamResponse<T>> MultiLineStreamCommandAsync<T>(
        string command,
        int successCode,
        NntpStreamLineParser<T> lineParser,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(lineParser);

        var response = await CommandAsync(command, new ResponseParser(), cancellationToken)
            .ConfigureAwait(false);

        if (response.Code != successCode)
        {
            return new NntpStreamResponse<T>(response.Code, response.Message, lineParser);
        }

        // With compression on, the whole compressed payload is read off the wire and inflated up
        // front, then streamed per line from memory; the wire is clean once this returns. In plain
        // mode the data block stays on the wire until the caller enumerates or disposes the response.
        if (_compressionActive)
        {
            await BeginCompressedDataBlockAsync(cancellationToken).ConfigureAwait(false);
        }

        _streaming = true;
        return new NntpStreamResponse<T>(response.Code, response.Message, this, lineParser);
    }

    async ValueTask<string?> INntpStreamSource.ReadStreamLineAsync(
        CancellationToken cancellationToken
    )
    {
        if (!_streaming)
        {
            return null;
        }

        var line = await ReadLineAsync(cancellationToken).ConfigureAwait(false);
        var processed = ProcessLine(line);
        if (processed == null)
        {
            _streaming = false;
            EndCompressedDataBlock();
        }

        return processed;
    }

    async ValueTask INntpStreamSource.DrainStreamAsync(CancellationToken cancellationToken)
    {
        var budgetStart = _bytesRead;
        while (_streaming)
        {
            var line = await ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (ProcessLine(line) == null)
            {
                _streaming = false;
                EndCompressedDataBlock();
                return;
            }

            // Past the drain budget the remainder is too large to skip cheaply, so abandon the
            // connection instead of reading it all off the wire (ADR-0003).
            if (_bytesRead - budgetStart > StreamDrainBudgetBytes)
            {
                AbandonStream();
                return;
            }
        }
    }

    /// <summary>
    /// Tears down the transport for a streamed response whose remaining data block exceeded the drain
    /// budget. <see cref="_streaming"/> is left set so <see cref="HasPendingStream"/> tells the pool to
    /// discard this connection and establish a fresh one, and so a non-pooled connection cannot be
    /// reused with unread bytes still on the wire.
    /// </summary>
    private void AbandonStream() => Dispose();

    /// <summary>
    /// Indicates that a streamed multi-line data block is still on the wire and must be drained
    /// before the connection can serve another command.
    /// </summary>
    internal bool HasPendingStream => _streaming;

    /// <summary>
    /// The reader the data-block framing pulls from: the in-memory inflated reader while a compressed
    /// data block is in flight, otherwise the live wire reader. The clear-text status line is always
    /// read off the wire before a data block begins, so it never sees the inflated reader.
    /// </summary>
    private PipeReader DataBlockReader => _dataReader ?? _reader!;

    /// <summary>
    /// Reads the compressed payload of the current multi-line response off the wire, inflates it, and
    /// arms <see cref="_dataReader"/> so the existing line framing rides the decompressed bytes. The
    /// whole block is materialized: streaming per line off the wire is incompatible with an inflate
    /// stage scoped to the block on a persistent connection (ADR-0005).
    /// </summary>
    private async Task BeginCompressedDataBlockAsync(CancellationToken cancellationToken)
    {
        var (buffer, length) = await ReadCompressedPayloadAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var inflated = NntpCompressedBlock.Inflate(buffer, length);
            _dataReader = PipeReader.Create(new ReadOnlySequence<byte>(inflated));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Completes and clears the in-memory inflated reader once a compressed data block has been fully
    /// consumed. A no-op in plain mode.
    /// </summary>
    private void EndCompressedDataBlock()
    {
        if (_dataReader == null)
        {
            return;
        }

        _dataReader.Complete();
        _dataReader = null;
    }

    /// <summary>
    /// Reads the compressed payload of a multi-line data block off the wire into a pooled buffer. The
    /// <see cref="NntpCompression.GzipWithTerminator"/> variant reads up to the literal terminating dot
    /// line the server appends after the payload; the <see cref="NntpCompression.Gzip"/> variant relies
    /// on the gzip stream being self-delimiting and reads until the wire reports completion.
    /// </summary>
    private async Task<(byte[] Buffer, int Length)> ReadCompressedPayloadAsync(
        CancellationToken cancellationToken
    )
    {
        var withTerminator = _options.Compression == NntpCompression.GzipWithTerminator;
        while (true)
        {
            var result = await _reader!.ReadAsync(cancellationToken).ConfigureAwait(false);
            var segment = result.Buffer;

            if (withTerminator && TryReadToTerminator(segment, out var payload, out var consumed))
            {
                var buffer = CopyToPooledBuffer(payload, out var length);
                AddBytesRead(consumed);
                _reader.AdvanceTo(segment.GetPosition(consumed));
                return (buffer, length);
            }

            if (result.IsCompleted)
            {
                if (withTerminator)
                {
                    _reader.AdvanceTo(segment.Start, segment.End);
                    throw new NntpException(
                        "The compressed multi-line data block ended before its terminator."
                    );
                }

                var buffer = CopyToPooledBuffer(segment, out var length);
                AddBytesRead(segment.Length);
                _reader.AdvanceTo(segment.End);
                return (buffer, length);
            }

            // Need more bytes: nothing consumed, everything examined.
            _reader.AdvanceTo(segment.Start, segment.End);
        }
    }

    private static bool TryReadToTerminator(
        in ReadOnlySequence<byte> segment,
        out ReadOnlySequence<byte> payload,
        out long consumed
    )
    {
        var reader = new SequenceReader<byte>(segment);
        if (reader.TryReadTo(out payload, CompressedBlockTerminator, advancePastDelimiter: true))
        {
            consumed = reader.Consumed;
            return true;
        }

        payload = default;
        consumed = 0;
        return false;
    }

    private static byte[] CopyToPooledBuffer(in ReadOnlySequence<byte> sequence, out int length)
    {
        length = (int)sequence.Length;
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(length, 1));
        sequence.CopyTo(buffer);
        return buffer;
    }

    /// <inheritdoc/>
    public async Task<TResponse> BufferedMultiLineCommandAsync<TResponse>(
        string command,
        IBufferedMultiLineResponseParser<TResponse> parser,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(parser);

        var response = await CommandAsync(command, new ResponseParser(), cancellationToken)
            .ConfigureAwait(false);

        if (!parser.IsSuccessResponse(response.Code))
        {
            return parser.ParseError(response.Code, response.Message);
        }

        if (_compressionActive)
        {
            await BeginCompressedDataBlockAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var (buffer, length) = await ReadDataBlockToBufferAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                // The parser takes ownership of the pooled buffer on the success path.
                return parser.Parse(response.Code, response.Message, buffer, length);
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }
        finally
        {
            EndCompressedDataBlock();
        }
    }

    /// <inheritdoc/>
    public async Task<TResponse> GetResponseAsync<TResponse>(
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(parser);

        var responseText = await ReadLineAsync(cancellationToken).ConfigureAwait(false);
        _log.ReceivedResponse(responseText ?? "");

        if (responseText == null)
        {
            throw new NntpException("Received no response.");
        }

        if (responseText.Length < 3 || !int.TryParse(responseText.AsSpan(0, 3), out var code))
        {
            throw new NntpException("Received invalid response.");
        }

        return parser.Parse(code, responseText[3..].Trim());
    }

    /// <inheritdoc/>
    public async Task WriteLineAsync(string line, CancellationToken cancellationToken = default)
    {
        BufferLine(line);
        await FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();
        await _writer!.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    internal bool Connected => _client.Connected;

    private void ThrowIfNotConnected()
    {
        if (!_client.Connected || _reader == null || _writer == null)
            throw new NntpException("Client not connected.");
    }

    private void ThrowIfStreaming()
    {
        if (_streaming)
            throw new NntpException(
                "A streamed multi-line response must be fully enumerated or disposed before issuing another command."
            );
    }

    private async Task<Stream> GetStreamAsync(string hostname, bool useSsl)
    {
        var stream = _client.GetStream();
        if (!useSsl)
        {
            return stream;
        }

        var sslStream = new SslStream(stream);
        await sslStream.AuthenticateAsClientAsync(hostname).ConfigureAwait(false);
        return sslStream;
    }

    private async IAsyncEnumerable<string> ReadMultiLineDataBlockAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        while (true)
        {
            var line = await ReadLineAsync(cancellationToken).ConfigureAwait(false);
            var processed = ProcessLine(line);
            if (processed == null)
            {
                yield break;
            }

            yield return processed;
        }
    }

    /// <summary>
    /// Reads a complete multi-line data block off the byte stream into a single contiguous buffer
    /// rented from <see cref="ArrayPool{T}"/>. Dot-stuffing is undone and the terminating dot is
    /// detected on the raw bytes, without transcoding the body to <see cref="string"/>. Each line is
    /// stored with a normalized CRLF terminator. The caller takes ownership of the returned buffer.
    /// </summary>
    private async Task<(byte[] Buffer, int Length)> ReadDataBlockToBufferAsync(
        CancellationToken cancellationToken
    )
    {
        var reader = DataBlockReader;
        var buffer = ArrayPool<byte>.Shared.Rent(InitialDataBlockBufferSize);
        var length = 0;
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var segment = result.Buffer;

                var consumed = AppendLines(segment, ref buffer, ref length, out var completed);
                AddDataBlockBytesRead(consumed);
                var consumedPosition = segment.GetPosition(consumed);

                if (completed || result.IsCompleted)
                {
                    reader.AdvanceTo(consumedPosition);
                    return (buffer, length);
                }

                reader.AdvanceTo(consumedPosition, segment.End);
            }
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

    private static long AppendLines(
        in ReadOnlySequence<byte> segment,
        ref byte[] buffer,
        ref int length,
        out bool completed
    )
    {
        completed = false;
        var reader = new SequenceReader<byte>(segment);
        while (
            reader.TryReadTo(
                out ReadOnlySequence<byte> lineSequence,
                (byte)'\n',
                advancePastDelimiter: true
            )
        )
        {
            if (AppendLine(lineSequence, ref buffer, ref length))
            {
                completed = true;
                break;
            }
        }

        return reader.Consumed;
    }

    /// <summary>
    /// Appends a single framed line to the data block buffer, returning <see langword="true"/> when the
    /// line is the terminating dot of the data block.
    /// </summary>
    private static bool AppendLine(
        in ReadOnlySequence<byte> lineSequence,
        ref byte[] buffer,
        ref int length
    )
    {
        if (lineSequence.IsSingleSegment)
        {
            return AppendLine(lineSequence.FirstSpan, ref buffer, ref length);
        }

        var lineLength = (int)lineSequence.Length;
        byte[]? rented = null;
        var line =
            lineLength <= StackAllocThreshold
                ? stackalloc byte[StackAllocThreshold]
                : (rented = ArrayPool<byte>.Shared.Rent(lineLength));
        try
        {
            lineSequence.CopyTo(line);
            return AppendLine(line[..lineLength], ref buffer, ref length);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static bool AppendLine(ReadOnlySpan<byte> line, ref byte[] buffer, ref int length)
    {
        // Strip the trailing CR of the CRLF terminator.
        if (line.Length > 0 && line[^1] == (byte)'\r')
        {
            line = line[..^1];
        }

        // A line containing only "." terminates the data block.
        if (line.Length == 1 && line[0] == (byte)'.')
        {
            return true;
        }

        // Undo dot-stuffing of a line that begins with ".".
        if (line.Length > 0 && line[0] == (byte)'.')
        {
            line = line[1..];
        }

        EnsureCapacity(ref buffer, length, length + line.Length + 2);
        line.CopyTo(buffer.AsSpan(length));
        length += line.Length;
        buffer[length++] = (byte)'\r';
        buffer[length++] = (byte)'\n';
        return false;
    }

    private static void EnsureCapacity(ref byte[] buffer, int length, int required)
    {
        if (buffer.Length >= required)
        {
            return;
        }

        var next = ArrayPool<byte>.Shared.Rent(Math.Max(buffer.Length * 2, required));
        buffer.AsSpan(0, length).CopyTo(next);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = next;
    }

    /// <summary>
    /// Reads a single CRLF-terminated line off the byte stream, counting the consumed bytes.
    /// </summary>
    private async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        var reader = DataBlockReader;
        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;

            if (TryReadLine(ref buffer, out var line, out var consumed))
            {
                AddDataBlockBytesRead(consumed);
                reader.AdvanceTo(buffer.Start);
                return line;
            }

            if (result.IsCompleted)
            {
                if (buffer.IsEmpty)
                {
                    reader.AdvanceTo(buffer.Start, buffer.End);
                    return null;
                }

                // The stream ended on a final line without a trailing CRLF.
                AddDataBlockBytesRead(buffer.Length);
                var remaining = DecodeLine(buffer);
                reader.AdvanceTo(buffer.End);
                return remaining;
            }

            reader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    private static bool TryReadLine(
        ref ReadOnlySequence<byte> buffer,
        out string line,
        out long consumed
    )
    {
        var reader = new SequenceReader<byte>(buffer);
        if (
            reader.TryReadTo(
                out ReadOnlySequence<byte> lineSequence,
                (byte)'\n',
                advancePastDelimiter: true
            )
        )
        {
            consumed = reader.Consumed;
            line = DecodeLine(lineSequence);
            buffer = buffer.Slice(reader.Position);
            return true;
        }

        line = string.Empty;
        consumed = 0;
        return false;
    }

    private static string DecodeLine(in ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsSingleSegment)
        {
            return DecodeSpan(sequence.FirstSpan);
        }

        var length = (int)sequence.Length;
        byte[]? rented = null;
        var buffer =
            length <= StackAllocThreshold
                ? stackalloc byte[StackAllocThreshold]
                : (rented = ArrayPool<byte>.Shared.Rent(length));
        try
        {
            sequence.CopyTo(buffer);
            return DecodeSpan(buffer[..length]);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static string DecodeSpan(ReadOnlySpan<byte> span)
    {
        // Strip the trailing CR of the CRLF terminator.
        if (span.Length > 0 && span[^1] == (byte)'\r')
        {
            span = span[..^1];
        }

        return UsenetEncoding.Default.GetString(span);
    }

    /// <inheritdoc/>
    public void BufferLine(string line)
    {
        ThrowIfNotConnected();
        ArgumentNullException.ThrowIfNull(line);

        var encoding = UsenetEncoding.Default;
        var byteCount = encoding.GetByteCount(line);
        var span = _output!.GetSpan(byteCount + 2);
        var written = encoding.GetBytes(line, span);
        span[written] = (byte)'\r';
        span[written + 1] = (byte)'\n';
        _output.Advance(written + 2);
    }

    /// <summary>
    /// Undoes dot-stuffing and detects the terminating dot of a multi-line data block, following the
    /// <a href="https://tools.ietf.org/html/rfc3977#section-3.1.1">RFC 3977</a> rules. The terminating
    /// line (".") and the end of input both map to <see langword="null"/>.
    /// </summary>
    private static string? ProcessLine(string? line)
    {
        if (line == null)
        {
            return null;
        }

        if (line.Length == 0 || line[0] != '.')
        {
            return line;
        }

        if (line.Length == 1)
        {
            return null;
        }

        return line[1] == '.' ? line[1..] : line;
    }

    private void AddBytesRead(long count)
    {
        unchecked
        {
            _bytesRead += count;
            if (_bytesRead < 0)
            {
                ResetCounters();
            }
        }
    }

    /// <summary>
    /// Counts bytes framed out of a data block. Only wire bytes are counted: while a compressed block
    /// is in flight the framing rides the in-memory inflated reader, and its wire cost was already
    /// counted when the compressed payload was read off the socket.
    /// </summary>
    private void AddDataBlockBytesRead(long count)
    {
        if (_dataReader == null)
        {
            AddBytesRead(count);
        }
    }

    private void AddBytesWritten(long count)
    {
        unchecked
        {
            _bytesWritten += count;
            if (_bytesWritten < 0)
            {
                ResetCounters();
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _dataReader?.Complete();
        _reader?.Complete();
        CompleteWriter();
        _stream?.Dispose();
        _client.Dispose();
    }

    /// <summary>
    /// An <see cref="IBufferWriter{T}"/> over the connection's <see cref="PipeWriter"/> that counts the
    /// bytes advanced into <see cref="BytesWritten"/>. Buffered bytes are sent on the next flush.
    /// </summary>
    private sealed class CountingBufferWriter(PipeWriter writer, NntpConnection owner)
        : IBufferWriter<byte>
    {
        public void Advance(int count)
        {
            writer.Advance(count);
            owner.AddBytesWritten(count);
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => writer.GetMemory(sizeHint);

        public Span<byte> GetSpan(int sizeHint = 0) => writer.GetSpan(sizeHint);
    }

    private void CompleteWriter()
    {
        if (_writer == null)
        {
            return;
        }

        try
        {
            // Completing flushes any buffered bytes, which can fault if the remote end already
            // dropped the connection. The stream is disposed next, so the buffer can be discarded.
            _writer.Complete();
        }
        catch (IOException)
        {
            // The remote end dropped the connection; the buffered bytes are abandoned with the stream.
        }
    }
}
