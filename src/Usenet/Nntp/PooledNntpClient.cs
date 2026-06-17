using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Usenet.Nntp.Contracts;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

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
    public bool HasPendingStream => _connection.HasPendingStream;

    public PooledNntpClient(ILoggerFactory? loggerFactory = null)
    {
        _connection = new NntpConnection(loggerFactory);
        _client = new NntpClient(_connection, loggerFactory);
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
        string password,
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

    public Task<NntpStreamResponse<NntpHeaderField>> XzhdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XzhdrAsync(field, range, cancellationToken));

    public Task<NntpHeaderField?> XzhdrByMessageIdAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XzhdrByMessageIdAsync(field, messageId, cancellationToken));

    public Task<NntpStreamResponse<NntpHeaderField>> CurrentXzhdrAsync(
        string field,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CurrentXzhdrAsync(field, cancellationToken));

    public Task<NntpStreamResponse<NntpArticleOverview>> XzverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XzverAsync(range, cancellationToken));

    public Task<NntpStreamResponse<NntpArticleOverview>> CurrentXzverAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CurrentXzverAsync(cancellationToken));

    public void ResetCounters() => _client.ResetCounters();

    public Task<NntpStreamResponse<NntpHeaderField>> XhdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XhdrAsync(field, range, cancellationToken));

    public Task<NntpHeaderField?> XhdrByMessageIdAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XhdrByMessageIdAsync(field, messageId, cancellationToken));

    public Task<NntpStreamResponse<NntpHeaderField>> CurrentXhdrAsync(
        string field,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CurrentXhdrAsync(field, cancellationToken));

    public Task<NntpStreamResponse<NntpArticleOverview>> XoverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.XoverAsync(range, cancellationToken));

    public Task<NntpStreamResponse<NntpArticleOverview>> CurrentXoverAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CurrentXoverAsync(cancellationToken));

    public Task<NntpCapabilities> CapabilitiesAsync(
        string? keyword = null,
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

    public Task<NntpStreamResponse<long>> ListGroupAsync(
        string? group = null,
        NntpArticleRange? range = null,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListGroupAsync(group, range, cancellationToken));

    public Task<NntpLastResponse> LastAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.LastAsync(cancellationToken));

    public Task<NntpNextResponse> NextAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.NextAsync(cancellationToken));

    public Task<NntpArticleResponse> ArticleAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ArticleAsync(messageId, cancellationToken));

    public Task<NntpArticleResponse> ArticleByNumberAsync(
        long number,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ArticleByNumberAsync(number, cancellationToken));

    public Task<NntpArticleResponse> CurrentArticleAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CurrentArticleAsync(cancellationToken));

    public Task<NntpArticleResponse> HeadAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.HeadAsync(messageId, cancellationToken));

    public Task<NntpArticleResponse> HeadByNumberAsync(
        long number,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.HeadByNumberAsync(number, cancellationToken));

    public Task<NntpArticleResponse> CurrentHeadAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CurrentHeadAsync(cancellationToken));

    public Task<NntpArticleResponse> BodyAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.BodyAsync(messageId, cancellationToken));

    public Task<NntpArticleResponse> BodyByNumberAsync(
        long number,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.BodyByNumberAsync(number, cancellationToken));

    public Task<NntpArticleResponse> CurrentBodyAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CurrentBodyAsync(cancellationToken));

    public Task<NntpStatResponse> StatAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.StatAsync(messageId, cancellationToken));

    public Task<NntpStatResponse> StatByNumberAsync(
        long number,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.StatByNumberAsync(number, cancellationToken));

    public Task<NntpStatResponse> CurrentStatAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.CurrentStatAsync(cancellationToken));

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

    public Task<NntpTextResponse> HelpAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.HelpAsync(cancellationToken));

    public Task<NntpGroupsResponse> NewGroupsAsync(
        NntpDateTime sinceDateTime,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.NewGroupsAsync(sinceDateTime, cancellationToken));

    public Task<NntpStreamResponse<NntpMessageId>> NewNewsAsync(
        string wildmat,
        NntpDateTime sinceDateTime,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.NewNewsAsync(wildmat, sinceDateTime, cancellationToken));

    public Task<NntpGroupOriginsResponse> ListActiveTimesAsync(
        string? wildmat = null,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListActiveTimesAsync(wildmat, cancellationToken));

    public Task<IImmutableList<NntpDistributionPattern>> ListDistribPatsAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListDistribPatsAsync(cancellationToken));

    public Task<NntpStreamResponse<NntpNewsgroupDescription>> ListNewsgroupsAsync(
        string? wildmat = null,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListNewsgroupsAsync(wildmat, cancellationToken));

    public Task<NntpStreamResponse<NntpArticleOverview>> OverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.OverAsync(range, cancellationToken));

    public Task<NntpArticleOverview?> OverByMessageIdAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.OverByMessageIdAsync(messageId, cancellationToken));

    public Task<NntpStreamResponse<NntpArticleOverview>> CurrentOverAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CurrentOverAsync(cancellationToken));

    public Task<NntpOverviewFormat> ListOverviewFormatAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListOverviewFormatAsync(cancellationToken));

    public Task<NntpStreamResponse<NntpHeaderField>> HdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.HdrAsync(field, range, cancellationToken));

    public Task<NntpHeaderField?> HdrByMessageIdAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.HdrByMessageIdAsync(field, messageId, cancellationToken));

    public Task<NntpStreamResponse<NntpHeaderField>> CurrentHdrAsync(
        string field,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CurrentHdrAsync(field, cancellationToken));

    public Task<NntpTextResponse> ListHeadersAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListHeadersAsync(range, cancellationToken));

    public Task<NntpTextResponse> ListHeadersByMessageIdAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListHeadersByMessageIdAsync(messageId, cancellationToken));

    public Task<NntpTextResponse> CurrentListHeadersAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.CurrentListHeadersAsync(cancellationToken));

    public Task<NntpGroupsResponse> ListCountsAsync(
        string? wildmat = null,
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListCountsAsync(wildmat, cancellationToken));

    public Task<IImmutableList<NntpDistribution>> ListDistributionsAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListDistributionsAsync(cancellationToken));

    public Task<IImmutableList<NntpModerator>> ListModeratorsAsync(
        CancellationToken cancellationToken = default
    ) => ExecuteCommandAsync(c => c.ListModeratorsAsync(cancellationToken));

    public Task<NntpTextResponse> ListMotdAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.ListMotdAsync(cancellationToken));

    public Task<NntpGroups> ListSubscriptionsAsync(CancellationToken cancellationToken = default) =>
        ExecuteCommandAsync(c => c.ListSubscriptionsAsync(cancellationToken));

    public Task<NntpStreamResponse<NntpGroup>> ListActiveAsync(
        string? wildmat = null,
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
            ObjectDisposedException.ThrowIf(_disposed, _client);

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
