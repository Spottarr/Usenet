using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Contracts;
using Usenet.Util;

namespace Usenet.Nntp;

/// <inheritdoc />
public sealed class NntpClientPool : INntpClientPool
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    private readonly Queue<PooledNntpClient> _availableClients = [];
    private readonly Dictionary<IPooledNntpClient, PooledNntpClient> _usedClients = [];
    private readonly CancellationTokenSource _cts = new();

    private readonly ILogger _logger = Logger.Create<NntpConnection>();
    private readonly SemaphoreSlim _semaphore;

    private readonly int _maxPoolSize;
    private readonly string _hostname;
    private readonly int _port;
    private readonly bool _useSsl;
    private readonly string _username;
    private readonly string _password;

    private int _currentPoolSize;
    private bool _disposed;

    public TimeSpan MonitorInterval { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan WaitTimeout { get; init; } = TimeSpan.FromSeconds(60);

    internal Func<PooledNntpClient> ClientFactory { get; init; } = () => new PooledNntpClient();

    public NntpClientPool(int maxPoolSize, string hostname, int port, bool useSsl, string username, string password)
    {
        Guard.ThrowIfNegativeOrZero(maxPoolSize, nameof(maxPoolSize));

        _semaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);

        _maxPoolSize = maxPoolSize;
        _hostname = hostname;
        _port = port;
        _useSsl = useSsl;
        _username = username;
        _password = password;

        // Start the background monitoring task
        Task.Run(() => MonitorIdleClients(_cts.Token));
    }

    public async Task<IPooledNntpClient> BorrowClient()
    {
        ObjectDisposedExceptionShims.ThrowIf(_disposed, this);

        var success = await _semaphore.WaitAsync(WaitTimeout).ConfigureAwait(false);
        if (!success) throw new InvalidOperationException("Timed out waiting for NNTP (usenet) client");

        _logger.BorrowingNntpClient();
        var client = BorrowClientInternal();

        if (client.Connected && client.Authenticated) return client;

        if (!client.Connected) await client.ConnectAsync(_hostname, _port, _useSsl).ConfigureAwait(false);
        if (!client.Authenticated) client.Authenticate(_username, _password);

        if (!client.Connected || !client.Authenticated)
            throw new InvalidOperationException($"Failed to connect to '{_hostname}:{_port}' SSL={_useSsl} C={client.Connected} A={client.Connected}.'");

        return client;
    }

    public void ReturnClient(IPooledNntpClient client)
    {
        Guard.ThrowIfNull(client, nameof(client));
        ObjectDisposedExceptionShims.ThrowIf(_disposed, this);

        _logger.ReturningNntpClient();

        lock (_lock)
        {
#pragma warning disable CA2000
            if(!_usedClients.Remove(client, out var impl))
#pragma warning restore CA2000
                throw new InvalidOperationException("Client not borrowed from this pool.");

            _availableClients.Enqueue(impl);
            _semaphore.Release();
        }
    }

    private PooledNntpClient BorrowClientInternal()
    {
        lock (_lock)
        {
            if (_availableClients.TryDequeue(out var existingClient))
            {
                _usedClients.Add(existingClient, existingClient);
                return existingClient;
            }

            if (_currentPoolSize > _maxPoolSize)
                throw new InvalidOperationException("No available clients in the pool.");

            _currentPoolSize++;
            _logger.CreatingNewNntpClient(_currentPoolSize, _maxPoolSize);

            var newClient = ClientFactory.Invoke();

            _usedClients.Add(newClient, newClient);
            return newClient;
        }
    }

    private async Task MonitorIdleClients(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(MonitorInterval, ct).ConfigureAwait(false);
            var now = DateTimeOffset.Now;

            lock (_lock)
            {
                var count = _availableClients.Count;
                for (var i = 0; i < count; i++)
                {
                    var client = _availableClients.Dequeue();
                    if (now - client.LastActivity > IdleTimeout)
                    {
                        client.Dispose();
                        _currentPoolSize--;
                        _logger.DisposingIdleNntpClient(_currentPoolSize, _maxPoolSize);
                        continue;
                    }

                    _availableClients.Enqueue(client);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cts.Cancel();

        lock (_lock)
        {
            foreach (var client in _availableClients)
            {
                client.Dispose();
            }

            _availableClients.Clear();
        }

        _semaphore.Dispose();
        _cts.Dispose();

        _disposed = true;
    }
}
