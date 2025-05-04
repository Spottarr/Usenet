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
    private readonly TimeSpan _monitorInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _idleTimeout = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(60);
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

    public NntpClientPool(int maxPoolSize, string hostname, int port, bool useSsl, string username, string password)
    {
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

    public async Task<PooledNntpClient> BorrowClient()
    {
        ObjectDisposedExceptionShims.ThrowIf(_disposed, this);

        var success = await _semaphore.WaitAsync(_waitTimeout).ConfigureAwait(false);
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

    public void ReturnClient(PooledNntpClient client)
    {
        Guard.ThrowIfNull(client, nameof(client));
        ObjectDisposedExceptionShims.ThrowIf(_disposed, this);

        client.Flush();
        _logger.ReturningNntpClient();

        lock (_lock)
        {
            _availableClients.Enqueue(client);
            _semaphore.Release();
        }
    }

    private PooledNntpClient BorrowClientInternal()
    {
        lock (_lock)
        {
            if (_availableClients.TryDequeue(out var client))
                return client;

            if (_currentPoolSize > _maxPoolSize)
                throw new InvalidOperationException("No available clients in the pool.");

            _currentPoolSize++;
            _logger.CreatingNewNntpClient(_currentPoolSize, _maxPoolSize);

            return new PooledNntpClient();
        }
    }

    private async Task MonitorIdleClients(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(_monitorInterval, ct).ConfigureAwait(false);
            var now = DateTimeOffset.Now;

            lock (_lock)
            {
                var count = _availableClients.Count;
                for (var i = 0; i < count; i++)
                {
                    var client = _availableClients.Dequeue();
                    if (now - client.LastActivity > _idleTimeout)
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
