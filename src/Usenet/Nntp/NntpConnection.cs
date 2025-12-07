using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Usenet.Exceptions;
using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Contracts;
using Usenet.Nntp.Parsers;
using Usenet.Util;
using Usenet.Util.Compatibility;

namespace Usenet.Nntp;

/// <summary>
/// A standard implementation of an NNTP connection.
/// Based on Kristian Hellang's NntpLib.Net project https://github.com/khellang/NntpLib.Net.
/// </summary>
/// <remarks>This implementation of the <see cref="INntpConnection"/> interface does support SSL encryption but
/// does not support compressed multi-line results.</remarks>
public sealed class NntpConnection : INntpConnection
{
    private readonly ILogger _log = Logger.Create<NntpConnection>();
    private readonly TcpClient _client = new();
    private CountingStream _stream;
    private StreamWriter _writer;
    private NntpStreamReader _reader;
    private const string AuthInfoPass = "AUTHINFO PASS";

    private bool _disposed;
    private bool _connected;

    /// <inheritdoc/>
    public async Task<TResponse> ConnectAsync<TResponse>(string hostname, int port, bool useSsl, IResponseParser<TResponse> parser,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _log.Connecting(hostname, port, useSsl);
        await _client.ConnectAsync(hostname, port, cancellationToken).ConfigureAwait(false);
        _stream = await GetStreamAsync(hostname, useSsl).ConfigureAwait(false);
        _writer = new StreamWriter(_stream, UsenetEncoding.Default) { AutoFlush = true };
        _reader = new NntpStreamReader(_stream, UsenetEncoding.Default);
        _connected = true;

        return await GetResponseAsync(parser, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<TResponse> CommandAsync<TResponse>(string command, IResponseParser<TResponse> parser, CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();
        Guard.ThrowIfNull(command, nameof(command));
        cancellationToken.ThrowIfCancellationRequested();

        var logCommand = command.StartsWith(AuthInfoPass, StringComparison.Ordinal)
            ? $"{AuthInfoPass} [REDACTED]"
            : command;
        _log.SendingCommand(logCommand);
        await WriteLineAsync(command, cancellationToken).ConfigureAwait(false);
        return await GetResponseAsync(parser, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<TResponse> MultiLineCommandAsync<TResponse>(string command, IMultiLineResponseParser<TResponse> parser,
        CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(parser, nameof(parser));
        cancellationToken.ThrowIfCancellationRequested();

        var response = await CommandAsync(command, new ResponseParser(), cancellationToken).ConfigureAwait(false);

        var dataBlock = parser.IsSuccessResponse(response.Code)
            ? ReadMultiLineDataBlock(cancellationToken)
            : AsyncEnumerable.Empty<string>();

        return parser.Parse(response.Code, response.Message, dataBlock);
    }

    /// <inheritdoc/>
    public async Task<TResponse> GetResponseAsync<TResponse>(IResponseParser<TResponse> parser, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(parser, nameof(parser));
        cancellationToken.ThrowIfCancellationRequested();

        var responseText = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        _log.ReceivedResponse(responseText ?? "");

        if (responseText == null)
        {
            throw new NntpException("Received no response.");
        }

        if (responseText.Length < 3 || !IntShims.TryParse(responseText.AsSpan(0, 3), out var code))
        {
            throw new NntpException("Received invalid response.");
        }

        return parser.Parse(code, responseText.Substring(3).Trim());
    }

    /// <inheritdoc/>
    public Task WriteLineAsync(string line, CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();
        cancellationToken.ThrowIfCancellationRequested();
        return _writer.WriteLineAsync(line);
    }

    public long BytesRead => _stream?.BytesRead ?? 0;
    public long BytesWritten => _stream?.BytesWritten ?? 0;
    public void ResetCounters() => _stream.ResetCounters();

    internal bool Connected => !_disposed && _connected && _client.Connected;

    private void ThrowIfNotConnected()
    {
        if (!_client.Connected)
            throw new NntpException("Client not connected.");
    }

    private async Task<CountingStream> GetStreamAsync(string hostname, bool useSsl)
    {
        var stream = _client.GetStream();
        if (!useSsl) return new CountingStream(stream);

        var sslStream = new SslStream(stream);
        await sslStream.AuthenticateAsClientAsync(hostname).ConfigureAwait(false);
        return new CountingStream(sslStream);
    }

    private async IAsyncEnumerable<string> ReadMultiLineDataBlock([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await _reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return line;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;

        _connected = false;

        _client?.Dispose();
        _writer?.Dispose();
        _reader?.Dispose();
        _stream?.Dispose();

        _disposed = true;
    }
}
