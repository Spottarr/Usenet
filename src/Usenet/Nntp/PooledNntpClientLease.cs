using Usenet.Nntp.Contracts;

namespace Usenet.Nntp;

internal sealed class PooledNntpClientLease : IPooledNntpClientLease
{
    private readonly NntpClientPool _pool;
    private readonly IInternalPooledNntpClient _client;
    private bool _disposed;

    public IPooledNntpClient Client => _client;

    internal PooledNntpClientLease(NntpClientPool pool, IInternalPooledNntpClient client)
    {
        _pool = pool;
        _client = client;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _pool.ReturnClient(_client);
        _disposed = true;
    }
}
