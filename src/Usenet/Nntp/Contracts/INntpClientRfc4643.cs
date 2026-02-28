namespace Usenet.Nntp.Contracts;

public interface INntpClientRfc4643
{
    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc4643#section-2.3">AUTHINFO USER and AUTHINFO PASS</a>
    /// (<a href="https://tools.ietf.org/html/rfc2980#section-3.1.1">ad 1</a>)
    /// commands are used to present clear text credentials to the server.
    /// </summary>
    /// <param name="username">The username to use.</param>
    /// <param name="password">The password to use.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>true if the user was authenticated successfully; otherwise false.</returns>
    Task<bool> AuthenticateAsync(
        string username,
        string password = null,
        CancellationToken cancellationToken = default
    );
}
