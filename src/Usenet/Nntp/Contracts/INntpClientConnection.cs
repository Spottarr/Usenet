using JetBrains.Annotations;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Contracts;

[PublicAPI]
public interface INntpClientConnection
{
    /// <summary>
    /// Attempts to establish a <a href="https://tools.ietf.org/html/rfc3977#section-5.1">connection</a>
    /// with a usenet server. The host, port and SSL setting are read from the
    /// <see cref="NntpConnectionOptions"/> the underlying connection was created with.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>true if a connection was made; otherwise false</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The client uses the
    /// <a href="https://tools.ietf.org/html/rfc3977#section-5.4">QUIT</a>
    /// command to terminate the session.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object.</returns>
    Task<NntpResponse> QuitAsync(CancellationToken cancellationToken = default);
}
