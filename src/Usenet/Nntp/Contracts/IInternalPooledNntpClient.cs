namespace Usenet.Nntp.Contracts;

internal interface IInternalPooledNntpClient : IPooledNntpClient, INntpClient, IDisposable
{
    bool Connected { get; }
    bool Authenticated { get; }
    bool HasError { get; }
    DateTimeOffset LastActivity { get; }
}
