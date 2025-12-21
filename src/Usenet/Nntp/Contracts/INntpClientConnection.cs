using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Contracts;

public interface INntpClientConnection
{
    /// <summary>
    /// Attempts to establish a <a href="https://tools.ietf.org/html/rfc3977#section-5.1">connection</a> with a usenet server.
    /// </summary>
    /// <param name="hostname">The hostname of the usenet server.</param>
    /// <param name="port">The port to use.</param>
    /// <param name="useSsl">A value to indicate whether or not to use SSL encryption.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>true if a connection was made; otherwise false</returns>
    Task<bool> ConnectAsync(string hostname, int port, bool useSsl, CancellationToken cancellationToken = default);

    /// <summary>
    /// The client uses the
    /// <a href="https://tools.ietf.org/html/rfc3977#section-5.4">QUIT</a>
    /// command to terminate the session.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object.</returns>
    Task<NntpResponse> QuitAsync(CancellationToken cancellationToken = default);
}
