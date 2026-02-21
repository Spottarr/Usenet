using Usenet.Extensions;
using Usenet.Nntp.Contracts;
using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;
using Usenet.Nntp.Writers;
using Usenet.Util;

namespace Usenet.Nntp;

/// <summary>
/// An NNTP client that is compliant with
/// <a href="https://tools.ietf.org/html/rfc2980">RFC 2980</a>,
/// <a href="https://tools.ietf.org/html/rfc3977">RFC 3977</a>,
/// <a href="https://tools.ietf.org/html/rfc4643">RFC 4643</a> and
/// <a href="https://tools.ietf.org/html/rfc6048">RFC 6048</a>.
/// Based on Kristian Hellang's NntpLib.Net project https://github.com/khellang/NntpLib.Net.
/// </summary>
public class NntpClient : INntpClient
{
    private readonly INntpConnection _connection;
    protected INntpConnection Connection => _connection;

    /// <summary>
    /// Creates a new instance of the <see cref="NntpClient"/> class.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    public NntpClient(INntpConnection connection)
    {
        _connection = connection.ThrowIfNull(nameof(connection));
    }

    /// <summary>
    /// The number of bytes read.
    /// </summary>
    public long BytesRead => _connection.Stream?.BytesRead ?? 0;

    /// <summary>
    /// The number of bytes written.
    /// </summary>
    public long BytesWritten => _connection.Stream?.BytesWritten ?? 0;

