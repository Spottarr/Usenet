using System.Net.Security;
using System.Net.Sockets;
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
    private StreamWriter _writer;
    private NntpStreamReader _reader;
    private const string AuthInfoPass = "AUTHINFO PASS";

    /// <inheritdoc/>
    public CountingStream Stream { get; private set; }

    /// <inheritdoc/>
    public async Task<TResponse> ConnectAsync<TResponse>(string hostname, int port, bool useSsl, IResponseParser<TResponse> parser,
        CancellationToken cancellationToken = default)
    {
        _log.Connecting(hostname, port, useSsl);
        await _client.ConnectAsync(hostname, port, cancellationToken).ConfigureAwait(false);
        Stream = await GetStreamAsync(hostname, useSsl, cancellationToken).ConfigureAwait(false);
        _writer = new StreamWriter(Stream, UsenetEncoding.Default) { AutoFlush = true };
        _reader = new NntpStreamReader(Stream, UsenetEncoding.Default);
        return await GetResponseAsync(parser, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<TResponse> CommandAsync<TResponse>(string command, IResponseParser<TResponse> parser, CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();
        Guard.ThrowIfNull(command, nameof(command));

        var logCommand = command.StartsWith(AuthInfoPass, StringComparison.Ordinal)
            ? $"{AuthInfoPass} [REDACTED]"
            : command;
        _log.SendingCommand(logCommand);
        await WriteLineInternalAsync(command, cancellationToken).ConfigureAwait(false);
        return await GetResponseAsync(parser, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<TResponse> MultiLineCommandAsync<TResponse>(string command, IMultiLineResponseParser<TResponse> parser, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(parser, nameof(parser));

        var response = await CommandAsync(command, new ResponseParser(), cancellationToken).ConfigureAwait(false);

        var dataBlock = parser.IsSuccessResponse(response.Code)
            ? await ReadMultiLineDataBlockAsync(cancellationToken).ConfigureAwait(false)
            : [];

        return parser.Parse(response.Code, response.Message, dataBlock);
    }

    /// <inheritdoc/>
    public async Task<TResponse> GetResponseAsync<TResponse>(IResponseParser<TResponse> parser, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(parser, nameof(parser));

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
    public async Task WriteLineAsync(string line, CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();
        await WriteLineInternalAsync(line, cancellationToken).ConfigureAwait(false);
    }

    internal bool Connected => _client.Connected;

    private async Task WriteLineInternalAsync(string line, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        cancellationToken.ThrowIfCancellationRequested();
        await _writer.WriteLineAsync(line).ConfigureAwait(false);
#else
        await _writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
#endif
    }

    private void ThrowIfNotConnected()
    {
        if (!_client.Connected)
        {
            throw new NntpException("Client not connected.");
        }
    }

    private async Task<CountingStream> GetStreamAsync(string hostname, bool useSsl, CancellationToken cancellationToken = default)
    {
        var stream = _client.GetStream();
        if (!useSsl)
        {
            return new CountingStream(stream);
        }

        var sslStream = new SslStream(stream);
        await sslStream.AuthenticateAsClientAsync(hostname).ConfigureAwait(false);
        return new CountingStream(sslStream);
    }

    private async Task<List<string>> ReadMultiLineDataBlockAsync(CancellationToken cancellationToken = default)
    {
        var lines = new List<string>();
        while (await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            lines.Add(line);
        }
        return lines;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _client?.Dispose();
        _writer?.Dispose();
        _reader?.Dispose();
    }
}
