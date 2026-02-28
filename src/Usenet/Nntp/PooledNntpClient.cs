using Usenet.Nntp.Contracts;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;
using Usenet.Util.Compatibility;

namespace Usenet.Nntp;

/// <inheritdoc cref="IPooledNntpClient" />
internal sealed class PooledNntpClient : IInternalPooledNntpClient
{
    private readonly NntpConnection _connection;
    private readonly NntpClient _client;
    private bool _disposed;
    private bool TcpConnected => _connection.Connected;
    private bool NntpConnected { get; set; }
    public DateTimeOffset LastActivity { get; private set; }
    public bool Connected => TcpConnected && NntpConnected;
    public bool Authenticated { get; private set; }
    public bool HasError { get; private set; }

    public PooledNntpClient()
    {
        _connection = new NntpConnection();
        _client = new NntpClient(_connection);
    }

    #region INntpClient

    public async Task<bool> ConnectAsync(
        string hostname,
        int port,
        bool useSsl,
        CancellationToken cancellationToken = default
    )
    {
        var res = await _client
            .ConnectAsync(hostname, port, useSsl, cancellationToken)
            .ConfigureAwait(false);
        NntpConnected = res;
        return res;
    }

    public async Task<bool> AuthenticateAsync(
        string username,
        string password = null,
        CancellationToken cancellationToken = default
    )
    {
        var res = await _client
            .AuthenticateAsync(username, password, cancellationToken)
            .ConfigureAwait(false);
        Authenticated = res;
        return res;
    }

    public Task<NntpResponse> XfeatureCompressGzipAsync(
        bool withTerminator,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XfeatureCompressGzipAsync(withTerminator, cancellationToken));

