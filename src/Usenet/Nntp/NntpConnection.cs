using System.Buffers;
using System.IO.Compression;
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
/// <remarks>This implementation of the <see cref="INntpConnection"/> interface supports SSL encryption and,
/// when an <see cref="NntpCompression"/> mode is configured, the transparent <c>COMPRESS DEFLATE</c>
/// transport (<a href="https://www.rfc-editor.org/rfc/rfc8054">RFC 8054</a>): once negotiated, the whole
/// session is carried as a continuous DEFLATE stream in both directions and the line framer rides the
/// decompressed bytes unchanged. The transport is built on
/// <see cref="System.IO.Pipelines"/>: lines are framed off the byte stream, dot-stuffing is undone and the
/// terminating dot is detected without transcoding the whole stream to <see cref="string"/>.</remarks>
[PublicAPI]
public sealed class NntpConnection : INntpConnection, INntpStreamSource, INntpCompressionControl
{
    private const int StackAllocThreshold = 256;
    private const int InitialDataBlockBufferSize = 4096;
    private const string AuthInfoPass = "AUTHINFO PASS";

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

    // The bidirectional DEFLATE layer installed once COMPRESS DEFLATE is negotiated (RFC 8054). The
    // command writer compresses into _compressStream and the line framer reads inflated bytes off
    // _decompressStream; both are null in plain mode. See ADR-0005.
    private DeflateStream? _compressStream;
    private DeflateStream? _decompressStream;

    // The per-command decompression scope installed for an XZVER/XZHDR data block (ADR-0006). Unlike
    // the session-wide DEFLATE layer above, this is scoped to one command's data block: the line
    // framer reads inflated bytes off _scopeDecompressStream, and the scope is torn down at the
    // in-band dot terminator so subsequent commands read plaintext again. Null when no scope is active.
    private Stream? _scopeDecompressStream;
    private bool _decompressionScopeActive;

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
        if (_options.Compression == NntpCompression.None || _compressionActive)
        {
            return;
        }

        // The COMPRESS command and its status-line response are exchanged in clear text; compression
        // takes effect for both directions immediately after the 206 CRLF (RFC 8054 §2.2).
        var response = await CommandAsync(
                "COMPRESS DEFLATE",
                new ResponseParser(),
                cancellationToken
            )
            .ConfigureAwait(false);

        // 206 = compression active; 502 = already active. Anything else must throw rather than
        // silently leave the transport expecting a compressed stream the server will never send.
        if (response.Code is not (206 or 502))
        {
            throw new NntpException(
                $"Server rejected 'COMPRESS DEFLATE': {response.Code} {response.Message}"
            );
        }

