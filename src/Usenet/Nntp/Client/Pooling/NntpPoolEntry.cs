using Microsoft.Extensions.Logging;
using Usenet.Nntp.Contracts;

namespace Usenet.Nntp.Client.Pooling;

/// <summary>
/// Holds a pooled <see cref="NntpClient"/> together with its <see cref="NntpConnection"/> and the
/// connection/authentication state the pool needs to manage it. The connection owns the
/// <see cref="HasError"/> and <see cref="HasPendingStream"/> flags, so this type no longer mirrors the
/// command surface — the lease exposes the underlying <see cref="NntpClient"/> directly.
/// </summary>
internal sealed class NntpPoolEntry : INntpPoolEntry
{
    private readonly NntpConnection _connection;
    private readonly NntpClient _client;
    private bool _disposed;
    private bool _nntpConnected;

    public NntpPoolEntry(NntpConnectionOptions options, ILoggerFactory? loggerFactory = null)
    {
        _connection = new NntpConnection(options, loggerFactory);
        _client = new NntpClient(_connection, loggerFactory);
    }

    public IPooledNntpClient Client
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!Connected || !Authenticated)
                throw new InvalidOperationException("Client not connected or authenticated");

            Touch();
            return _client;
        }
    }

    public bool Connected => _connection.Connected && _nntpConnected;
    public bool Authenticated { get; private set; }
    public bool HasError => _connection.HasError;
    public bool HasPendingStream => _connection.HasPendingStream;
    public DateTimeOffset LastActivity { get; private set; }

    public void Touch() => LastActivity = DateTimeOffset.Now;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _nntpConnected = await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);
        return _nntpConnected;
    }

    public async Task<bool> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        Authenticated = await _client
            .AuthenticateAsync(username, password, cancellationToken)
            .ConfigureAwait(false);
        return Authenticated;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

#pragma warning disable CA1031
        try
        {
            // Try to gracefully QUIT the NNTP session
            if (_nntpConnected)
                _ = _client.QuitAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // It's possible that the connection is already closed or broken.
            // We can ignore any exceptions here as we're disposing anyway.
        }
#pragma warning restore CA1031

        _nntpConnected = false;
        Authenticated = false;
        _connection.Dispose();

        _disposed = true;
    }
}
