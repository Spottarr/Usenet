using Usenet.Nntp.Contracts;
using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Writers;
using Usenet.Util;

namespace Usenet.Tests.Nntp.Writers;

internal sealed class ArticleWriterTests
{
    public static IEnumerable<Func<(NntpArticle, string[])>> ArticleWriteData()
    {
        yield return () =>
            (
                new NntpArticle(
                    0,
                    "1@example.com",
                    "group",
                    MultiValueDictionary<string, string>.EmptyIgnoreCase,
                    new List<string>(0)
                ),
                ["Message-ID: <1@example.com>", "Newsgroups: group", "", "."]
            );

        yield return () =>
            (
                new NntpArticle(
                    0,
                    "<2@example.com>",
                    "group",
                    MultiValueDictionary<string, string>.EmptyIgnoreCase,
                    new List<string>(0)
                ),
                ["Message-ID: <2@example.com>", "Newsgroups: group", "", "."]
            );

        yield return () =>
            (
                new NntpArticle(
                    0,
                    "3@example.com",
                    "group",
                    new MultiValueDictionary<string, string>
                    {
                        { "From", "\"Demo User\" <nobody@example.net>" },
                    },
                    new List<string> { "This is just a test article." }
                ),
                [
                    "Message-ID: <3@example.com>",
                    "Newsgroups: group",
                    "From: \"Demo User\" <nobody@example.net>",
                    "",
                    "This is just a test article.",
                    ".",
                ]
            );

        yield return () =>
            (
                new NntpArticle(
                    0,
                    "4@example.com",
                    "group",
                    new MultiValueDictionary<string, string>
                    {
                        { "Message-ID", "<9999@example.com>" },
                    },
                    new List<string> { "This is just a test article." }
                ),
                [
                    "Message-ID: <4@example.com>",
                    "Newsgroups: group",
                    "",
                    "This is just a test article.",
                    ".",
                ]
            );

        yield return () =>
            (
                new NntpArticle(
                    0,
                    "5@example.com",
                    "group",
                    new MultiValueDictionary<string, string>
                    {
                        { "Message-ID", "9999@example.com" },
                    },
                    new List<string> { "This is just a test article." }
                ),
                [
                    "Message-ID: <5@example.com>",
                    "Newsgroups: group",
                    "",
                    "This is just a test article.",
                    ".",
                ]
            );
    }

    [Test]
    [MethodDataSource(nameof(ArticleWriteData))]
    internal async Task ArticleShouldBeWrittenCorrectly(
        NntpArticle article,
        string[] expectedLines,
        CancellationToken cancellationToken
    )
    {
        using var connection = new MockConnection();
        await ArticleWriter.WriteAsync(connection, article, cancellationToken);
        await Assert.That(connection.GetLines()).IsEquivalentTo(expectedLines);
    }
}

internal sealed class MockConnection : INntpConnection
{
    private readonly List<string> _lines = [];

    public void Dispose() { }

    public Task<TResponse> ConnectAsync<TResponse>(
        string hostname,
        int port,
        bool useSsl,
        IResponseParser<TResponse> parser
    ) => throw new NotImplementedException();

    public Task<TResponse> ConnectAsync<TResponse>(
        string hostname,
        int port,
        bool useSsl,
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    public Task<TResponse> CommandAsync<TResponse>(
        string command,
        IResponseParser<TResponse> parser
    ) => throw new NotImplementedException();

    public Task<TResponse> CommandAsync<TResponse>(
        string command,
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    public Task<TResponse> MultiLineCommandAsync<TResponse>(
        string command,
        IMultiLineResponseParser<TResponse> parser
    ) => throw new NotImplementedException();

    public Task<TResponse> MultiLineCommandAsync<TResponse>(
        string command,
        IMultiLineResponseParser<TResponse> parser,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    public Task<TResponse> GetResponseAsync<TResponse>(IResponseParser<TResponse> parser) =>
        throw new NotImplementedException();

    public Task<TResponse> GetResponseAsync<TResponse>(
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    public Task WriteLineAsync(string line) => WriteLineAsync(line, CancellationToken.None);

    public Task WriteLineAsync(string line, CancellationToken cancellationToken)
    {
        _lines.Add(line);
        return Task.CompletedTask;
    }

    public CountingStream Stream => throw new NotImplementedException();

    public string[] GetLines() => _lines.ToArray();
}
