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
    public async Task<bool> ConnectAsync(string hostname, int port, bool useSsl)
    {
        Guard.ThrowIfNullOrWhiteSpace(hostname, nameof(hostname));
        var response = await _connection.ConnectAsync(hostname, port, useSsl, new ResponseParser(200, 201)).ConfigureAwait(false);
        return response.Success;
    }

    /// <inheritdoc />
    public bool Authenticate(string username, string password = null)
    {
        Guard.ThrowIfNullOrWhiteSpace(username, nameof(username));
        var userResponse = _connection.Command($"AUTHINFO USER {username}", new ResponseParser(281));
        if (userResponse.Success)
        {
            return true;
        }

        if (userResponse.Code != 381 || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var passResponse = _connection.Command($"AUTHINFO PASS {password}", new ResponseParser(281));
        return passResponse.Success;
    }

    /// <inheritdoc />
    public NntpMultiLineResponse Capabilities() => _connection.MultiLineCommand("CAPABILITIES", new MultiLineResponseParser(101));

    /// <inheritdoc />
    public NntpMultiLineResponse Capabilities(string keyword) =>
        _connection.MultiLineCommand($"CAPABILITIES {keyword.ThrowIfNullOrWhiteSpace(nameof(keyword))}", new MultiLineResponseParser(101));

    /// <inheritdoc />
    public NntpModeReaderResponse ModeReader() => _connection.Command("MODE READER", new ModeReaderResponseParser());

    /// <inheritdoc />
    public NntpResponse Quit() => _connection.Command("QUIT", new ResponseParser(205));

    /// <inheritdoc />
    public NntpGroupResponse Group(string group) =>
        _connection.Command($"GROUP {group.ThrowIfNullOrWhiteSpace(nameof(group))}", new GroupResponseParser());

    /// <inheritdoc />
    public NntpGroupResponse ListGroup(string group, NntpArticleRange range) =>
        _connection.MultiLineCommand($"LISTGROUP {group.ThrowIfNullOrWhiteSpace(nameof(group))} {range}", new ListGroupResponseParser());

    /// <inheritdoc />
    public NntpGroupResponse ListGroup(string group) =>
        _connection.MultiLineCommand($"LISTGROUP {group.ThrowIfNullOrWhiteSpace(nameof(group))}", new ListGroupResponseParser());

    /// <inheritdoc />
    public NntpGroupResponse ListGroup() =>
        _connection.MultiLineCommand("LISTGROUP", new ListGroupResponseParser());

    /// <inheritdoc />
    public NntpLastResponse Last() => _connection.Command("LAST", new LastResponseParser());

    /// <inheritdoc />
    public NntpNextResponse Next() => _connection.Command("NEXT", new NextResponseParser());

    /// <inheritdoc />
    public NntpArticleResponse Article(NntpMessageId messageId) =>
        _connection.MultiLineCommand(
            $"ARTICLE {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Article));

    /// <inheritdoc />
    public NntpArticleResponse Article(long number) =>
        _connection.MultiLineCommand($"ARTICLE {number}", new ArticleResponseParser(ArticleRequestType.Article));

    /// <inheritdoc />
    public NntpArticleResponse Article() =>
        _connection.MultiLineCommand("ARTICLE", new ArticleResponseParser(ArticleRequestType.Article));

    /// <inheritdoc />
    public NntpArticleResponse Head(NntpMessageId messageId) =>
        _connection.MultiLineCommand(
            $"HEAD {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Head));

    /// <inheritdoc />
    public NntpArticleResponse Head(long number) =>
        _connection.MultiLineCommand($"HEAD {number}", new ArticleResponseParser(ArticleRequestType.Head));

    /// <inheritdoc />
    public NntpArticleResponse Head() =>
        _connection.MultiLineCommand("HEAD", new ArticleResponseParser(ArticleRequestType.Head));

    /// <inheritdoc />
    public NntpArticleResponse Body(NntpMessageId messageId) =>
        _connection.MultiLineCommand(
            $"BODY {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Body));

    /// <inheritdoc />
    public NntpArticleResponse Body(long number) =>
        _connection.MultiLineCommand($"BODY {number}", new ArticleResponseParser(ArticleRequestType.Body));

    /// <inheritdoc />
    public NntpArticleResponse Body() =>
        _connection.MultiLineCommand("BODY", new ArticleResponseParser(ArticleRequestType.Body));

    /// <inheritdoc />
    public NntpStatResponse Stat(NntpMessageId messageId) =>
        _connection.Command($"STAT {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}", new StatResponseParser());

    /// <inheritdoc />
    public NntpStatResponse Stat(long number) => _connection.Command($"STAT {number}", new StatResponseParser());

    /// <inheritdoc />
    public NntpStatResponse Stat() => _connection.Command("STAT", new StatResponseParser());

    /// <inheritdoc />
    public bool Post(NntpArticle article)
    {
        var initialResponse = _connection.Command("POST", new ResponseParser(340));
        if (!initialResponse.Success)
        {
            return false;
        }

        ArticleWriter.Write(_connection, article);
        var subsequentResponse = _connection.GetResponse(new ResponseParser(240));
        return subsequentResponse.Success;
    }

    /// <inheritdoc />
    public bool Ihave(NntpArticle article)
    {
        var initialResponse = _connection.Command("IHAVE", new ResponseParser(335));
        if (!initialResponse.Success)
        {
            return false;
        }

        ArticleWriter.Write(_connection, article);
        var subsequentResponse = _connection.GetResponse(new ResponseParser(235));
        return subsequentResponse.Success;
    }

    /// <inheritdoc />
    public NntpDateResponse Date() => _connection.Command("DATE", new DateResponseParser());

    /// <inheritdoc />
    public NntpMultiLineResponse Help() =>
        _connection.MultiLineCommand("HELP", new MultiLineResponseParser(100));

    /// <inheritdoc />
    public NntpGroupsResponse NewGroups(NntpDateTime sinceDateTime) =>
        _connection.MultiLineCommand($"NEWGROUPS {sinceDateTime}", new GroupsResponseParser(231, GroupStatusRequestType.Basic));

    /// <inheritdoc />
    public NntpMultiLineResponse NewNews(string wildmat, NntpDateTime sinceDateTime) =>
        _connection.MultiLineCommand($"NEWNEWS {wildmat} {sinceDateTime}", new MultiLineResponseParser(230));

    /// <inheritdoc />
    public NntpGroupOriginsResponse ListActiveTimes() =>
        _connection.MultiLineCommand("LIST ACTIVE.TIMES", new GroupOriginsResponseParser());

    /// <inheritdoc />
    public NntpGroupOriginsResponse ListActiveTimes(string wildmat) =>
        _connection.MultiLineCommand($"LIST ACTIVE.TIMES {wildmat}", new GroupOriginsResponseParser());

    /// <inheritdoc />
    public NntpMultiLineResponse ListDistribPats() =>
        _connection.MultiLineCommand("LIST DISTRIB.PATS", new MultiLineResponseParser(215));

    /// <inheritdoc />
    public NntpMultiLineResponse ListNewsgroups() =>
        _connection.MultiLineCommand("LIST NEWSGROUPS", new MultiLineResponseParser(215));

    /// <inheritdoc />
    public NntpMultiLineResponse ListNewsgroups(string wildmat) =>
        _connection.MultiLineCommand($"LIST NEWSGROUPS {wildmat}", new MultiLineResponseParser(215));

    /// <inheritdoc />
    public NntpMultiLineResponse Over(NntpMessageId messageId) =>
        _connection.MultiLineCommand($"OVER {messageId}", new MultiLineResponseParser(224));

    /// <inheritdoc />
    public NntpMultiLineResponse Over(NntpArticleRange range) =>
        _connection.MultiLineCommand($"OVER {range}", new MultiLineResponseParser(224));

    /// <inheritdoc />
    public NntpMultiLineResponse Over() => _connection.MultiLineCommand("OVER", new MultiLineResponseParser(224));


    /// <inheritdoc />
    public NntpMultiLineResponse ListOverviewFormat() =>
        _connection.MultiLineCommand("LIST OVERVIEW.FMT", new MultiLineResponseParser(215));

    /// <inheritdoc />
    public NntpMultiLineResponse Hdr(string field, NntpMessageId messageId) =>
        _connection.MultiLineCommand($"HDR {field} {messageId}", new MultiLineResponseParser(225));

    /// <inheritdoc />
    public NntpMultiLineResponse Hdr(string field, NntpArticleRange range) =>
        _connection.MultiLineCommand($"HDR {field} {range}", new MultiLineResponseParser(225));

    /// <inheritdoc />
    public NntpMultiLineResponse Hdr(string field) =>
        _connection.MultiLineCommand($"HDR {field}", new MultiLineResponseParser(225));

    /// <inheritdoc />
    public NntpMultiLineResponse ListHeaders(NntpMessageId messageId) =>
        _connection.MultiLineCommand($"LIST HEADERS {messageId}", new MultiLineResponseParser(215));

    /// <inheritdoc />
    public NntpMultiLineResponse ListHeaders(NntpArticleRange range) =>
        _connection.MultiLineCommand($"LIST HEADERS {range}", new MultiLineResponseParser(215));

    /// <inheritdoc />
    public NntpMultiLineResponse ListHeaders() =>
        _connection.MultiLineCommand("LIST HEADERS", new MultiLineResponseParser(215));

    /// <inheritdoc />
    public NntpMultiLineResponse Xhdr(string field, NntpMessageId messageId) =>
        _connection.MultiLineCommand($"XHDR {field} {messageId}", new MultiLineResponseParser(221));

    /// <inheritdoc />
    public NntpMultiLineResponse Xhdr(string field, NntpArticleRange range) =>
        _connection.MultiLineCommand($"XHDR {field} {range}", new MultiLineResponseParser(221));

    /// <inheritdoc />
    public NntpMultiLineResponse Xhdr(string field) =>
        _connection.MultiLineCommand($"XHDR {field}", new MultiLineResponseParser(221));

    /// <inheritdoc />
    public NntpMultiLineResponse Xover(NntpArticleRange range) =>
        _connection.MultiLineCommand($"XOVER {range}", new MultiLineResponseParser(224));

    /// <inheritdoc />
    public NntpMultiLineResponse Xover() => _connection.MultiLineCommand("XOVER", new MultiLineResponseParser(224));

    /// <inheritdoc />
    public NntpGroupsResponse ListCounts() =>
        _connection.MultiLineCommand("LIST COUNTS", new GroupsResponseParser(215, GroupStatusRequestType.Extended));

    /// <inheritdoc />
    public NntpGroupsResponse ListCounts(string wildmat) =>
        _connection.MultiLineCommand($"LIST COUNTS {wildmat}", new GroupsResponseParser(215, GroupStatusRequestType.Extended));

    /// <inheritdoc />
    public NntpMultiLineResponse ListDistributions() =>
        _connection.MultiLineCommand("LIST DISTRIBUTIONS", new MultiLineResponseParser(215));

    /// <inheritdoc />
    public NntpMultiLineResponse ListModerators() =>
        _connection.MultiLineCommand("LIST MODERATORS", new MultiLineResponseParser(215));

    /// <inheritdoc />
    public NntpMultiLineResponse ListMotd() =>
        _connection.MultiLineCommand("LIST MOTD", new MultiLineResponseParser(215));

    /// <inheritdoc />
    public NntpMultiLineResponse ListSubscriptions() =>
        _connection.MultiLineCommand("LIST SUBSCRIPTIONS", new MultiLineResponseParser(215));

    /// <inheritdoc />
    public NntpGroupsResponse ListActive() =>
        _connection.MultiLineCommand("LIST ACTIVE", new GroupsResponseParser(215, GroupStatusRequestType.Basic));

    /// <inheritdoc />
    public NntpGroupsResponse ListActive(string wildmat) =>
        _connection.MultiLineCommand($"LIST ACTIVE {wildmat}", new GroupsResponseParser(215, GroupStatusRequestType.Basic));

    /// <inheritdoc />
    public NntpResponse XfeatureCompressGzip(bool withTerminator) => throw new NotImplementedException();
    //connection.Command($"XFEATURE COMPRESS GZIP{(withTerminator ? " TERMINATOR" : string.Empty)}", new ResponseParser(290));

    /// <inheritdoc />
    public NntpMultiLineResponse Xzhdr(string field, NntpMessageId messageId) => throw new NotImplementedException();
    //connection.MultiLineCommand($"XZHDR {field} {messageId}", new MultiLineResponseParser(221), true);

    /// <inheritdoc />
    public NntpMultiLineResponse Xzhdr(string field, NntpArticleRange range) => throw new NotImplementedException();
    //connection.MultiLineCommand($"XZHDR {field} {RangeFormatter.Format(from, to)}", new MultiLineResponseParser(221), true);

    /// <inheritdoc />
    public NntpMultiLineResponse Xzhdr(string field) => throw new NotImplementedException();
    //connection.MultiLineCommand($"XZHDR {field}", new MultiLineResponseParser(221), true);

    /// <inheritdoc />
    public NntpMultiLineResponse Xzver(NntpArticleRange range) => throw new NotImplementedException();
    //connection.MultiLineCommand($"XZVER {RangeFormatter.Format(from, to)}", new MultiLineResponseParser(224), true);

    /// <inheritdoc />
    public NntpMultiLineResponse Xzver() => throw new NotImplementedException();
    //connection.MultiLineCommand("XZVER", new MultiLineResponseParser(224), true);
}
