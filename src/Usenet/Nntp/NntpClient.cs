using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usenet.Extensions;
using Usenet.Nntp.Contracts;
using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;
using Usenet.Nntp.Writers;

namespace Usenet.Nntp;

/// <summary>
/// An NNTP client that is compliant with
/// <a href="https://tools.ietf.org/html/rfc2980">RFC 2980</a>,
/// <a href="https://tools.ietf.org/html/rfc3977">RFC 3977</a>,
/// <a href="https://tools.ietf.org/html/rfc4643">RFC 4643</a> and
/// <a href="https://tools.ietf.org/html/rfc6048">RFC 6048</a>.
/// Based on Kristian Hellang's NntpLib.Net project https://github.com/khellang/NntpLib.Net.
/// </summary>
[PublicAPI]
public partial class NntpClient : INntpClient
{
    protected INntpConnection Connection { get; }

    private readonly ILoggerFactory _loggerFactory;
    private readonly NntpStreamLineParsers _streamLineParsers;

    /// <summary>
    /// Creates a new instance of the <see cref="NntpClient"/> class.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="loggerFactory">
    /// An optional <see cref="ILoggerFactory"/> threaded through to the response parsers.
    /// When <see langword="null"/>, logging is disabled via <see cref="NullLoggerFactory"/>.
    /// </param>
    public NntpClient(INntpConnection connection, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        Connection = connection;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _streamLineParsers = new NntpStreamLineParsers(_loggerFactory);
    }

    /// <summary>
    /// The number of bytes read.
    /// </summary>
    public long BytesRead => Connection.BytesRead;

    /// <summary>
    /// The number of bytes written.
    /// </summary>
    public long BytesWritten => Connection.BytesWritten;

