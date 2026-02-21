using Usenet.Nntp.Parsers;
using Usenet.Util;

namespace Usenet.Nntp.Contracts;

/// <summary>
/// Represents an NNTP connection.
/// Based on Kristian Hellang's NntpLib.Net project https://github.com/khellang/NntpLib.Net.
/// </summary>
public interface INntpConnection : IDisposable
{
    /// <summary>
    /// Attempts to establish a connection with a usenet server.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="hostname">The hostname of the usenet server.</param>
    /// <param name="port">The port to use.</param>
    /// <param name="useSsl">A value to indicate whether to use SSL encryption.</param>
    /// <param name="parser">The response parser to use.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> ConnectAsync<TResponse>(string hostname, int port, bool useSsl, IResponseParser<TResponse> parser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command to the usenet server asynchronously. The response is expected to be a single line.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="command">The command to send to the server.</param>
    /// <param name="parser">The response parser to use.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> CommandAsync<TResponse>(string command, IResponseParser<TResponse> parser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command to the usenet server asynchronously. The response is expected to be multiple lines.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="command">The command to send to the server.</param>
    /// <param name="parser">The multi-line response parser to use.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> MultiLineCommandAsync<TResponse>(string command, IMultiLineResponseParser<TResponse> parser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single-line response from the usenet server asynchronously.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="parser">The response parser to use.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> GetResponseAsync<TResponse>(IResponseParser<TResponse> parser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a line to the usenet server asynchronously.
    /// </summary>
    /// <param name="line">The line to send to the usenet server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteLineAsync(string line, CancellationToken cancellationToken = default);

    /// <summary>
    /// The stream used by the connection.
    /// </summary>
    CountingStream? Stream { get; }
}
