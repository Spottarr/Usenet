using System.Text;
using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;
using Usenet.Util;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class ArticleResponseParserTests
{
    public static IEnumerable<Func<(int, string, int, string[], NntpArticle)>> MultiLineParseData()
    {
        yield return () =>
            (
                220,
                "123 <123@poster.com>",
                (int)ArticleRequestType.Article,
                [],
                new NntpArticle(123, "<123@poster.com>", null, null, new List<string>(0))
            );

        yield return () =>
            (
                220,
                "123 <123@poster.com>",
                (int)ArticleRequestType.Article,
                [
                    "Path: pathost!demo!whitehouse!not-for-mail",
                    "From: \"Demo User\" <nobody@example.net>",
                    "",
                    "This is just a test article (1).",
                    "With two lines.",
                ],
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
            );

        yield return () =>
            (
                222,
                "123 <123@poster.com>",
                (int)ArticleRequestType.Body,
                ["This is just a test article (2).", "With two lines."],
                new NntpArticle(
                    123,
                    "<123@poster.com>",
                    null,
                    null,
                    new List<string> { "This is just a test article (2).", "With two lines." }
                )
            );

        yield return () =>
            (
                221,
                "123 <123@poster.com>",
                (int)ArticleRequestType.Head,
                ["Multi: line1", " line2", " line3", "Path: pathost!demo!whitehouse!not-for-mail"],
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
            );

        yield return () =>
            (
                221,
                "123 <123@poster.com>",
                (int)ArticleRequestType.Head,
                ["Invalid header line", "Path: pathost!demo!whitehouse!not-for-mail"],
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
            );
    }

    [Test]
    [MethodDataSource(nameof(MultiLineParseData))]
    internal async Task MultiLineResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        int requestType,
        string[] lines,
        NntpArticle expectedArticle
    )
    {
        var articleResponse = new ArticleResponseParser((ArticleRequestType)requestType).Parse(
            responseCode,
            responseMessage,
            lines.ToList()
        );
        var actualArticle = articleResponse.Article;
        await Assert.That(actualArticle).IsEqualTo(expectedArticle);
    }

    public static IEnumerable<Func<(int, string, int, string[])>> InvalidMultiLineParseData()
    {
        yield return () => (412, "No newsgroup selected", (int)ArticleRequestType.Article, []);
        yield return () =>
            (420, "No current article selected", (int)ArticleRequestType.Article, []);
        yield return () =>
            (423, "No article with that number", (int)ArticleRequestType.Article, []);
        yield return () => (430, "No such article found", (int)ArticleRequestType.Article, []);
    }

    [Test]
    [MethodDataSource(nameof(InvalidMultiLineParseData))]
    internal async Task InvalidMultiLineResponseShouldBeParsedCorrectly(
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
        await Assert.That(articleResponse.Article).IsNull();
    }

    /// <summary>
    /// While headers are being read eagerly, the body is lazily enumerated.
    /// This means that the body is not read from the stream until it is actually needed.
    /// Previously, the IEnumerator reading the response data was disposed.
    /// This causes the IEnumerable to return no data while the underlying stream still contains it.
    /// Any subsequent command would then receive this data that was still in the stream, causing them to fail.
    /// </summary>
    [Test]
    public async Task LazyEnumerationOfBodyShouldReadFromSourceStream()
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

        await Assert.That(articleResponse.Article).IsNotNull();
        var body = string.Concat(articleResponse.Article!.Body);

        await Assert.That(body).IsNotEmpty();
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
