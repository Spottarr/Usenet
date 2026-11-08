using Usenet.Nntp.Contracts;

namespace Usenet.Nntp.Client.Pooling;

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

        // Mark the lease as returned before handing the client back: if the pool refuses the
        // return (e.g. it was disposed underneath us) a second dispose must not try again.
        _disposed = true;
        _pool.ReturnClient(_entry);
    }
}
