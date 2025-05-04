using Usenet.Nntp.Contracts;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;
using Usenet.Util;

namespace Usenet.Nntp;

/// <inheritdoc />
public sealed class PooledNntpClient : IPooledNntpClient
{
    private readonly NntpConnection _connection;
    private readonly NntpClient _client;
    private bool _disposed;
    public bool Connected { get; private set; }
    public bool Authenticated { get; private set; }
    public DateTimeOffset LastActivity { get; private set; }

    public PooledNntpClient()
    {
        _connection = new NntpConnection();
        _client = new NntpClient(_connection);
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

    /// <summary>
    /// Reads any unprocessed data from the underlying connection
    /// This prevents consecutive commands on this pooled client from failing due to partially unprocessed data.
    /// </summary>
    internal void Flush() => _connection.Flush();

    internal async Task<bool> ConnectAsync(string hostname, int port, bool useSsl)
    {
        var res = await _client.ConnectAsync(hostname, port, useSsl).ConfigureAwait(false);
        Connected = res;
        return res;
    }

    internal bool Authenticate(string username, string password = null)
    {
        var res = _client.Authenticate(username, password);
        Authenticated = res;
        return res;
    }

    #region INntpClient

    public NntpResponse XfeatureCompressGzip(bool withTerminator) => Client.XfeatureCompressGzip(withTerminator);

    public NntpMultiLineResponse Xzhdr(string field, NntpMessageId messageId) => Client.Xzhdr(field, messageId);

    public NntpMultiLineResponse Xzhdr(string field, NntpArticleRange range) => Client.Xzhdr(field, range);

    public NntpMultiLineResponse Xzhdr(string field) => Client.Xzhdr(field);

    public NntpMultiLineResponse Xzver(NntpArticleRange range) => Client.Xzver(range);

    public NntpMultiLineResponse Xzver() => Client.Xzver();

    public void ResetCounters() => Client.ResetCounters();

    public NntpMultiLineResponse Xhdr(string field, NntpMessageId messageId) => Client.Xhdr(field, messageId);

    public NntpMultiLineResponse Xhdr(string field, NntpArticleRange range) => Client.Xhdr(field, range);

    public NntpMultiLineResponse Xhdr(string field) => Client.Xhdr(field);

    public NntpMultiLineResponse Xover(NntpArticleRange range) => Client.Xover(range);

    public NntpMultiLineResponse Xover() => Client.Xover();

    public NntpMultiLineResponse Capabilities() => Client.Capabilities();

    public NntpMultiLineResponse Capabilities(string keyword) => Client.Capabilities(keyword);

    public NntpModeReaderResponse ModeReader() => Client.ModeReader();
    public NntpResponse Quit() => Client.Quit();
    public NntpGroupResponse Group(string group) => Client.Group(group);
    public NntpGroupResponse ListGroup(string group, NntpArticleRange range) => Client.ListGroup(group, range);
    public NntpGroupResponse ListGroup(string group) => Client.ListGroup(group);
    public NntpGroupResponse ListGroup() => Client.ListGroup();
    public NntpLastResponse Last() => Client.Last();
    public NntpNextResponse Next() => Client.Next();
    public NntpArticleResponse Article(NntpMessageId messageId) => Client.Article(messageId);
    public NntpArticleResponse Article(long number) => Client.Article(number);
    public NntpArticleResponse Article() => Client.Article();
    public NntpArticleResponse Head(NntpMessageId messageId) => Client.Head(messageId);
    public NntpArticleResponse Head(long number) => Client.Head(number);
    public NntpArticleResponse Head() => Client.Head();
    public NntpArticleResponse Body(NntpMessageId messageId) => Client.Body();
    public NntpArticleResponse Body(long number) => Client.Body(number);
    public NntpArticleResponse Body() => Client.Body();
    public NntpStatResponse Stat(NntpMessageId messageId) => Client.Stat(messageId);

    public NntpStatResponse Stat(long number) => Client.Stat(number);

    public NntpStatResponse Stat() => Client.Stat();

    public bool Post(NntpArticle article) => Client.Post(article);

    public bool Ihave(NntpArticle article) => Client.Ihave(article);

    public NntpDateResponse Date() => Client.Date();

    public NntpMultiLineResponse Help() => Client.Help();

    public NntpGroupsResponse NewGroups(NntpDateTime sinceDateTime) => Client.NewGroups(sinceDateTime);

    public NntpMultiLineResponse NewNews(string wildmat, NntpDateTime sinceDateTime) => Client.NewNews(wildmat, sinceDateTime);

    public NntpGroupOriginsResponse ListActiveTimes() => Client.ListActiveTimes();

    public NntpGroupOriginsResponse ListActiveTimes(string wildmat) => Client.ListActiveTimes(wildmat);

    public NntpMultiLineResponse ListDistribPats() => Client.ListDistribPats();

    public NntpMultiLineResponse ListNewsgroups() => Client.ListNewsgroups();

    public NntpMultiLineResponse ListNewsgroups(string wildmat) => Client.ListNewsgroups(wildmat);

    public NntpMultiLineResponse Over(NntpMessageId messageId) => Client.Over(messageId);

    public NntpMultiLineResponse Over(NntpArticleRange range) => Client.Over(range);

    public NntpMultiLineResponse Over() => Client.Over();

    public NntpMultiLineResponse ListOverviewFormat() => Client.ListOverviewFormat();

    public NntpMultiLineResponse Hdr(string field, NntpMessageId messageId) => Client.Hdr(field, messageId);

    public NntpMultiLineResponse Hdr(string field, NntpArticleRange range) => Client.Hdr(field, range);

    public NntpMultiLineResponse Hdr(string field) => Client.Hdr(field);

    public NntpMultiLineResponse ListHeaders(NntpMessageId messageId) => Client.ListHeaders(messageId);

    public NntpMultiLineResponse ListHeaders(NntpArticleRange range) => Client.ListHeaders(range);

    public NntpMultiLineResponse ListHeaders() => Client.ListHeaders();

    public NntpGroupsResponse ListCounts() => Client.ListCounts();

    public NntpGroupsResponse ListCounts(string wildmat) => Client.ListCounts(wildmat);

    public NntpMultiLineResponse ListDistributions() => Client.ListDistributions();

    public NntpMultiLineResponse ListModerators() => Client.ListModerators();

    public NntpMultiLineResponse ListMotd() => Client.ListMotd();

    public NntpMultiLineResponse ListSubscriptions() => Client.ListSubscriptions();

    public NntpGroupsResponse ListActive() => Client.ListActive();

    public NntpGroupsResponse ListActive(string wildmat) => Client.ListActive(wildmat);

    #endregion

    public void Dispose()
    {
        if (_disposed) return;

        _client.Quit();
        Connected = false;
        Authenticated = false;
        _connection.Dispose();

        _disposed = true;
    }
}
