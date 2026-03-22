using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp;

public partial class NntpClient
{
    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> XhdrAsync(string field, NntpMessageId messageId) =>
        XhdrAsync(field, messageId, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> XhdrAsync(string field, NntpArticleRange range) =>
        XhdrAsync(field, range, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> XhdrAsync(string field) =>
        XhdrAsync(field, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> XoverAsync(NntpArticleRange range) =>
        XoverAsync(range, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> XoverAsync() => XoverAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> CapabilitiesAsync() =>
        CapabilitiesAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> CapabilitiesAsync(string keyword) =>
        CapabilitiesAsync(keyword, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpModeReaderResponse> ModeReaderAsync() =>
        ModeReaderAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpGroupResponse> GroupAsync(string group) =>
        GroupAsync(group, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpGroupResponse> ListGroupAsync(string group, NntpArticleRange range) =>
        ListGroupAsync(group, range, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpGroupResponse> ListGroupAsync(string group) =>
        ListGroupAsync(group, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpGroupResponse> ListGroupAsync() => ListGroupAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpLastResponse> LastAsync() => LastAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpNextResponse> NextAsync() => NextAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpArticleResponse> ArticleAsync(NntpMessageId messageId) =>
        ArticleAsync(messageId, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpArticleResponse> ArticleAsync(long number) =>
        ArticleAsync(number, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpArticleResponse> ArticleAsync() => ArticleAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpArticleResponse> HeadAsync(NntpMessageId messageId) =>
        HeadAsync(messageId, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpArticleResponse> HeadAsync(long number) =>
        HeadAsync(number, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpArticleResponse> HeadAsync() => HeadAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpArticleResponse> BodyAsync(NntpMessageId messageId) =>
        BodyAsync(messageId, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpArticleResponse> BodyAsync(long number) =>
        BodyAsync(number, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpArticleResponse> BodyAsync() => BodyAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpStatResponse> StatAsync(NntpMessageId messageId) =>
        StatAsync(messageId, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpStatResponse> StatAsync(long number) =>
        StatAsync(number, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpStatResponse> StatAsync() => StatAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<bool> PostAsync(NntpArticle article) => PostAsync(article, CancellationToken.None);

    /// <inheritdoc/>
    public Task<bool> IhaveAsync(NntpArticle article) =>
        IhaveAsync(article, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpDateResponse> DateAsync() => DateAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> HelpAsync() => HelpAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpGroupsResponse> NewGroupsAsync(NntpDateTime sinceDateTime) =>
        NewGroupsAsync(sinceDateTime, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> NewNewsAsync(string wildmat, NntpDateTime sinceDateTime) =>
        NewNewsAsync(wildmat, sinceDateTime, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpGroupOriginsResponse> ListActiveTimesAsync() =>
        ListActiveTimesAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpGroupOriginsResponse> ListActiveTimesAsync(string wildmat) =>
        ListActiveTimesAsync(wildmat, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> ListDistribPatsAsync() =>
        ListDistribPatsAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> ListNewsgroupsAsync() =>
        ListNewsgroupsAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> ListNewsgroupsAsync(string wildmat) =>
        ListNewsgroupsAsync(wildmat, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> OverAsync(NntpMessageId messageId) =>
        OverAsync(messageId, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> OverAsync(NntpArticleRange range) =>
        OverAsync(range, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> OverAsync() => OverAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> ListOverviewFormatAsync() =>
        ListOverviewFormatAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> HdrAsync(string field, NntpMessageId messageId) =>
        HdrAsync(field, messageId, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> HdrAsync(string field, NntpArticleRange range) =>
        HdrAsync(field, range, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> HdrAsync(string field) =>
        HdrAsync(field, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> ListHeadersAsync(NntpMessageId messageId) =>
        ListHeadersAsync(messageId, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> ListHeadersAsync(NntpArticleRange range) =>
        ListHeadersAsync(range, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> ListHeadersAsync() =>
        ListHeadersAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<bool> AuthenticateAsync(string username, string password) =>
        AuthenticateAsync(username, password, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpGroupsResponse> ListCountsAsync() => ListCountsAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpGroupsResponse> ListCountsAsync(string wildmat) =>
        ListCountsAsync(wildmat, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> ListDistributionsAsync() =>
        ListDistributionsAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> ListModeratorsAsync() =>
        ListModeratorsAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> ListMotdAsync() => ListMotdAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> ListSubscriptionsAsync() =>
        ListSubscriptionsAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpGroupsResponse> ListActiveAsync() => ListActiveAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpGroupsResponse> ListActiveAsync(string wildmat) =>
        ListActiveAsync(wildmat, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpResponse> XfeatureCompressGzipAsync(bool withTerminator) =>
        XfeatureCompressGzipAsync(withTerminator, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> XzhdrAsync(string field, NntpMessageId messageId) =>
        XzhdrAsync(field, messageId, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> XzhdrAsync(string field, NntpArticleRange range) =>
        XzhdrAsync(field, range, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> XzhdrAsync(string field) =>
        XzhdrAsync(field, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> XzverAsync(NntpArticleRange range) =>
        XzverAsync(range, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpMultiLineResponse> XzverAsync() => XzverAsync(CancellationToken.None);

    /// <inheritdoc/>
    public Task<bool> ConnectAsync(string hostname, int port, bool useSsl) =>
        ConnectAsync(hostname, port, useSsl, CancellationToken.None);

    /// <inheritdoc/>
    public Task<NntpResponse> QuitAsync() => QuitAsync(CancellationToken.None);
}
