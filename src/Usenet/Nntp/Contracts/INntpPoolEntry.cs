namespace Usenet.Nntp.Contracts;

/// <summary>
/// A single entry in the <see cref="NntpClientPool"/>. The pool manages connection and authentication
/// state through this interface and hands the underlying command <see cref="Client"/> to callers via a
/// lease; the per-command interception that previously lived here has moved down into the connection,
/// which now owns the <see cref="HasError"/> flag directly.
/// </summary>
internal interface INntpPoolEntry : IDisposable
{
    /// <summary>
    /// The connected, authenticated NNTP client exposed to the lease holder.
    /// </summary>
    IPooledNntpClient Client { get; }

    bool Connected { get; }
    bool Authenticated { get; }
    bool HasError { get; }

    /// <summary>
    /// Indicates that a streamed multi-line response was returned without being fully enumerated or
    /// disposed, so unread bytes remain on the connection and it cannot be safely reused.
    /// </summary>
    bool HasPendingStream { get; }

    DateTimeOffset LastActivity { get; }

    /// <summary>
    /// Stamps <see cref="LastActivity"/> with the current time so the pool's idle monitor measures
    /// the idle interval from when the client was last handed out or returned.
    /// </summary>
    void Touch();

    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    Task<bool> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default
    );
}
