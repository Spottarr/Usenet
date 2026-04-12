using JetBrains.Annotations;

namespace Usenet.Nntp.Contracts;

[PublicAPI]
public interface IPooledNntpClientLease : IDisposable
{
    public IPooledNntpClient Client { get; }
}
