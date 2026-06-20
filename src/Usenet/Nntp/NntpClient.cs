using System.Collections.Immutable;
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
// IPooledNntpClient is a subset of INntpClient, so the pool can hand the lease an NntpClient
// directly as its command surface without a hand-written per-command decorator.
public class NntpClient : INntpClient, IPooledNntpClient
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
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var response = await Connection
            .ConnectAsync(new ResponseParser(200, 201), cancellationToken)
            .ConfigureAwait(false);
        return response.Success;
    }

    /// <inheritdoc />
    public async Task<bool> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        var authenticated = await AuthenticateCoreAsync(username, password, cancellationToken)
            .ConfigureAwait(false);

        // Compression is the third step of the session-setup recipe (connect -> authenticate ->
        // enable compression): RFC 8054 §2.2 forbids authenticating once COMPRESS is active, so it is
        // negotiated only after a successful authentication. See ADR-0005.
        if (authenticated)
        {
            await EnableConfiguredCompressionAsync(cancellationToken).ConfigureAwait(false);
        }

        return authenticated;
    }

    private async Task<bool> AuthenticateCoreAsync(
        string username,
        string password,
        CancellationToken cancellationToken
    )
    {
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

    private Task EnableConfiguredCompressionAsync(CancellationToken cancellationToken) =>
        Connection
            is INntpCompressionControl
            {
                Compression: not NntpCompression.None,
                CompressionEnabled: false,
            } control
            ? control.EnableCompressionAsync(cancellationToken)
            : Task.CompletedTask;

    /// <inheritdoc />
    public Task<NntpCapabilities> CapabilitiesAsync(
        string? keyword = null,
        CancellationToken cancellationToken = default
    )
    {
        var command = string.IsNullOrWhiteSpace(keyword)
            ? "CAPABILITIES"
            : $"CAPABILITIES {keyword}";
        return Connection.MultiLineCommandAsync(
            command,
            new CapabilitiesResponseParser(),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<NntpModeReaderResponse> ModeReaderAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.CommandAsync(
            "MODE READER",
            new ModeReaderResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpResponse> QuitAsync(CancellationToken cancellationToken = default) =>
        Connection.CommandAsync("QUIT", new ResponseParser(205), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupResponse> GroupAsync(
        string group,
        CancellationToken cancellationToken = default
    )
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
        string? group = null,
        NntpArticleRange? range = null,
        CancellationToken cancellationToken = default
    )
    {
        string command;
        if (group is null)
        {
            command = "LISTGROUP";
        }
        else
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(group);
            command = range is null ? $"LISTGROUP {group}" : $"LISTGROUP {group} {range}";
        }

        return Connection.MultiLineStreamCommandAsync<long>(
            command,
            211,
            NntpStreamLineParsers.ArticleNumber,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<NntpLastResponse> LastAsync(CancellationToken cancellationToken = default) =>
        Connection.CommandAsync("LAST", new LastResponseParser(_loggerFactory), cancellationToken);

    /// <inheritdoc />
    public Task<NntpNextResponse> NextAsync(CancellationToken cancellationToken = default) =>
        Connection.CommandAsync("NEXT", new NextResponseParser(_loggerFactory), cancellationToken);

    /// <inheritdoc />
    public Task<NntpArticleResponse> ArticleAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            $"ARTICLE {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Article, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> ArticleByNumberAsync(
        long number,
        CancellationToken cancellationToken = default
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            $"ARTICLE {number}",
            new ArticleResponseParser(ArticleRequestType.Article, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> CurrentArticleAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            "ARTICLE",
            new ArticleResponseParser(ArticleRequestType.Article, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> HeadAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            $"HEAD {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Head, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> HeadByNumberAsync(
        long number,
        CancellationToken cancellationToken = default
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            $"HEAD {number}",
            new ArticleResponseParser(ArticleRequestType.Head, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> CurrentHeadAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            "HEAD",
            new ArticleResponseParser(ArticleRequestType.Head, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> BodyAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            $"BODY {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new ArticleResponseParser(ArticleRequestType.Body, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> BodyByNumberAsync(
        long number,
        CancellationToken cancellationToken = default
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            $"BODY {number}",
            new ArticleResponseParser(ArticleRequestType.Body, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleResponse> CurrentBodyAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.BufferedMultiLineCommandAsync(
            "BODY",
            new ArticleResponseParser(ArticleRequestType.Body, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStatResponse> StatAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) =>
        Connection.CommandAsync(
            $"STAT {messageId.ThrowIfNullOrWhiteSpace(nameof(messageId))}",
            new StatResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStatResponse> StatByNumberAsync(
        long number,
        CancellationToken cancellationToken = default
    ) =>
        Connection.CommandAsync(
            $"STAT {number}",
            new StatResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStatResponse> CurrentStatAsync(CancellationToken cancellationToken = default) =>
        Connection.CommandAsync("STAT", new StatResponseParser(_loggerFactory), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> PostAsync(
        NntpArticle article,
        CancellationToken cancellationToken = default
    )
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
    public async Task<bool> IhaveAsync(
        NntpArticle article,
        CancellationToken cancellationToken = default
    )
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
    public Task<NntpDateResponse> DateAsync(CancellationToken cancellationToken = default) =>
        Connection.CommandAsync("DATE", new DateResponseParser(_loggerFactory), cancellationToken);

    /// <inheritdoc />
    public Task<NntpTextResponse> HelpAsync(CancellationToken cancellationToken = default) =>
        Connection.MultiLineCommandAsync("HELP", new TextResponseParser(100), cancellationToken);

    /// <inheritdoc />
    public Task<NntpGroupsResponse> NewGroupsAsync(
        NntpDateTime sinceDateTime,
        CancellationToken cancellationToken = default
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
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpMessageId>(
            $"NEWNEWS {wildmat} {sinceDateTime}",
            230,
            NntpStreamLineParsers.MessageId,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpGroupOriginsResponse> ListActiveTimesAsync(
        string? wildmat = null,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineCommandAsync(
            string.IsNullOrWhiteSpace(wildmat)
                ? "LIST ACTIVE.TIMES"
                : $"LIST ACTIVE.TIMES {wildmat}",
            new GroupOriginsResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<IImmutableList<NntpDistributionPattern>> ListDistribPatsAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineCommandAsync(
            "LIST DISTRIB.PATS",
            new DistribPatsResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpNewsgroupDescription>> ListNewsgroupsAsync(
        string? wildmat = null,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpNewsgroupDescription>(
            string.IsNullOrWhiteSpace(wildmat) ? "LIST NEWSGROUPS" : $"LIST NEWSGROUPS {wildmat}",
            215,
            _streamLineParsers.NewsgroupDescription,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpArticleOverview>> OverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpArticleOverview>(
            $"OVER {range}",
            224,
            _streamLineParsers.Overview,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpArticleOverview?> OverByMessageIdAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) =>
        SingleRecordAsync<NntpArticleOverview>(
            $"OVER {messageId}",
            224,
            _streamLineParsers.Overview,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpArticleOverview>> CurrentOverAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpArticleOverview>(
            "OVER",
            224,
            _streamLineParsers.Overview,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpOverviewFormat> ListOverviewFormatAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineCommandAsync(
            "LIST OVERVIEW.FMT",
            new OverviewFormatResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> HdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpHeaderField>(
            $"HDR {field} {range}",
            225,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpHeaderField?> HdrByMessageIdAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) =>
        SingleRecordAsync<NntpHeaderField>(
            $"HDR {field} {messageId}",
            225,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> CurrentHdrAsync(
        string field,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpHeaderField>(
            $"HDR {field}",
            225,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpTextResponse> ListHeadersAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineCommandAsync(
            $"LIST HEADERS {range}",
            new TextResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpTextResponse> ListHeadersByMessageIdAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineCommandAsync(
            $"LIST HEADERS {messageId}",
            new TextResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpTextResponse> CurrentListHeadersAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineCommandAsync(
            "LIST HEADERS",
            new TextResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> XhdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpHeaderField>(
            $"XHDR {field} {range}",
            221,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpHeaderField?> XhdrByMessageIdAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    ) =>
        SingleRecordAsync<NntpHeaderField>(
            $"XHDR {field} {messageId}",
            221,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <summary>
    /// Issues a command whose message-id form yields exactly one record (the RFC 3977
    /// <c>OVER</c>/<c>HDR</c> and the legacy <c>XHDR</c> single-article forms), materializes its
    /// small data block, and returns the single row parsed by the shared per-line parser. A one-row
    /// stream with its drain contract would be overkill for one record (ADR-0003).
    /// </summary>
    /// <typeparam name="T">The type the single data-block line is parsed into.</typeparam>
    /// <param name="command">The command to send to the server.</param>
    /// <param name="successCode">The response code that indicates a data block follows.</param>
    /// <param name="lineParser">The shared per-line parser to apply to the record.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The parsed record, or <see langword="null"/> when the article is absent or the
    /// record is unparseable.</returns>
    private async Task<T?> SingleRecordAsync<T>(
        string command,
        int successCode,
        NntpStreamLineParser<T> lineParser,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var response = await Connection
            .MultiLineCommandAsync(command, new TextResponseParser(successCode), cancellationToken)
            .ConfigureAwait(false);

        if (!response.Success)
        {
            return null;
        }

        foreach (var line in response.Lines)
        {
            if (lineParser(line, out var value))
            {
                return value;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> CurrentXhdrAsync(
        string field,
        CancellationToken cancellationToken = default
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
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpArticleOverview>(
            $"XOVER {range}",
            224,
            _streamLineParsers.Overview,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpArticleOverview>> CurrentXoverAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpArticleOverview>(
            "XOVER",
            224,
            _streamLineParsers.Overview,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpArticleOverview>> XzverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineDecompressedStreamCommandAsync<NntpArticleOverview>(
            $"XZVER {range}",
            224,
            _streamLineParsers.Overview,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpArticleOverview>> CurrentXzverAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineDecompressedStreamCommandAsync<NntpArticleOverview>(
            "XZVER",
            224,
            _streamLineParsers.Overview,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> XzhdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineDecompressedStreamCommandAsync<NntpHeaderField>(
            $"XZHDR {field} {range}",
            221,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpHeaderField>> CurrentXzhdrAsync(
        string field,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineDecompressedStreamCommandAsync<NntpHeaderField>(
            $"XZHDR {field}",
            221,
            _streamLineParsers.HeaderField,
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpGroupsResponse> ListCountsAsync(
        string? wildmat = null,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineCommandAsync(
            string.IsNullOrWhiteSpace(wildmat) ? "LIST COUNTS" : $"LIST COUNTS {wildmat}",
            new GroupsResponseParser(215, GroupStatusRequestType.Extended, _loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<IImmutableList<NntpDistribution>> ListDistributionsAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineCommandAsync(
            "LIST DISTRIBUTIONS",
            new DistributionsResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<IImmutableList<NntpModerator>> ListModeratorsAsync(
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineCommandAsync(
            "LIST MODERATORS",
            new ModeratorsResponseParser(_loggerFactory),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpTextResponse> ListMotdAsync(CancellationToken cancellationToken = default) =>
        Connection.MultiLineCommandAsync(
            "LIST MOTD",
            new TextResponseParser(215),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpGroups> ListSubscriptionsAsync(CancellationToken cancellationToken = default) =>
        Connection.MultiLineCommandAsync(
            "LIST SUBSCRIPTIONS",
            new SubscriptionsResponseParser(),
            cancellationToken
        );

    /// <inheritdoc />
    public Task<NntpStreamResponse<NntpGroup>> ListActiveAsync(
        string? wildmat = null,
        CancellationToken cancellationToken = default
    ) =>
        Connection.MultiLineStreamCommandAsync<NntpGroup>(
            string.IsNullOrWhiteSpace(wildmat) ? "LIST ACTIVE" : $"LIST ACTIVE {wildmat}",
            215,
            _streamLineParsers.BasicGroup,
            cancellationToken
        );
}
