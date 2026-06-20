using Usenet.Nntp.Contracts;

namespace Usenet.Nntp;

internal sealed class PooledNntpClientLease : IPooledNntpClientLease
{
    private readonly NntpClientPool _pool;
    private readonly INntpPoolEntry _entry;
    private bool _disposed;

    public IPooledNntpClient Client => _entry.Client;

    internal PooledNntpClientLease(NntpClientPool pool, INntpPoolEntry entry)
    {
        _pool = pool;
        _entry = entry;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _pool.ReturnClient(_entry);
        _disposed = true;
    }
}
