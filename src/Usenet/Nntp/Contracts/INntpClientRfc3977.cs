using System.Diagnostics.CodeAnalysis;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Contracts;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
public interface INntpClientRfc3977
{
    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-5.2">CAPABILITIES</a>
    /// command allows a client to determine the capabilities of the server at any given time.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing the capabilities.</returns>
    Task<NntpMultiLineResponse> CapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-5.2">CAPABILITIES</a>
    /// command allows a client to determine the capabilities of the server at any given time.
    /// </summary>
    /// <param name="keyword">Can be provided for additional features if supported by the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing the capabilities.</returns>
    Task<NntpMultiLineResponse> CapabilitiesAsync(
        string keyword,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-5.3">MODE READER</a>
    /// (<a href="https://tools.ietf.org/html/rfc2980#section-2.3">ad 1</a>) command
    /// instructs a mode-switching server to switch modes, as described in Section 3.4.2.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A mode reader response object.</returns>
    Task<NntpModeReaderResponse> ModeReaderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.1.1">GROUP</a>
    /// command selects a newsgroup as the currently selected newsgroup and returns summary information about it.
    /// </summary>
    /// <param name="group">The name of the group to select.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A group response object.</returns>
    Task<NntpGroupResponse> GroupAsync(string group, CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.1.2">LISTGROUP</a>
    /// command selects a newsgroup in the same manner as the
    /// GROUP command (see Section 6.1.1) but also provides a list of article
    /// numbers in the newsgroup. Only article numbers in the specified range are included in the list.
    /// </summary>
    /// <param name="group">The name of the group to select.</param>
    /// <param name="range">Only include article numbers within this range in the list.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A group response object.</returns>
    Task<NntpGroupResponse> ListGroupAsync(
        string group,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.1.2">LISTGROUP</a>
    /// command selects a newsgroup in the same manner as the
    /// GROUP command (see Section 6.1.1) but also provides a list of article
    /// numbers in the newsgroup.
    /// </summary>
    /// <param name="group">The name of the group to select.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A group response object.</returns>
    Task<NntpGroupResponse> ListGroupAsync(
        string group,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.1.2">LISTGROUP</a>
    /// command without no group specified provides a list of article
    /// numbers in the newsgroup.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A group response object.</returns>
    Task<NntpGroupResponse> ListGroupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.1.3">LAST</a> command.
    /// If the currently selected newsgroup is valid, the current article
    /// number will be set to the previous article in that newsgroup (that
    /// is, the highest existing article number less than the current article
    /// number).
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A last response object.</returns>
    Task<NntpLastResponse> LastAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.1.4">NEXT</a> command.
    /// If the currently selected newsgroup is valid, the current article
    /// number will be set to the next article in that newsgroup (that is,
    /// the lowest existing article number greater than the current article
    /// number).
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A next response object.</returns>
    Task<NntpNextResponse> NextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.1">ARTICLE</a> command
    /// selects an article according to the arguments and
    /// presents the entire article (that is, the headers, an empty line, and
    /// the body, in that order) to the client.
    /// </summary>
    /// <param name="messageId">The message-id of the article to received from the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An article response object.</returns>
    Task<NntpArticleResponse> ArticleAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.1">ARTICLE</a> command
    /// selects an article according to the arguments and
    /// presents the entire article (that is, the headers, an empty line, and
    /// the body, in that order) to the client.
    /// </summary>
    /// <param name="number">The number of the article to receive from the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An article response object.</returns>
    Task<NntpArticleResponse> ArticleAsync(
        long number,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.1">ARTICLE</a> command
    /// selects an article according to the arguments and
    /// presents the entire article (that is, the headers, an empty line, and
    /// the body, in that order) to the client.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An article response object.</returns>
    Task<NntpArticleResponse> ArticleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.2">HEAD</a>
    /// command behaves identically to the ARTICLE command except
    /// that, if the article exists, the response code is 221 instead of 220
    /// and only the headers are presented (the empty line separating the
    /// headers and body MUST NOT be included).
    /// </summary>
    /// <param name="messageId">The message-id of the article to received from the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An article response object.</returns>
    Task<NntpArticleResponse> HeadAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.2">HEAD</a>
    /// command behaves identically to the ARTICLE command except
    /// that, if the article exists, the response code is 221 instead of 220
    /// and only the headers are presented (the empty line separating the
    /// headers and body MUST NOT be included).
    /// </summary>
    /// <param name="number">The number of the article to receive from the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An article response object.</returns>
    Task<NntpArticleResponse> HeadAsync(long number, CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.2">HEAD</a>
    /// command behaves identically to the ARTICLE command except
    /// that, if the article exists, the response code is 221 instead of 220
    /// and only the headers are presented (the empty line separating the
    /// headers and body MUST NOT be included).
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An article response object.</returns>
    Task<NntpArticleResponse> HeadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.3">BODY</a>
    /// command behaves identically to the ARTICLE command except
    /// that, if the article exists, the response code is 222 instead of 220
    /// and only the body is presented (the empty line separating the headers
    /// and body MUST NOT be included).
    /// </summary>
    /// <param name="messageId">The message-id of the article to received from the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An article response object.</returns>
    Task<NntpArticleResponse> BodyAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.3">BODY</a>
    /// command behaves identically to the ARTICLE command except
    /// that, if the article exists, the response code is 222 instead of 220
    /// and only the body is presented (the empty line separating the headers
    /// and body MUST NOT be included).
    /// See <a href="https://tools.ietf.org/html/rfc3977#section-6.2.3">RFC 3977</a> for more information.
    /// </summary>
    /// <param name="number">The number of the article to receive from the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An article response object.</returns>
    Task<NntpArticleResponse> BodyAsync(long number, CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.3">BODY</a>
    /// command behaves identically to the ARTICLE command except
    /// that, if the article exists, the response code is 222 instead of 220
    /// and only the body is presented (the empty line separating the headers
    /// and body MUST NOT be included).
    /// See <a href="https://tools.ietf.org/html/rfc3977#section-6.2.3">RFC 3977</a> for more information.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An article response object.</returns>
    Task<NntpArticleResponse> BodyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.4">STAT</a>
    /// command behaves identically to the ARTICLE command except
    /// that, if the article exists, it is NOT presented to the client and
    /// the response code is 223 instead of 220.  Note that the response is
    /// NOT multi-line.
    /// </summary>
    /// <param name="messageId">The message-id of the article to received from the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A stat response object.</returns>
    Task<NntpStatResponse> StatAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.4">STAT</a>
    /// command behaves identically to the ARTICLE command except
    /// that, if the article exists, it is NOT presented to the client and
    /// the response code is 223 instead of 220.  Note that the response is
    /// NOT multi-line.
    /// </summary>
    /// <param name="number">The number of the article to receive from the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A stat response object.</returns>
    Task<NntpStatResponse> StatAsync(long number, CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.2.4">STAT</a>
    /// command behaves identically to the ARTICLE command except
    /// that, if the article exists, it is NOT presented to the client and
    /// the response code is 223 instead of 220.  Note that the response is
    /// NOT multi-line.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A stat response object.</returns>
    Task<NntpStatResponse> StatAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Post an article.
    /// <a href="https://tools.ietf.org/html/rfc3977#section-6.3.1">POST</a> an article.
    /// </summary>
    /// <param name="article">The article to post.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>true if the article was posted successfully; otherwise false.</returns>
    Task<bool> PostAsync(NntpArticle article, CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-6.3.2">IHAVE</a> command
    /// informs the server that the client has an article with the specified message-id.
    /// </summary>
    /// <param name="article">The article to send.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>true if the article was accepted; otherwise false.</returns>
    Task<bool> IhaveAsync(NntpArticle article, CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-7.1">DATE</a>
    /// command exists to help clients find out the current Coordinated
    /// Universal Time [TF.686-1] from the server's perspective.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A date response object.</returns>
    Task<NntpDateResponse> DateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a herf="https://tools.ietf.org/html/rfc3977#section-7.2">HELP</a>
    /// command provides a short summary of the commands that are
    /// understood by this implementation of the server.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response with a short summary of the available commands.</returns>
    Task<NntpMultiLineResponse> HelpAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-7.3">NEWGROUPS</a>
    /// command returns a list of newsgroups created on the server since
    /// the specified date and time.
    /// </summary>
    /// <param name="sinceDateTime">List all newsgroups created since this date and time.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A groups response object.</returns>
    Task<NntpGroupsResponse> NewGroupsAsync(
        NntpDateTime sinceDateTime,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-7.4">NEWNEWS</a>
    /// command returns a list of message-ids of articles posted or
    /// received on the server, in the newsgroups whose names match the
    /// wildmat, since the specified date and time.
    /// </summary>
    /// <param name="wildmat">The wildmat to use for filtering the group names.</param>
    /// <param name="sinceDateTime">List all newsgroups that have new articles
    /// posted or received since this date and time.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing a list of message-ids.</returns>
    Task<NntpMultiLineResponse> NewNewsAsync(
        string wildmat,
        NntpDateTime sinceDateTime,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-7.6.4">active.times list</a>
    /// (<a href="https://tools.ietf.org/html/rfc2980#section-2.1.3">ad 1</a>)
    /// is maintained by some NNTP servers to contain
    /// information about who created a particular newsgroup and when.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A group origins response.</returns>
    Task<NntpGroupOriginsResponse> ListActiveTimesAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-7.6.4">active.times list</a>
    /// (<a href="https://tools.ietf.org/html/rfc2980#section-2.1.3">ad 1</a>)
    /// is maintained by some NNTP servers to contain
    /// information about who created a particular newsgroup and when.
    /// </summary>
    /// <param name="wildmat">The wildmat to use for filtering the group names.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A group origins response.</returns>
    Task<NntpGroupOriginsResponse> ListActiveTimesAsync(
        string wildmat,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-7.6.5">distrib.pats list</a>
    /// (<a href="https://tools.ietf.org/html/rfc2980#section-2.1.5">ad 1</a>)
    /// is maintained by some NNTP servers to assist
    /// clients to choose a value for the content of the Distribution header
    /// of a news article being posted.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object containing newsgroup distribution information.</returns>
    Task<NntpMultiLineResponse> ListDistribPatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-7.6.6">newsgroups list</a>
    /// (<a href="https://tools.ietf.org/html/rfc2980#section-2.1.6">ad 1</a>)
    /// is maintained by NNTP servers to contain the name
    /// of each newsgroup that is available on the server and a short
    /// description about the purpose of the group.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing a list of newsgroups available on the server.</returns>
    Task<NntpMultiLineResponse> ListNewsgroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-7.6.6">newsgroups list</a>
    /// (<a href="https://tools.ietf.org/html/rfc2980#section-2.1.6">ad 1</a>)
    /// is maintained by NNTP servers to contain the name
    /// of each newsgroup that is available on the server and a short
    /// description about the purpose of the group.
    /// </summary>
    /// <param name="wildmat">The wildmat to use for filtering the group names.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing a list of newsgroups available on the server.</returns>
    Task<NntpMultiLineResponse> ListNewsgroupsAsync(
        string wildmat,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-8.3">OVER</a>
    /// command returns the contents of all the fields in the
    /// database for an article specified by message-id, or from a specified
    /// article or range of articles in the currently selected newsgroup.
    /// </summary>
    /// <param name="messageId">The message-id of the article to received from the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing header fields.</returns>
    Task<NntpMultiLineResponse> OverAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-8.3">OVER</a>
    /// command returns the contents of all the fields in the
    /// database for an article specified by message-id, or from a specified
    /// article or range of articles in the currently selected newsgroup.
    /// </summary>
    /// <param name="range">Only include article numbers within this range in the list.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing header fields.</returns>
    Task<NntpMultiLineResponse> OverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-8.3">OVER</a>
    /// command returns the contents of all the fields in the
    /// database for an article specified by message-id, or from a specified
    /// article or range of articles in the currently selected newsgroup.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing header fields.</returns>
    Task<NntpMultiLineResponse> OverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-8.4">LIST OVERVIEW.FMT</a>
    /// (<a href="https://tools.ietf.org/html/rfc2980#section-2.1.7">ad 1</a>)
    /// command returns a description of the fields in
    /// the database for which it is consistent (as described above).
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing a description of the fields
    /// in the overview database for which it is consistent.</returns>
    Task<NntpMultiLineResponse> ListOverviewFormatAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-8.5">HDR</a>
    /// command provides access to specific fields from an article
    /// specified by message-id, or from a specified article or range of
    /// articles in the currently selected newsgroup.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="messageId">The message-id of the article to received from the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing the specfied header fields.</returns>
    Task<NntpMultiLineResponse> HdrAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-8.5">HDR</a>
    /// command provides access to specific fields from an article
    /// specified by message-id, or from a specified article or range of
    /// articles in the currently selected newsgroup.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="range">Only include article numbers within this range in the list.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing the specfied header fields.</returns>
    Task<NntpMultiLineResponse> HdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-8.5">HDR</a>
    /// command provides access to specific fields from an article
    /// specified by message-id, or from a specified article or range of
    /// articles in the currently selected newsgroup.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing the specfied header fields.</returns>
    Task<NntpMultiLineResponse> HdrAsync(
        string field,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-8.6">LIST HEADERS</a>
    /// command returns a list of fields that may be
    /// retrieved using the HDR command.
    /// </summary>
    /// <param name="messageId">The message-id of the article to received from the server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containg a list of header
    /// fields that may be retrieved using the HDR command.</returns>
    Task<NntpMultiLineResponse> ListHeadersAsync(
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-8.6">LIST HEADERS</a>
    /// command returns a list of fields that may be
    /// retrieved using the HDR command.
    /// </summary>
    /// <param name="range">Only include article numbers within this range in the list.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containg a list of header
    /// fields that may be retrieved using the HDR command.</returns>
    Task<NntpMultiLineResponse> ListHeadersAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc3977#section-8.6">LIST HEADERS</a>
    /// command returns a list of fields that may be
    /// retrieved using the HDR command.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containg a list of header
    /// fields that may be retrieved using the HDR command.</returns>
    Task<NntpMultiLineResponse> ListHeadersAsync(CancellationToken cancellationToken = default);
}
