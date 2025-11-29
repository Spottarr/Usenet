using Usenet.Nntp.Contracts;
using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Writers;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Xunit;

namespace Usenet.Tests.Nntp.Writers;

public class ArticleWriterTests
{
    public static readonly IEnumerable<object[]> ArticleWriteData =
    [
        [
            new XSerializable<NntpArticle>(new NntpArticle(0, "1@example.com", "group", null, new List<string>(0))),
            new[] { "Message-ID: <1@example.com>", "Newsgroups: group", "", "." }
        ],
        [
            new XSerializable<NntpArticle>(new NntpArticle(0, "<2@example.com>", "group", null, new List<string>(0))),
            new[] { "Message-ID: <2@example.com>", "Newsgroups: group", "", "." }
        ],
        [
            new XSerializable<NntpArticle>(new NntpArticle(0, "3@example.com", "group",
                new MultiValueDictionary<string, string> { { "From", "\"Demo User\" <nobody@example.net>" }, },
                new List<string> { "This is just a test article." })),
            new[]
            {
                "Message-ID: <3@example.com>", "Newsgroups: group", "From: \"Demo User\" <nobody@example.net>", "", "This is just a test article.", "."
            }
        ],
        [
            new XSerializable<NntpArticle>(new NntpArticle(0, "4@example.com", "group", new MultiValueDictionary<string, string>
            {
                { "Message-ID", "<9999@example.com>" }, // not allowed, should be ignored
            }, new List<string> { "This is just a test article." })),
            new[] { "Message-ID: <4@example.com>", "Newsgroups: group", "", "This is just a test article.", "." }
        ],
        [
            new XSerializable<NntpArticle>(new NntpArticle(0, "5@example.com", "group", new MultiValueDictionary<string, string>
            {
                { "Message-ID", "9999@example.com" }, // not allowed, should be ignored
            }, new List<string> { "This is just a test article." })),
            new[] { "Message-ID: <5@example.com>", "Newsgroups: group", "", "This is just a test article.", "." }
        ]
    ];

    [Theory]
    [MemberData(nameof(ArticleWriteData))]
    internal void ArticleShouldBeWrittenCorrectly(XSerializable<NntpArticle> article, string[] expectedLines)
    {
        using var connection = new MockConnection();
        ArticleWriter.Write(connection, article.Object);
        Assert.Equal(expectedLines, connection.GetLines());
    }
}

internal sealed class MockConnection : INntpConnection
{
    private readonly List<string> _lines = [];

    public void Dispose()
    {
    }

    public Task<TResponse> ConnectAsync<TResponse>(string hostname, int port, bool useSsl, IResponseParser<TResponse> parser, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public TResponse Command<TResponse>(string command, IResponseParser<TResponse> parser)
    {
        throw new NotImplementedException();
    }

    public TResponse MultiLineCommand<TResponse>(string command, IMultiLineResponseParser<TResponse> parser)
    {
        throw new NotImplementedException();
    }

    public void WriteLine(string line)
    {
        _lines.Add(line);
    }

    public CountingStream Stream => throw new NotImplementedException();

    public TResponse GetResponse<TResponse>(IResponseParser<TResponse> parser)
    {
        throw new NotImplementedException();
    }

    public string[] GetLines()
    {
        return _lines.ToArray();
    }
}
