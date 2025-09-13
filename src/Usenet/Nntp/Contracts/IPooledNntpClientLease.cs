namespace Usenet.Nntp.Contracts;

public interface IPooledNntpClientLease : IDisposable
{
    public IPooledNntpClient Client { get; }
}
