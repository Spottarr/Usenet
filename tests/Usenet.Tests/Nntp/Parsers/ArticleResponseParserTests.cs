using System.Text;
using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Xunit;

namespace Usenet.Tests.Nntp.Parsers;

public class ArticleResponseParserTests
{
    public static readonly IEnumerable<object[]> MultiLineParseData =
    [
        [
            220,
            "123 <123@poster.com>",
            (int)ArticleRequestType.Article,
            Array.Empty<string>(),
            new XSerializable<NntpArticle>(
                new NntpArticle(123, "<123@poster.com>", null, null, new List<string>(0))
            ),
        ],
        [
            220,
            "123 <123@poster.com>",
            (int)ArticleRequestType.Article,
            new[]
            {
                "Path: pathost!demo!whitehouse!not-for-mail",
                "From: \"Demo User\" <nobody@example.net>",
                "",
                "This is just a test article (1).",
                "With two lines.",
            },
            new XSerializable<NntpArticle>(
                new NntpArticle(
                    123,
                    "<123@poster.com>",
                    null,
                    new MultiValueDictionary<string, string>
                    {
                        { "Path", "pathost!demo!whitehouse!not-for-mail" },
                        { "From", "\"Demo User\" <nobody@example.net>" },
                    },
                    new List<string> { "This is just a test article (1).", "With two lines." }
                )
            ),
        ],
        [
            222,
            "123 <123@poster.com>",
            (int)ArticleRequestType.Body,
            new[] { "This is just a test article (2).", "With two lines." },
            new XSerializable<NntpArticle>(
                new NntpArticle(
                    123,
                    "<123@poster.com>",
                    null,
                    null,
                    new List<string> { "This is just a test article (2).", "With two lines." }
                )
            ),
        ],
        [
            221,
            "123 <123@poster.com>",
            (int)ArticleRequestType.Head,
            new[]
            {
                "Multi: line1",
                " line2",
                " line3",
                "Path: pathost!demo!whitehouse!not-for-mail",
            },
            new XSerializable<NntpArticle>(
                new NntpArticle(
                    123,
                    "<123@poster.com>",
                    null,
                    new MultiValueDictionary<string, string>
                    {
                        { "Multi", "line1 line2 line3" },
                        { "Path", "pathost!demo!whitehouse!not-for-mail" },
                    },
                    new List<string>(0)
                )
            ),
        ],
        [
            221,
            "123 <123@poster.com>",
            (int)ArticleRequestType.Head,
            new[] { "Invalid header line", "Path: pathost!demo!whitehouse!not-for-mail" },
            new XSerializable<NntpArticle>(
                new NntpArticle(
                    123,
                    "<123@poster.com>",
                    null,
                    new MultiValueDictionary<string, string>
                    {
                        { "Path", "pathost!demo!whitehouse!not-for-mail" },
                    },
                    new List<string>(0)
                )
            ),
        ],
    ];

    [Theory]
    [MemberData(nameof(MultiLineParseData))]
    internal void MultiLineResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        int requestType,
        string[] lines,
        XSerializable<NntpArticle> expected
    )
    {
        var expectedArticle = expected.Object;
        var articleResponse = new ArticleResponseParser((ArticleRequestType)requestType).Parse(
            responseCode,
            responseMessage,
            lines.ToList()
        );
        var actualArticle = articleResponse.Article;
        Assert.Equal(expectedArticle, actualArticle);
    }

    public static readonly IEnumerable<object[]> InvalidMultiLineParseData =
    [
        [412, "No newsgroup selected", (int)ArticleRequestType.Article, Array.Empty<string>()],
        [
            420,
            "No current article selected",
            (int)ArticleRequestType.Article,
            Array.Empty<string>(),
        ],
        [
            423,
            "No article with that number",
            (int)ArticleRequestType.Article,
            Array.Empty<string>(),
        ],
        [430, "No such article found", (int)ArticleRequestType.Article, Array.Empty<string>()],
    ];

    [Theory]
    [MemberData(nameof(InvalidMultiLineParseData))]
    internal void InvalidMultiLineResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        int requestType,
        string[] lines
    )
    {
        var articleResponse = new ArticleResponseParser((ArticleRequestType)requestType).Parse(
            responseCode,
            responseMessage,
            lines.ToList()
        );
        Assert.Null(articleResponse.Article);
    }

    /// <summary>
    /// While headers are being read eagerly, the body is lazily enumerated.
    /// This means that the body is not read from the stream until it is actually needed.
    /// Previously, the IEnumerator reading the response data was disposed.
    /// This causes the IEnumerable to return no data while the underlying stream still contains it.
    /// Any subsequent command would then receive this data that was still in the stream, causing them to fail.
    /// </summary>
    [Fact]
    public void LazyEnumerationOfBodyShouldReadFromSourceStream()
    {
        const string response = """
            FirstHeader: FirstValue
            SecondHeader: SecondValue

            This is an
             example of some
             body text
             as returned by
             the server.
            """;

        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(response));
        using var reader = new StreamReader(stream, Encoding.ASCII);

        var data = ReadMultiLineDataBlock(reader);
        var parser = new ArticleResponseParser(ArticleRequestType.Article);
        var articleResponse = parser.Parse(220, "", data);

        var body = string.Concat(articleResponse.Article.Body);

        Assert.NotEmpty(body);
    }

    /// <summary>
    /// Mimics <see cref="Usenet.Nntp.NntpConnection.ReadMultiLineDataBlockAsync"/>
    /// </summary>
    private static IEnumerable<string> ReadMultiLineDataBlock(StreamReader reader)
    {
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}