    /// <summary>
    /// Resets the counters.
    /// </summary>
    public void ResetCounters()
    {
        Connection.ResetCounters();
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync(
        string hostname,
        int port,
        bool useSsl,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        var response = await Connection
            .ConnectAsync(hostname, port, useSsl, new ResponseParser(200, 201), cancellationToken)
            .ConfigureAwait(false);
        return response.Success;
    }

    /// <inheritdoc />
    public async Task<bool> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        var userResponse = await Connection
            .CommandAsync($"AUTHINFO USER {username}", new ResponseParser(281), cancellationToken)
            .ConfigureAwait(false);
        if (userResponse.Success)
        {
            return true;
        }

        if (userResponse.Code != 381 || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var passResponse = await Connection
            .CommandAsync($"AUTHINFO PASS {password}", new ResponseParser(281), cancellationToken)
            .ConfigureAwait(false);
        return passResponse.Success;
    }

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> CapabilitiesAsync(CancellationToken cancellationToken) =>
        Connection.MultiLineCommandAsync(
            "CAPABILITIES",
            new MultiLineResponseParser(101),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> CapabilitiesAsync(
        string keyword,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);
        return Connection.MultiLineCommandAsync(
            $"CAPABILITIES {keyword}",
            new MultiLineResponseParser(101),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<NntpModeReaderResponse> ModeReaderAsync(CancellationToken cancellationToken) =>
        Connection.CommandAsync(
            "MODE READER",
            new ModeReaderResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpResponse> QuitAsync(CancellationToken cancellationToken) =>
        Connection.CommandAsync("QUIT", new ResponseParser(205), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupResponse> GroupAsync(string group, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        return Connection.CommandAsync(
            $"GROUP {group}",
            new GroupResponseParser(_loggerFactory),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<NntpStreamResponse<long>> ListGroupAsync(
        string group,
        NntpArticleRange range,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        return Connection.MultiLineStreamCommandAsync<long>(
            $"LISTGROUP {group} {range}",
            211,
            NntpStreamLineParsers.ArticleNumber,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<NntpStreamResponse<long>> ListGroupAsync(
        string group,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        return Connection.MultiLineStreamCommandAsync<long>(
            $"LISTGROUP {group}",
            211,
            NntpStreamLineParsers.ArticleNumber,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<NntpStreamResponse<long>> ListGroupAsync(CancellationToken cancellationToken) =>
        Connection.MultiLineStreamCommandAsync<long>(
            "LISTGROUP",
            211,
            NntpStreamLineParsers.ArticleNumber,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpLastResponse> LastAsync(CancellationToken cancellationToken) =>
        Connection.CommandAsync("LAST", new LastResponseParser(_loggerFactory), cancellationToken);

    /// <inheritdoc />
    public Task<NntpNextResponse> NextAsync(CancellationToken cancellationToken) =>
        Connection.CommandAsync("NEXT", new NextResponseParser(_loggerFactory), cancellationToken);

    /// <inheritdoc />
    public Task<NntpArticleResponse> ArticleAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            $"ARTICLE {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Article, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> ArticleAsync(
        long number,
        CancellationToken cancellationToken
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            $"ARTICLE {number}",
            new ArticleResponseParser(ArticleRequestType.Article, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> ArticleAsync(CancellationToken cancellationToken) =>
        Connection.BufferedMultiLineCommandAsync(
            "ARTICLE",
            new ArticleResponseParser(ArticleRequestType.Article, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> HeadAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            $"HEAD {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Head, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> HeadAsync(long number, CancellationToken cancellationToken) =>
        Connection.BufferedMultiLineCommandAsync(
            $"HEAD {number}",
            new ArticleResponseParser(ArticleRequestType.Head, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> HeadAsync(CancellationToken cancellationToken) =>
        Connection.BufferedMultiLineCommandAsync(
            "HEAD",
            new ArticleResponseParser(ArticleRequestType.Head, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> BodyAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            $"BODY {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Body, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> BodyAsync(long number, CancellationToken cancellationToken) =>
        Connection.BufferedMultiLineCommandAsync(
            $"BODY {number}",
            new ArticleResponseParser(ArticleRequestType.Body, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> BodyAsync(CancellationToken cancellationToken) =>
        Connection.BufferedMultiLineCommandAsync(
            "BODY",
            new ArticleResponseParser(ArticleRequestType.Body, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStatResponse> StatAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken
    ) =>
        Connection.CommandAsync(
            $"STAT {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new StatResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStatResponse> StatAsync(long number, CancellationToken cancellationToken) =>
        Connection.CommandAsync(
            $"STAT {number}",
            new StatResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStatResponse> StatAsync(CancellationToken cancellationToken) =>
        Connection.CommandAsync("STAT", new StatResponseParser(_loggerFactory), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> PostAsync(NntpArticle article, CancellationToken cancellationToken)
    {
        var initialResponse = await Connection
            .CommandAsync("POST", new ResponseParser(340), cancellationToken)
            .ConfigureAwait(false);
        if (!initialResponse.Success)
        {
            return false;
        }

        await ArticleWriter
            .WriteAsync(Connection, article, cancellationToken)
            .ConfigureAwait(false);
        var subsequentResponse = await Connection
            .GetResponseAsync(new ResponseParser(240), cancellationToken)
            .ConfigureAwait(false);
        return subsequentResponse.Success;
    }

    /// <inheritdoc />
    public async Task<bool> IhaveAsync(NntpArticle article, CancellationToken cancellationToken)
    {
        var initialResponse = await Connection
            .CommandAsync("IHAVE", new ResponseParser(335), cancellationToken)
            .ConfigureAwait(false);
        if (!initialResponse.Success)
        {
            return false;
        }

        await ArticleWriter
            .WriteAsync(Connection, article, cancellationToken)
            .ConfigureAwait(false);
        var subsequentResponse = await Connection
            .GetResponseAsync(new ResponseParser(235), cancellationToken)
            .ConfigureAwait(false);
        return subsequentResponse.Success;
    }

    /// <inheritdoc />
    public Task<NntpDateResponse> DateAsync(CancellationToken cancellationToken) =>
        Connection.CommandAsync("DATE", new DateResponseParser(_loggerFactory), cancellationToken);

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> HelpAsync(CancellationToken cancellationToken) =>
        Connection.MultiLineCommandAsync(
            "HELP",
            new MultiLineResponseParser(100),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpGroupsResponse> NewGroupsAsync(
        NntpDateTime sinceDateTime,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineCommandAsync(
            $"NEWGROUPS {sinceDateTime}",
            new GroupsResponseParser(231, GroupStatusRequestType.Basic, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpMessageId>> NewNewsAsync(
        string wildmat,
        NntpDateTime sinceDateTime,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpMessageId>(
            $"NEWNEWS {wildmat} {sinceDateTime}",
            230,
            NntpStreamLineParsers.MessageId,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpGroupOriginsResponse> ListActiveTimesAsync(
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineCommandAsync(
            "LIST ACTIVE.TIMES",
            new GroupOriginsResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpGroupOriginsResponse> ListActiveTimesAsync(
        string wildmat,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineCommandAsync(
            $"LIST ACTIVE.TIMES {wildmat}",
            new GroupOriginsResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListDistribPatsAsync(CancellationToken cancellationToken) =>
        Connection.MultiLineCommandAsync(
            "LIST DISTRIB.PATS",
            new MultiLineResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpNewsgroupDescription>> ListNewsgroupsAsync(
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpNewsgroupDescription>(
            "LIST NEWSGROUPS",
            215,
            _streamLineParsers.NewsgroupDescription,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpNewsgroupDescription>> ListNewsgroupsAsync(
        string wildmat,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpNewsgroupDescription>(
            $"LIST NEWSGROUPS {wildmat}",
            215,
            _streamLineParsers.NewsgroupDescription,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> OverAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineCommandAsync(
            $"OVER {messageId}",
            new MultiLineResponseParser(224),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> OverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineCommandAsync(
            $"OVER {range}",
            new MultiLineResponseParser(224),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> OverAsync(CancellationToken cancellationToken) =>
        Connection.MultiLineCommandAsync(
            "OVER",
            new MultiLineResponseParser(224),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListOverviewFormatAsync(
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineCommandAsync(
            "LIST OVERVIEW.FMT",
            new MultiLineResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> HdrAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpHeaderField>(
            $"HDR {field} {messageId}",
            225,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> HdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpHeaderField>(
            $"HDR {field} {range}",
            225,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> HdrAsync(
        string field,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpHeaderField>(
            $"HDR {field}",
            225,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListHeadersAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineCommandAsync(
            $"LIST HEADERS {messageId}",
            new MultiLineResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListHeadersAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineCommandAsync(
            $"LIST HEADERS {range}",
            new MultiLineResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListHeadersAsync(CancellationToken cancellationToken) =>
        Connection.MultiLineCommandAsync(
            "LIST HEADERS",
            new MultiLineResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> XhdrAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpHeaderField>(
            $"XHDR {field} {messageId}",
            221,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> XhdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpHeaderField>(
            $"XHDR {field} {range}",
            221,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> XhdrAsync(
        string field,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpHeaderField>(
            $"XHDR {field}",
            221,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpArticleOverview>> XoverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpArticleOverview>(
            $"XOVER {range}",
            224,
            _streamLineParsers.Overview,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpArticleOverview>> XoverAsync(
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpArticleOverview>(
            "XOVER",
            224,
            _streamLineParsers.Overview,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpGroupsResponse> ListCountsAsync(CancellationToken cancellationToken) =>
        Connection.MultiLineCommandAsync(
            "LIST COUNTS",
            new GroupsResponseParser(215, GroupStatusRequestType.Extended, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpGroupsResponse> ListCountsAsync(
        string wildmat,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineCommandAsync(
            $"LIST COUNTS {wildmat}",
            new GroupsResponseParser(215, GroupStatusRequestType.Extended, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListDistributionsAsync(
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineCommandAsync(
            "LIST DISTRIBUTIONS",
            new MultiLineResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListModeratorsAsync(CancellationToken cancellationToken) =>
        Connection.MultiLineCommandAsync(
            "LIST MODERATORS",
            new MultiLineResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListMotdAsync(CancellationToken cancellationToken) =>
        Connection.MultiLineCommandAsync(
            "LIST MOTD",
            new MultiLineResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> ListSubscriptionsAsync(
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineCommandAsync(
            "LIST SUBSCRIPTIONS",
            new MultiLineResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpGroup>> ListActiveAsync(
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpGroup>(
            "LIST ACTIVE",
            215,
            _streamLineParsers.BasicGroup,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpGroup>> ListActiveAsync(
        string wildmat,
        CancellationToken cancellationToken
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpGroup>(
            $"LIST ACTIVE {wildmat}",
            215,
            _streamLineParsers.BasicGroup,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpResponse> XfeatureCompressGzipAsync(
        bool withTerminator,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XzhdrAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XzhdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XzhdrAsync(
        string field,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XzverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<NntpMultiLineResponse> XzverAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
