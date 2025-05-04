namespace Usenet.Nntp.Contracts;

/// <summary>
/// Represents a pool of authenticated NNTP clients.
/// </summary>
internal interface INntpClientPool : IDisposable
{
    /// <summary>
    /// Time between checks for idle clients
    /// </summary>
    TimeSpan MonitorInterval { get; init; }

    /// <summary>
    /// Time a client can be idle before automatically being disconnected
    /// </summary>
    TimeSpan IdleTimeout { get; init; }

    /// <summary>
    /// Time to wait for a client to become available when all clients in the pool have been borrowed
    /// </summary>
    TimeSpan WaitTimeout { get; init; }

    /// <summary>
    /// Retrieves an authenticated and connected NNTP client from the pool.
    /// If no clients are available, it will wait until one becomes available or the wait times out.
    /// </summary>
    /// <returns></returns>
    Task<PooledNntpClient> BorrowClient();
    void ReturnClient(PooledNntpClient client);
}
