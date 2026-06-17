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
public sealed class NntpConnection : INntpConnection, INntpStreamSource
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
    private readonly TcpClient _client = new();
    private Stream? _stream;
    private PipeReader? _reader;
    private PipeWriter? _writer;
    private CountingBufferWriter? _output;
    private long _bytesRead;
    private long _bytesWritten;
    private bool _streaming;

    /// <summary>
    /// Creates a new instance of the <see cref="NntpConnection"/> class.
    /// </summary>
    /// <param name="loggerFactory">
    /// An optional <see cref="ILoggerFactory"/> used to create the connection's logger.
    /// When <see langword="null"/>, logging is disabled via <see cref="NullLoggerFactory"/>.
    /// </param>
    public NntpConnection(ILoggerFactory? loggerFactory = null) =>
        _log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<NntpConnection>();

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
        string hostname,
        int port,
        bool useSsl,
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken = default
    )
    {
        _log.Connecting(hostname, port, useSsl);
        await _client.ConnectAsync(hostname, port, cancellationToken).ConfigureAwait(false);
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

        var dataBlock = parser.IsSuccessResponse(response.Code)
            ? await ReadMultiLineDataBlockAsync(cancellationToken)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : [];

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

        // The data block stays on the wire until the caller enumerates or disposes the response.
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
        var buffer = ArrayPool<byte>.Shared.Rent(InitialDataBlockBufferSize);
        var length = 0;
        try
        {
            while (true)
            {
                var result = await _reader!.ReadAsync(cancellationToken).ConfigureAwait(false);
                var segment = result.Buffer;

                var consumed = AppendLines(segment, ref buffer, ref length, out var completed);
                AddBytesRead(consumed);
                var consumedPosition = segment.GetPosition(consumed);

                if (completed || result.IsCompleted)
                {
                    _reader.AdvanceTo(consumedPosition);
                    return (buffer, length);
                }

                _reader.AdvanceTo(consumedPosition, segment.End);
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
        while (true)
        {
            var result = await _reader!.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;

            if (TryReadLine(ref buffer, out var line, out var consumed))
            {
                AddBytesRead(consumed);
                _reader.AdvanceTo(buffer.Start);
                return line;
            }

            if (result.IsCompleted)
            {
                if (buffer.IsEmpty)
                {
                    _reader.AdvanceTo(buffer.Start, buffer.End);
                    return null;
                }

                // The stream ended on a final line without a trailing CRLF.
                AddBytesRead(buffer.Length);
                var remaining = DecodeLine(buffer);
                _reader.AdvanceTo(buffer.End);
                return remaining;
            }

            _reader.AdvanceTo(buffer.Start, buffer.End);
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
