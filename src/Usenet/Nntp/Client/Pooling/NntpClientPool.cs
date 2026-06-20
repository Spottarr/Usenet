using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usenet.Extensions;
using Usenet.Nntp.Contracts;

namespace Usenet.Nntp.Client.Pooling;

/// <inheritdoc />
[PublicAPI]
public sealed class NntpClientPool : INntpClientPool
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    private readonly Queue<INntpPoolEntry> _availableClients = [];
    private readonly HashSet<INntpPoolEntry> _usedClients = [];
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _monitorTask;

    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly SemaphoreSlim _semaphore;

    private readonly int _maxPoolSize;
    private readonly NntpConnectionOptions _connectionOptions;
    private readonly string _username;
    private readonly string _password;

    private int _currentPoolSize;
    private bool _disposed;

    public TimeSpan MonitorInterval { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan WaitTimeout { get; init; } = TimeSpan.FromSeconds(60);

    internal Func<INntpPoolEntry> ClientFactory { get; init; }

    public NntpClientPool(NntpPoolOptions options, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Connection);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxPoolSize);

        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<NntpClientPool>();

        _maxPoolSize = options.MaxPoolSize;
        _connectionOptions = options.Connection;
        _username = options.Username;
        _password = options.Password;

        ClientFactory = () => new NntpPoolEntry(_connectionOptions, _loggerFactory);

        _semaphore = new SemaphoreSlim(_maxPoolSize, _maxPoolSize);

        // Start the background idle-monitor. The task is tracked so it can be awaited
        // to a clean stop on dispose, leaving no orphaned task or timer behind.
        _monitorTask = MonitorIdleClientsAsync(_cts.Token);
    }

    public async Task<IPooledNntpClientLease> GetLease(
        CancellationToken cancellationToken = default
    )
    {
        var client = await BorrowClient(cancellationToken).ConfigureAwait(false);
        return new PooledNntpClientLease(this, client);
    }

    private async Task<INntpPoolEntry> BorrowClient(CancellationToken cancellationToken)
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
            await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
        if (!client.Authenticated)
            await client
                .AuthenticateAsync(_username, _password, cancellationToken)
                .ConfigureAwait(false);

        if (!client.Connected || !client.Authenticated)
            throw new InvalidOperationException(
                $"Failed to connect to '{_connectionOptions.Host}:{_connectionOptions.Port}' "
                    + $"SSL={_connectionOptions.UseSsl} C={client.Connected} A={client.Authenticated}.'"
            );

        return client;
    }

    internal void ReturnClient(INntpPoolEntry client)
    {
        ArgumentNullException.ThrowIfNull(client);
        ObjectDisposedException.ThrowIf(_disposed, this);

        bool dispose;
        lock (_lock)
        {
            _logger.ReturningNntpClient();

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
                // Stamp the activity time on return so the idle monitor measures the idle interval
                // from when the connection became available again.
                client.Touch();
                _availableClients.Enqueue(client);
            }
        }

        // Disposing closes the underlying connection, which may block, so keep it out of the lock.
        if (dispose)
            client.Dispose();

        _semaphore.Release();
    }

    private INntpPoolEntry BorrowClientInternal()
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
        List<INntpPoolEntry>? idleClients = null;

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

        List<INntpPoolEntry> clients;
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
