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
public sealed partial class NntpConnection : INntpConnection
{
    private const int StackAllocThreshold = 256;
    private const string AuthInfoPass = "AUTHINFO PASS";

    private readonly ILogger _log;
    private readonly TcpClient _client = new();
    private Stream? _stream;
    private PipeReader? _reader;
    private PipeWriter? _writer;
    private long _bytesRead;
    private long _bytesWritten;

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
        CancellationToken cancellationToken
    )
    {
        _log.Connecting(hostname, port, useSsl);
        await _client.ConnectAsync(hostname, port, cancellationToken).ConfigureAwait(false);
        _stream = await GetStreamAsync(hostname, useSsl).ConfigureAwait(false);
        _reader = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));
        _writer = PipeWriter.Create(_stream, new StreamPipeWriterOptions(leaveOpen: true));
        return await GetResponseAsync(parser, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<TResponse> CommandAsync<TResponse>(
        string command,
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken
    )
    {
        ThrowIfNotConnected();
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
        CancellationToken cancellationToken
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
    public async Task<TResponse> GetResponseAsync<TResponse>(
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken
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
    public async Task WriteLineAsync(string line, CancellationToken cancellationToken)
    {
        ThrowIfNotConnected();
        WriteLine(line);
        await _writer!.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    internal bool Connected => _client.Connected;

    private void ThrowIfNotConnected()
    {
        if (!_client.Connected || _reader == null || _writer == null)
            throw new NntpException("Client not connected.");
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

    private void WriteLine(string line)
    {
        var encoding = UsenetEncoding.Default;
        var byteCount = encoding.GetByteCount(line);
        var span = _writer!.GetSpan(byteCount + 2);
        var written = encoding.GetBytes(line, span);
        span[written] = (byte)'\r';
        span[written + 1] = (byte)'\n';
        _writer.Advance(written + 2);
        AddBytesWritten(written + 2);
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
