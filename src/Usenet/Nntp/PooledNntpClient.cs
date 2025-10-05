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

    public async Task<bool> ConnectAsync(string hostname, int port, bool useSsl)
    {
        var res = await _client.ConnectAsync(hostname, port, useSsl).ConfigureAwait(false);
        NntpConnected = res;
        return res;
    }

    public bool Authenticate(string username, string password = null)
    {
        var res = _client.Authenticate(username, password);
        Authenticated = res;
        return res;
    }

    public NntpResponse XfeatureCompressGzip(bool withTerminator) => ExecuteCommand(c => c.XfeatureCompressGzip(withTerminator));
    public NntpMultiLineResponse Xzhdr(string field, NntpMessageId messageId) => ExecuteCommand(c => c.Xzhdr(field, messageId));
    public NntpMultiLineResponse Xzhdr(string field, NntpArticleRange range) => ExecuteCommand(c => c.Xzhdr(field, range));
    public NntpMultiLineResponse Xzhdr(string field) => ExecuteCommand(c => c.Xzhdr(field));
    public NntpMultiLineResponse Xzver(NntpArticleRange range) => ExecuteCommand(c => c.Xzver(range));
    public NntpMultiLineResponse Xzver() => ExecuteCommand(c => c.Xzver());
    public void ResetCounters() => ExecuteCommand(c => c.ResetCounters());
    public NntpMultiLineResponse Xhdr(string field, NntpMessageId messageId) => ExecuteCommand(c => c.Xhdr(field, messageId));
    public NntpMultiLineResponse Xhdr(string field, NntpArticleRange range) => ExecuteCommand(c => c.Xhdr(field, range));
    public NntpMultiLineResponse Xhdr(string field) => ExecuteCommand(c => c.Xhdr(field));
    public NntpMultiLineResponse Xover(NntpArticleRange range) => ExecuteCommand(c => c.Xover(range));
    public NntpMultiLineResponse Xover() => ExecuteCommand(c => c.Xover());
    public NntpMultiLineResponse Capabilities() => ExecuteCommand(c => c.Capabilities());
    public NntpMultiLineResponse Capabilities(string keyword) => ExecuteCommand(c => c.Capabilities(keyword));
    public NntpModeReaderResponse ModeReader() => ExecuteCommand(c => c.ModeReader());
    public NntpResponse Quit() => ExecuteCommand(c => c.Quit());
    public NntpGroupResponse Group(string group) => ExecuteCommand(c => c.Group(group));
    public NntpGroupResponse ListGroup(string group, NntpArticleRange range) => ExecuteCommand(c => c.ListGroup(group, range));
    public NntpGroupResponse ListGroup(string group) => ExecuteCommand(c => c.ListGroup(group));
    public NntpGroupResponse ListGroup() => ExecuteCommand(c => c.ListGroup());
    public NntpLastResponse Last() => ExecuteCommand(c => c.Last());
    public NntpNextResponse Next() => ExecuteCommand(c => c.Next());
    public NntpArticleResponse Article(NntpMessageId messageId) => ExecuteCommand(c => c.Article(messageId));
    public NntpArticleResponse Article(long number) => ExecuteCommand(c => c.Article(number));
    public NntpArticleResponse Article() => ExecuteCommand(c => c.Article());
    public NntpArticleResponse Head(NntpMessageId messageId) => ExecuteCommand(c => c.Head(messageId));
    public NntpArticleResponse Head(long number) => ExecuteCommand(c => c.Head(number));
    public NntpArticleResponse Head() => ExecuteCommand(c => c.Head());
    public NntpArticleResponse Body(NntpMessageId messageId) => ExecuteCommand(c => c.Body(messageId));
    public NntpArticleResponse Body(long number) => ExecuteCommand(c => c.Body(number));
    public NntpArticleResponse Body() => ExecuteCommand(c => c.Body());
    public NntpStatResponse Stat(NntpMessageId messageId) => ExecuteCommand(c => c.Stat(messageId));
    public NntpStatResponse Stat(long number) => ExecuteCommand(c => c.Stat(number));
    public NntpStatResponse Stat() => ExecuteCommand(c => c.Stat());
    public bool Post(NntpArticle article) => ExecuteCommand(c => c.Post(article));
    public bool Ihave(NntpArticle article) => ExecuteCommand(c => c.Ihave(article));
    public NntpDateResponse Date() => ExecuteCommand(c => c.Date());
    public NntpMultiLineResponse Help() => ExecuteCommand(c => c.Help());
    public NntpGroupsResponse NewGroups(NntpDateTime sinceDateTime) => ExecuteCommand(c => c.NewGroups(sinceDateTime));
    public NntpMultiLineResponse NewNews(string wildmat, NntpDateTime sinceDateTime) => ExecuteCommand(c => c.NewNews(wildmat, sinceDateTime));
    public NntpGroupOriginsResponse ListActiveTimes() => ExecuteCommand(c => c.ListActiveTimes());
    public NntpGroupOriginsResponse ListActiveTimes(string wildmat) => ExecuteCommand(c => c.ListActiveTimes(wildmat));
    public NntpMultiLineResponse ListDistribPats() => ExecuteCommand(c => c.ListDistribPats());
    public NntpMultiLineResponse ListNewsgroups() => ExecuteCommand(c => c.ListNewsgroups());
    public NntpMultiLineResponse ListNewsgroups(string wildmat) => ExecuteCommand(c => c.ListNewsgroups(wildmat));
    public NntpMultiLineResponse Over(NntpMessageId messageId) => ExecuteCommand(c => c.Over(messageId));
    public NntpMultiLineResponse Over(NntpArticleRange range) => ExecuteCommand(c => c.Over(range));
    public NntpMultiLineResponse Over() => ExecuteCommand(c => c.Over());
    public NntpMultiLineResponse ListOverviewFormat() => ExecuteCommand(c => c.ListOverviewFormat());
    public NntpMultiLineResponse Hdr(string field, NntpMessageId messageId) => ExecuteCommand(c => c.Hdr(field, messageId));
    public NntpMultiLineResponse Hdr(string field, NntpArticleRange range) => ExecuteCommand(c => c.Hdr(field, range));
    public NntpMultiLineResponse Hdr(string field) => ExecuteCommand(c => c.Hdr(field));
    public NntpMultiLineResponse ListHeaders(NntpMessageId messageId) => ExecuteCommand(c => c.ListHeaders(messageId));
    public NntpMultiLineResponse ListHeaders(NntpArticleRange range) => ExecuteCommand(c => c.ListHeaders(range));
    public NntpMultiLineResponse ListHeaders() => ExecuteCommand(c => c.ListHeaders());
    public NntpGroupsResponse ListCounts() => ExecuteCommand(c => c.ListCounts());
    public NntpGroupsResponse ListCounts(string wildmat) => ExecuteCommand(c => c.ListCounts(wildmat));
    public NntpMultiLineResponse ListDistributions() => ExecuteCommand(c => c.ListDistributions());
    public NntpMultiLineResponse ListModerators() => ExecuteCommand(c => c.ListModerators());
    public NntpMultiLineResponse ListMotd() => ExecuteCommand(c => c.ListMotd());
    public NntpMultiLineResponse ListSubscriptions() => ExecuteCommand(c => c.ListSubscriptions());
    public NntpGroupsResponse ListActive() => ExecuteCommand(c => c.ListActive());
    public NntpGroupsResponse ListActive(string wildmat) => ExecuteCommand(c => c.ListActive(wildmat));

    #endregion

    public void Dispose()
    {
        if (_disposed) return;

#pragma warning disable CA1031
        try
        {
            // Try to gracefully QUIT the NNTP session
            if (NntpConnected) _client.Quit();
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

    private T ExecuteCommand<T>(Func<NntpClient, T> command)
    {
        try
        {
            return command(Client);
        }
        catch (Exception)
        {
            HasError = true;
            throw;
        }
    }

    private void ExecuteCommand(Action<NntpClient> command)
    {
        try
        {
            command(Client);
        }
        catch (Exception)
        {
            HasError = true;
            throw;
        }
    }
}
