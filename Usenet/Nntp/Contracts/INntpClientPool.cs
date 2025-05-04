namespace Usenet.Nntp.Contracts;

/// <summary>
/// Represents a pool of authenticated NNTP clients.
/// </summary>
internal interface INntpClientPool : IDisposable
{
    Task<PooledNntpClient> BorrowClient();
    void ReturnClient(PooledNntpClient client);
}
