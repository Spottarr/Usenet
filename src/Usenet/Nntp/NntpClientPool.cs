using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usenet.Extensions;
using Usenet.Nntp.Contracts;
using Usenet.Util;

namespace Usenet.Nntp;

/// <inheritdoc />
[PublicAPI]
public sealed class NntpClientPool : INntpClientPool
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    private readonly Queue<IInternalPooledNntpClient> _availableClients = [];
    private readonly HashSet<IInternalPooledNntpClient> _usedClients = [];
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _monitorTask;

    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
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

    internal Func<IInternalPooledNntpClient> ClientFactory { get; init; }

    public NntpClientPool(
        int maxPoolSize,
        string hostname,
        int port,
        bool useSsl,
        string username,
        string password,
        ILoggerFactory? loggerFactory = null
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPoolSize);

        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<NntpClientPool>();
        ClientFactory = () => new PooledNntpClient(_loggerFactory);

        _semaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);

        _maxPoolSize = maxPoolSize;
        _hostname = hostname;
        _port = port;
        _useSsl = useSsl;
        _username = username;
        _password = password;

        // Start the background idle-monitor. The task is tracked so it can be awaited
        // to a clean stop on dispose, leaving no orphaned task or timer behind.
        _monitorTask = MonitorIdleClientsAsync(_cts.Token);
    }

    public Task<IPooledNntpClientLease> GetLease() => GetLease(CancellationToken.None);

    public async Task<IPooledNntpClientLease> GetLease(CancellationToken cancellationToken)
    {
        var client = await BorrowClient(cancellationToken).ConfigureAwait(false);
        return new PooledNntpClientLease(this, client);
    }

    private async Task<IInternalPooledNntpClient> BorrowClient(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var success = await _semaphore
            .WaitAsync(WaitTimeout, cancellationToken)
            .ConfigureAwait(false);
        if (!success)
            throw new InvalidOperationException("Timed out waiting for NNTP (usenet) client");

        var client = BorrowClientInternal();

        if (client is { Connected: true, Authenticated: true })
            return client;

        if (!client.Connected)
            await client
                .ConnectAsync(_hostname, _port, _useSsl, cancellationToken)
                .ConfigureAwait(false);
        if (!client.Authenticated)
            await client
                .AuthenticateAsync(_username, _password, cancellationToken)
                .ConfigureAwait(false);

        if (!client.Connected || !client.Authenticated)
            throw new InvalidOperationException(
                $"Failed to connect to '{_hostname}:{_port}' SSL={_useSsl} C={client.Connected} A={client.Authenticated}.'"
            );

        return client;
    }

    internal void ReturnClient(IInternalPooledNntpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.ReturningNntpClient();

        bool dispose;
        lock (_lock)
        {
            if (!_usedClients.Remove(client))
                throw new InvalidOperationException("Client not borrowed from this pool.");

            // If the client has encountered an error (e.g. broken pipe) during the most recent
            // operation, or a streamed response was returned without being fully drained, drop it
            // from the pool instead of handing back a connection with unread bytes on the wire.
            dispose = client.HasError || client.HasPendingStream;
            if (dispose)
            {
                _currentPoolSize--;
                _logger.DisposingErroredNntpClient(_currentPoolSize, _maxPoolSize);
            }
            else
            {
                _availableClients.Enqueue(client);
            }
        }

        // Disposing closes the underlying connection, which may block, so keep it out of the lock.
        if (dispose)
            client.Dispose();

        _semaphore.Release();
    }

    private IInternalPooledNntpClient BorrowClientInternal()
    {
        lock (_lock)
        {
            _logger.BorrowingNntpClient();

            if (_availableClients.TryDequeue(out var existingClient))
            {
                _usedClients.Add(existingClient);
                return existingClient;
            }

            if (_currentPoolSize >= _maxPoolSize)
                throw new InvalidOperationException("No available clients in the pool.");

            _currentPoolSize++;
            _logger.CreatingNewNntpClient(_currentPoolSize, _maxPoolSize);

            var newClient = ClientFactory.Invoke();

            _usedClients.Add(newClient);
            return newClient;
        }
    }

    private async Task MonitorIdleClientsAsync(CancellationToken cancellationToken)
    {
        // Yield out of the constructor so init-only properties (e.g. MonitorInterval) are
        // assigned by the object initializer before the timer reads them.
        await Task.Yield();

        using var timer = new PeriodicTimer(MonitorInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                EvictIdleClients();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the pool is disposed and the monitor is cancelled.
        }
    }

    private void EvictIdleClients()
    {
        var now = DateTimeOffset.Now;
        List<IInternalPooledNntpClient>? idleClients = null;

        lock (_lock)
        {
            var count = _availableClients.Count;
            for (var i = 0; i < count; i++)
            {
                var client = _availableClients.Dequeue();
                if (now - client.LastActivity > IdleTimeout)
                {
                    (idleClients ??= []).Add(client);
                    _currentPoolSize--;
                    _logger.DisposingIdleNntpClient(_currentPoolSize, _maxPoolSize);
                    continue;
                }

                _availableClients.Enqueue(client);
            }
        }

        if (idleClients is null)
            return;

        // Disposing closes the underlying connection, which may block, so keep it out of the lock.
        foreach (var client in idleClients)
            client.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        // Stop the idle monitor and wait for it to unwind before tearing down the clients it
        // would otherwise touch. The monitor swallows the cancellation, so this will not throw.
        _cts.Cancel();
        _monitorTask.GetAwaiter().GetResult();

        List<IInternalPooledNntpClient> clients;
        lock (_lock)
        {
            clients = [.. _availableClients, .. _usedClients];
            _availableClients.Clear();
            _usedClients.Clear();
        }

        foreach (var client in clients)
            client.Dispose();

        _semaphore.Dispose();
        _cts.Dispose();
    }
}
