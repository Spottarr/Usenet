namespace Usenet.Nntp.Contracts;

internal interface IInternalPooledNntpClient : IPooledNntpClient, INntpClient, IDisposable
{
    bool Connected { get; }
    bool Authenticated { get; }
    bool HasError { get; }

    /// <summary>
    /// Indicates that a streamed multi-line response was returned without being fully enumerated or
    /// disposed, so unread bytes remain on the connection and it cannot be safely reused.
    /// </summary>
    bool HasPendingStream { get; }

    DateTimeOffset LastActivity { get; }
}