        InstallDeflateLayer();
        _compressionActive = true;
    }

    /// <summary>
    /// Installs the bidirectional raw-DEFLATE layer over the live stream and rebuilds the pipe reader,
    /// writer and counting output on top of it. Because RFC 8054 starts compression immediately after
    /// the 206 CRLF, a server may pipeline the status line and the first compressed bytes into one TCP
    /// segment; any such bytes the plaintext reader buffered past the status line are recovered and
    /// replayed through the decompressor ahead of the socket so the segment decodes correctly.
    /// </summary>
    private void InstallDeflateLayer()
    {
        var leftover = DrainBufferedBytes(_reader!);
        _reader!.Complete();
        _writer!.Complete();

        var decompressInput = leftover.Length > 0 ? new PrefixStream(leftover, _stream!) : _stream!;
        _decompressStream = new DeflateStream(
            decompressInput,
            CompressionMode.Decompress,
            leaveOpen: true
        );
        _compressStream = new DeflateStream(_stream!, CompressionMode.Compress, leaveOpen: true);

        _reader = PipeReader.Create(_decompressStream);
        _writer = PipeWriter.Create(_compressStream);
        _output = new CountingBufferWriter(_writer, this);
    }

    /// <summary>
    /// Returns the bytes already buffered in <paramref name="reader"/> without waiting on the socket,
    /// leaving the reader fully consumed. Used to recover bytes over-read past the COMPRESS status line.
    /// </summary>
    private static byte[] DrainBufferedBytes(PipeReader reader)
    {
        if (!reader.TryRead(out var result))
        {
            return [];
        }

        var buffer = result.Buffer;
        var bytes = buffer.ToArray();
        reader.AdvanceTo(buffer.End);
        return bytes;
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

        var dataBlock = await ReadMultiLineDataBlockAsync(cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return parser.Parse(response.Code, response.Message, dataBlock);
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

        // When compression is active the data block is transparently inflated by the transport's
        // DEFLATE layer, so the response streams per line off the decompressing reader exactly as it
        // does in plain mode; the data block stays buffered until the caller enumerates or disposes.
        _streaming = true;
        return new NntpStreamResponse<T>(response.Code, response.Message, this, lineParser);
    }

    /// <inheritdoc/>
    public async Task<NntpStreamResponse<T>> MultiLineDecompressedStreamCommandAsync<T>(
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

        // The data block is a single compressed member with the dot terminator inside it. Install a
        // per-command decompression scope (codec chosen by sniffing the first byte), then stream the
        // inflated lines through the existing framer exactly as the plaintext siblings do. See ADR-0006.
        await InstallDecompressionScopeAsync(cancellationToken).ConfigureAwait(false);
        _streaming = true;
        return new NntpStreamResponse<T>(response.Code, response.Message, this, lineParser);
    }

    /// <summary>
    /// Installs the per-command decompression scope over the live stream and rebuilds the line reader
    /// on top of it. The data block's first byte selects the decoder — the <c>[COMPRESS=GZIP]</c> label
    /// is unreliable, so <c>0x78</c> (a zlib header) maps to <see cref="ZLibStream"/>, <c>0x1f</c> (the
    /// gzip magic) to <see cref="GZipStream"/>, and anything else to a raw <see cref="DeflateStream"/>.
    /// Any bytes the plaintext reader buffered past the status line are recovered and replayed through
    /// the decompressor ahead of the socket via <see cref="PrefixStream"/>. See ADR-0006.
    /// </summary>
    private async ValueTask InstallDecompressionScopeAsync(CancellationToken cancellationToken)
    {
        var leftover = DrainBufferedBytes(_reader!);
        await _reader!.CompleteAsync().ConfigureAwait(false);

        // The decoder is chosen from the first data-block byte, so make sure one is available to peek.
        if (leftover.Length == 0)
        {
            var first = new byte[1];
            var read = await _stream!.ReadAsync(first, cancellationToken).ConfigureAwait(false);
            leftover = read == 0 ? [] : first;
        }

        var input = leftover.Length > 0 ? new PrefixStream(leftover, _stream!) : _stream!;
        var magic = leftover.Length > 0 ? leftover[0] : (byte)0;
        _scopeDecompressStream = magic switch
        {
            0x78 => new ZLibStream(input, CompressionMode.Decompress, leaveOpen: true),
            0x1f => new GZipStream(input, CompressionMode.Decompress, leaveOpen: true),
            _ => new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true),
        };
        _reader = PipeReader.Create(_scopeDecompressStream);
        _decompressionScopeActive = true;
    }

    /// <summary>
    /// Tears down the per-command decompression scope at the dot terminator, disposing the
    /// decompressor and restoring a plaintext reader over the live stream so the next command reads
    /// uncompressed bytes. A no-op when no scope is active. The decompressor is created with
    /// <c>leaveOpen</c>, so disposing it leaves the underlying stream intact.
    /// </summary>
    private void TeardownDecompressionScope()
    {
        if (!_decompressionScopeActive)
        {
            return;
        }

        _decompressionScopeActive = false;
        _reader!.Complete();
        _scopeDecompressStream!.Dispose();
        _scopeDecompressStream = null;
        _reader = PipeReader.Create(_stream!, new StreamPipeReaderOptions(leaveOpen: true));
    }

    /// <summary>
    /// Marks the in-flight stream as finished and tears down any per-command decompression scope so
    /// the connection is ready to serve the next command in plaintext.
    /// </summary>
    private void EndStream()
    {
        _streaming = false;
        TeardownDecompressionScope();
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
            EndStream();
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
                EndStream();
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
        var reader = _reader!;
        var buffer = ArrayPool<byte>.Shared.Rent(InitialDataBlockBufferSize);
        var length = 0;
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var segment = result.Buffer;

                var consumed = AppendLines(segment, ref buffer, ref length, out var completed);
                AddBytesRead(consumed);
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
        var reader = _reader!;
        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;

            if (TryReadLine(ref buffer, out var line, out var consumed))
            {
                AddBytesRead(consumed);
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
                AddBytesRead(buffer.Length);
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

    /// <summary>
    /// Counts bytes framed off the reader. When compression is active the reader sits above the
    /// DEFLATE layer, so this counts decompressed (logical) bytes; the compressed wire size is not
    /// tracked separately (ADR-0005).
    /// </summary>
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
        // Completing the reader/writer disposes the DEFLATE streams when they are installed; both are
        // created with leaveOpen so the underlying stream survives until it is disposed below.
        _reader?.Complete();
        CompleteWriter();
        _decompressStream?.Dispose();
        _compressStream?.Dispose();
        _scopeDecompressStream?.Dispose();
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

    /// <summary>
    /// A read-only stream that yields a fixed prefix of bytes before delegating to an inner stream.
    /// Used to replay bytes over-read past the COMPRESS status line back into the decompressor ahead
    /// of the socket, so a server that coalesces the 206 response and the first compressed bytes into
    /// one segment decodes correctly. See ADR-0005.
    /// </summary>
    private sealed class PrefixStream(byte[] prefix, Stream inner) : Stream
    {
        private int _offset;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (_offset >= prefix.Length)
            {
                return inner.Read(buffer);
            }

            var n = Math.Min(buffer.Length, prefix.Length - _offset);
            prefix.AsSpan(_offset, n).CopyTo(buffer);
            _offset += n;
            return n;
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        ) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            if (_offset >= prefix.Length)
            {
                return inner.ReadAsync(buffer, cancellationToken);
            }

            var n = Math.Min(buffer.Length, prefix.Length - _offset);
            prefix.AsSpan(_offset, n).CopyTo(buffer.Span);
            _offset += n;
            return ValueTask.FromResult(n);
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