    public Task<NntpMultiLineResponse> XzhdrAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XzhdrAsync(field, messageId, cancellationToken));

    public Task<NntpMultiLineResponse> XzhdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XzhdrAsync(field, range, cancellationToken));

    public Task<NntpMultiLineResponse> XzhdrAsync(
        string field,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XzhdrAsync(field, cancellationToken));

    public Task<NntpMultiLineResponse> XzverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XzverAsync(range, cancellationToken));

    public Task<NntpMultiLineResponse> XzverAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.XzverAsync(cancellationToken));

    public void ResetCounters() => _client.ResetCounters();

    public Task<NntpMultiLineResponse> XhdrAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XhdrAsync(field, messageId, cancellationToken));

    public Task<NntpMultiLineResponse> XhdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XhdrAsync(field, range, cancellationToken));

    public Task<NntpMultiLineResponse> XhdrAsync(
        string field,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XhdrAsync(field, cancellationToken));

    public Task<NntpMultiLineResponse> XoverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XoverAsync(range, cancellationToken));

    public Task<NntpMultiLineResponse> XoverAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.XoverAsync(cancellationToken));

    public Task<NntpMultiLineResponse> CapabilitiesAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CapabilitiesAsync(cancellationToken));

    public Task<NntpMultiLineResponse> CapabilitiesAsync(
        string keyword,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CapabilitiesAsync(keyword, cancellationToken));

    public Task<NntpModeReaderResponse> ModeReaderAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ModeReaderAsync(cancellationToken));

    public Task<NntpResponse> QuitAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.QuitAsync(cancellationToken));

    public Task<NntpGroupResponse> GroupAsync(
        string group,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.GroupAsync(group, cancellationToken));

    public Task<NntpGroupResponse> ListGroupAsync(
        string group,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListGroupAsync(group, range, cancellationToken));

    public Task<NntpGroupResponse> ListGroupAsync(
        string group,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListGroupAsync(group, cancellationToken));

    public Task<NntpGroupResponse> ListGroupAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.ListGroupAsync(cancellationToken));

    public Task<NntpLastResponse> LastAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.LastAsync(cancellationToken));

    public Task<NntpNextResponse> NextAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.NextAsync(cancellationToken));

    public Task<NntpArticleResponse> ArticleAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ArticleAsync(messageId, cancellationToken));

    public Task<NntpArticleResponse> ArticleAsync(
        long number,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ArticleAsync(number, cancellationToken));

    public Task<NntpArticleResponse> ArticleAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.ArticleAsync(cancellationToken));

    public Task<NntpArticleResponse> HeadAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.HeadAsync(messageId, cancellationToken));

    public Task<NntpArticleResponse> HeadAsync(
        long number,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.HeadAsync(number, cancellationToken));

    public Task<NntpArticleResponse> HeadAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.HeadAsync(cancellationToken));

    public Task<NntpArticleResponse> BodyAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.BodyAsync(messageId, cancellationToken));

    public Task<NntpArticleResponse> BodyAsync(
        long number,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.BodyAsync(number, cancellationToken));

    public Task<NntpArticleResponse> BodyAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.BodyAsync(cancellationToken));

    public Task<NntpStatResponse> StatAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.StatAsync(messageId, cancellationToken));

    public Task<NntpStatResponse> StatAsync(
        long number,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.StatAsync(number, cancellationToken));

    public Task<NntpStatResponse> StatAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.StatAsync(cancellationToken));

    public Task<bool> PostAsync(
        NntpArticle article,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.PostAsync(article, cancellationToken));

    public Task<bool> IhaveAsync(
        NntpArticle article,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.IhaveAsync(article, cancellationToken));

    public Task<NntpDateResponse> DateAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.DateAsync(cancellationToken));

    public Task<NntpMultiLineResponse> HelpAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.HelpAsync(cancellationToken));

    public Task<NntpGroupsResponse> NewGroupsAsync(
        NntpDateTime sinceDateTime,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.NewGroupsAsync(sinceDateTime, cancellationToken));

    public Task<NntpMultiLineResponse> NewNewsAsync(
        string wildmat,
        NntpDateTime sinceDateTime,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.NewNewsAsync(wildmat, sinceDateTime, cancellationToken));

    public Task<NntpGroupOriginsResponse> ListActiveTimesAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListActiveTimesAsync(cancellationToken));

    public Task<NntpGroupOriginsResponse> ListActiveTimesAsync(
        string wildmat,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListActiveTimesAsync(wildmat, cancellationToken));

    public Task<NntpMultiLineResponse> ListDistribPatsAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListDistribPatsAsync(cancellationToken));

    public Task<NntpMultiLineResponse> ListNewsgroupsAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListNewsgroupsAsync(cancellationToken));

    public Task<NntpMultiLineResponse> ListNewsgroupsAsync(
        string wildmat,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListNewsgroupsAsync(wildmat, cancellationToken));

    public Task<NntpMultiLineResponse> OverAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.OverAsync(messageId, cancellationToken));

    public Task<NntpMultiLineResponse> OverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.OverAsync(range, cancellationToken));

    public Task<NntpMultiLineResponse> OverAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.OverAsync(cancellationToken));

    public Task<NntpMultiLineResponse> ListOverviewFormatAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListOverviewFormatAsync(cancellationToken));

    public Task<NntpMultiLineResponse> HdrAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.HdrAsync(field, messageId, cancellationToken));

    public Task<NntpMultiLineResponse> HdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.HdrAsync(field, range, cancellationToken));

    public Task<NntpMultiLineResponse> HdrAsync(
        string field,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.HdrAsync(field, cancellationToken));

    public Task<NntpMultiLineResponse> ListHeadersAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListHeadersAsync(messageId, cancellationToken));

    public Task<NntpMultiLineResponse> ListHeadersAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListHeadersAsync(range, cancellationToken));

    public Task<NntpMultiLineResponse> ListHeadersAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListHeadersAsync(cancellationToken));

    public Task<NntpGroupsResponse> ListCountsAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListCountsAsync(cancellationToken));

    public Task<NntpGroupsResponse> ListCountsAsync(
        string wildmat,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListCountsAsync(wildmat, cancellationToken));

    public Task<NntpMultiLineResponse> ListDistributionsAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListDistributionsAsync(cancellationToken));

    public Task<NntpMultiLineResponse> ListModeratorsAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListModeratorsAsync(cancellationToken));

    public Task<NntpMultiLineResponse> ListMotdAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListMotdAsync(cancellationToken));

    public Task<NntpMultiLineResponse> ListSubscriptionsAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListSubscriptionsAsync(cancellationToken));

    public Task<NntpGroupsResponse> ListActiveAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListActiveAsync(cancellationToken));

    public Task<NntpGroupsResponse> ListActiveAsync(
        string wildmat,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListActiveAsync(wildmat, cancellationToken));

    #endregion

    public void Dispose()
    {
        if (_disposed)
            return;

#pragma warning disable CA1031
        try
        {
            // Try to gracefully QUIT the NNTP session
            if (NntpConnected)
                _ = _client.QuitAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // It's possible that the connection is already closed or broken.
            // We can ignore any exceptions here as we're disposing anyway.
        }
#pragma warning restore CA1031

        NntpConnected = false;
        Authenticated = false;
        _connection.Dispose();

        _disposed = true;
    }

    private NntpClient Client
    {
        get
        {
            ObjectDisposedExceptionShims.ThrowIf(_disposed, _client);

            if (!Connected || !Authenticated)
                throw new InvalidOperationException("Client not connected or authenticated");

            LastActivity = DateTimeOffset.Now;

            return _client;
        }
    }

    private async Task<T> ExecuteCommandAsync<T>(Func<NntpClient, Task<T>> command)
    {
        try
        {
            return await command(Client).ConfigureAwait(false);
        }
        catch (Exception)
        {
            HasError = true;
            throw;
        }
    }
}
