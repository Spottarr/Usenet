using Usenet.Nntp.Parsers;

namespace Usenet.Nntp;

public sealed partial class NntpConnection
{
    /// <inheritdoc/>
    public Task<TResponse> ConnectAsync<TResponse>(
        string hostname,
        int port,
        bool useSsl,
        IResponseParser<TResponse> parser
    ) => ConnectAsync(hostname, port, useSsl, parser, CancellationToken.None);

    /// <inheritdoc/>
    public Task<TResponse> CommandAsync<TResponse>(
        string command,
        IResponseParser<TResponse> parser
    ) => CommandAsync(command, parser, CancellationToken.None);

    /// <inheritdoc/>
    public Task<TResponse> MultiLineCommandAsync<TResponse>(
        string command,
        IMultiLineResponseParser<TResponse> parser
    ) => MultiLineCommandAsync(command, parser, CancellationToken.None);

    /// <inheritdoc/>
    public Task<TResponse> GetResponseAsync<TResponse>(IResponseParser<TResponse> parser) =>
        GetResponseAsync(parser, CancellationToken.None);

    /// <inheritdoc/>
    public Task WriteLineAsync(string line) => WriteLineAsync(line, CancellationToken.None);
}