    /// <summary>
    /// Resets the counters.
    /// </summary>
    public void ResetCounters()
    {
        _connection.Stream?.ResetCounters();
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync(string hostname, int port, bool useSsl, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(hostname, nameof(hostname));
        var response = await _connection.ConnectAsync(hostname, port, useSsl, new ResponseParser(200, 201), cancellationToken).ConfigureAwait(false);
        return response.Success;
    }

    /// <inheritdoc />
    public async Task<bool> AuthenticateAsync(string username, string password = "", CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNullOrWhiteSpace(username, nameof(username));
        var userResponse = await _connection.CommandAsync($"AUTHINFO USER {username}", new ResponseParser(281), cancellationToken).ConfigureAwait(false);
        if (userResponse.Success)
        {
            return true;
        }

        if (userResponse.Code != 381 || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var passResponse = await _connection.CommandAsync($"AUTHINFO PASS {password}", new ResponseParser(281), cancellationToken).ConfigureAwait(false);
        return passResponse.Success;
    }

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> CapabilitiesAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("CAPABILITIES", new MultiLineResponseParser(101), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> CapabilitiesAsync(string keyword, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"CAPABILITIES {keyword.ThrowIfNullOrWhiteSpace(nameof(keyword))}", new MultiLineResponseParser(101), cancellationToken);

    /// <inheritdoc />
    public Task<NntpModeReaderResponse> ModeReaderAsync(CancellationToken cancellationToken = default) =>
        _connection.CommandAsync("MODE READER", new ModeReaderResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpResponse> QuitAsync(CancellationToken cancellationToken = default) =>
        _connection.CommandAsync("QUIT", new ResponseParser(205), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupResponse> GroupAsync(string group, CancellationToken cancellationToken = default) =>
        _connection.CommandAsync($"GROUP {group.ThrowIfNullOrWhiteSpace(nameof(group))}", new GroupResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupResponse> ListGroupAsync(string group, NntpArticleRange range, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"LISTGROUP {group.ThrowIfNullOrWhiteSpace(nameof(group))} {range}", new ListGroupResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupResponse> ListGroupAsync(string group, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"LISTGROUP {group.ThrowIfNullOrWhiteSpace(nameof(group))}", new ListGroupResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupResponse> ListGroupAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LISTGROUP", new ListGroupResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpLastResponse> LastAsync(CancellationToken cancellationToken = default) =>
        _connection.CommandAsync("LAST", new LastResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpNextResponse> NextAsync(CancellationToken cancellationToken = default) =>
        _connection.CommandAsync("NEXT", new NextResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpArticleResponse> ArticleAsync(NntpMessageId messageId, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync(
            $"ARTICLE {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Article), cancellationToken);

    /// <inheritdoc />
    public Task<NntpArticleResponse> ArticleAsync(long number, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"ARTICLE {number}", new ArticleResponseParser(ArticleRequestType.Article), cancellationToken);

    /// <inheritdoc />
    public Task<NntpArticleResponse> ArticleAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("ARTICLE", new ArticleResponseParser(ArticleRequestType.Article), cancellationToken);

    /// <inheritdoc />
    public Task<NntpArticleResponse> HeadAsync(NntpMessageId messageId, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync(
            $"HEAD {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Head), cancellationToken);

    /// <inheritdoc />
    public Task<NntpArticleResponse> HeadAsync(long number, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"HEAD {number}", new ArticleResponseParser(ArticleRequestType.Head), cancellationToken);

    /// <inheritdoc />
    public Task<NntpArticleResponse> HeadAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("HEAD", new ArticleResponseParser(ArticleRequestType.Head), cancellationToken);

    /// <inheritdoc />
    public Task<NntpArticleResponse> BodyAsync(NntpMessageId messageId, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync(
            $"BODY {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Body), cancellationToken);

    /// <inheritdoc />
    public Task<NntpArticleResponse> BodyAsync(long number, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"BODY {number}", new ArticleResponseParser(ArticleRequestType.Body), cancellationToken);

    /// <inheritdoc />
    public Task<NntpArticleResponse> BodyAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("BODY", new ArticleResponseParser(ArticleRequestType.Body), cancellationToken);

    /// <inheritdoc />
    public Task<NntpStatResponse> StatAsync(NntpMessageId messageId, CancellationToken cancellationToken = default) =>
        _connection.CommandAsync($"STAT {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}", new StatResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpStatResponse> StatAsync(long number, CancellationToken cancellationToken = default) =>
        _connection.CommandAsync($"STAT {number}", new StatResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpStatResponse> StatAsync(CancellationToken cancellationToken = default) =>
        _connection.CommandAsync("STAT", new StatResponseParser(), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> PostAsync(NntpArticle article, CancellationToken cancellationToken = default)
    {
        var initialResponse = await _connection.CommandAsync("POST", new ResponseParser(340), cancellationToken).ConfigureAwait(false);
        if (!initialResponse.Success)
        {
            return false;
        }

        await ArticleWriter.WriteAsync(_connection, article, cancellationToken).ConfigureAwait(false);
        var subsequentResponse = await _connection.GetResponseAsync(new ResponseParser(240), cancellationToken).ConfigureAwait(false);
        return subsequentResponse.Success;
    }

    /// <inheritdoc />
    public async Task<bool> IhaveAsync(NntpArticle article, CancellationToken cancellationToken = default)
    {
        var initialResponse = await _connection.CommandAsync("IHAVE", new ResponseParser(335), cancellationToken).ConfigureAwait(false);
        if (!initialResponse.Success)
        {
            return false;
        }

        await ArticleWriter.WriteAsync(_connection, article, cancellationToken).ConfigureAwait(false);
        var subsequentResponse = await _connection.GetResponseAsync(new ResponseParser(235), cancellationToken).ConfigureAwait(false);
        return subsequentResponse.Success;
    }

    /// <inheritdoc />
    public Task<NntpDateResponse> DateAsync(CancellationToken cancellationToken = default) =>
        _connection.CommandAsync("DATE", new DateResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> HelpAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("HELP", new MultiLineResponseParser(100), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupsResponse> NewGroupsAsync(NntpDateTime sinceDateTime, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"NEWGROUPS {sinceDateTime}", new GroupsResponseParser(231, GroupStatusRequestType.Basic), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> NewNewsAsync(string wildmat, NntpDateTime sinceDateTime, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"NEWNEWS {wildmat} {sinceDateTime}", new MultiLineResponseParser(230), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupOriginsResponse> ListActiveTimesAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LIST ACTIVE.TIMES", new GroupOriginsResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupOriginsResponse> ListActiveTimesAsync(string wildmat, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"LIST ACTIVE.TIMES {wildmat}", new GroupOriginsResponseParser(), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListDistribPatsAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LIST DISTRIB.PATS", new MultiLineResponseParser(215), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListNewsgroupsAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LIST NEWSGROUPS", new MultiLineResponseParser(215), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListNewsgroupsAsync(string wildmat, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"LIST NEWSGROUPS {wildmat}", new MultiLineResponseParser(215), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> OverAsync(NntpMessageId messageId, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"OVER {messageId}", new MultiLineResponseParser(224), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> OverAsync(NntpArticleRange range, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"OVER {range}", new MultiLineResponseParser(224), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> OverAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("OVER", new MultiLineResponseParser(224), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListOverviewFormatAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LIST OVERVIEW.FMT", new MultiLineResponseParser(215), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> HdrAsync(string field, NntpMessageId messageId, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"HDR {field} {messageId}", new MultiLineResponseParser(225), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> HdrAsync(string field, NntpArticleRange range, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"HDR {field} {range}", new MultiLineResponseParser(225), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> HdrAsync(string field, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"HDR {field}", new MultiLineResponseParser(225), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListHeadersAsync(NntpMessageId messageId, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"LIST HEADERS {messageId}", new MultiLineResponseParser(215), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListHeadersAsync(NntpArticleRange range, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"LIST HEADERS {range}", new MultiLineResponseParser(215), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListHeadersAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LIST HEADERS", new MultiLineResponseParser(215), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XhdrAsync(string field, NntpMessageId messageId, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"XHDR {field} {messageId}", new MultiLineResponseParser(221), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XhdrAsync(string field, NntpArticleRange range, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"XHDR {field} {range}", new MultiLineResponseParser(221), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XhdrAsync(string field, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"XHDR {field}", new MultiLineResponseParser(221), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XoverAsync(NntpArticleRange range, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"XOVER {range}", new MultiLineResponseParser(224), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XoverAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("XOVER", new MultiLineResponseParser(224), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupsResponse> ListCountsAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LIST COUNTS", new GroupsResponseParser(215, GroupStatusRequestType.Extended), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupsResponse> ListCountsAsync(string wildmat, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"LIST COUNTS {wildmat}", new GroupsResponseParser(215, GroupStatusRequestType.Extended), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListDistributionsAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LIST DISTRIBUTIONS", new MultiLineResponseParser(215), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListModeratorsAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LIST MODERATORS", new MultiLineResponseParser(215), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListMotdAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LIST MOTD", new MultiLineResponseParser(215), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListSubscriptionsAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LIST SUBSCRIPTIONS", new MultiLineResponseParser(215), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupsResponse> ListActiveAsync(CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync("LIST ACTIVE", new GroupsResponseParser(215, GroupStatusRequestType.Basic), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupsResponse> ListActiveAsync(string wildmat, CancellationToken cancellationToken = default) =>
        _connection.MultiLineCommandAsync($"LIST ACTIVE {wildmat}", new GroupsResponseParser(215, GroupStatusRequestType.Basic), cancellationToken);

    /// <inheritdoc />
    public Task<NntpResponse> XfeatureCompressGzipAsync(bool withTerminator, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XzhdrAsync(string field, NntpMessageId messageId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XzhdrAsync(string field, NntpArticleRange range, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XzhdrAsync(string field, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XzverAsync(NntpArticleRange range, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XzverAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
