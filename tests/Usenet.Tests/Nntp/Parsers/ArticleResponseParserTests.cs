using System.Buffers;
using Usenet.Nntp.Parsers;
using Usenet.Util;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class ArticleResponseParserTests
{
    private static readonly string[] ArticleBodyLines =
    [
        "This is just a test article (1).",
        "With two lines.",
    ];

    private static readonly string[] BodyLines =
    [
        "This is just a test article (2).",
        "With two lines.",
    ];

    /// <summary>
    /// Builds the contiguous, CRLF-terminated, pool-rented byte buffer the connection materializes for
    /// the byte-oriented read path (dot-stuffing already undone, terminating dot already removed). The
    /// parser takes ownership and the response returns the buffer to the pool when disposed.
    /// </summary>
    private static (byte[] Buffer, int Length) BuildDataBlock(string[] lines)
    {
        var bytes =
            lines.Length == 0
                ? []
                : UsenetEncoding.Default.GetBytes(string.Join("\r\n", lines) + "\r\n");
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(bytes.Length, 1));
        bytes.CopyTo(buffer, 0);
        return (buffer, bytes.Length);
    }

    [Test]
    public async Task ArticleShouldExposeHeadersAndBody()
    {
        string[] lines =
        [
            "Path: pathost!demo!whitehouse!not-for-mail",
            "From: \"Demo User\" <nobody@example.net>",
            "",
            "This is just a test article (1).",
            "With two lines.",
        ];
        var (buffer, length) = BuildDataBlock(lines);

        using var response = new ArticleResponseParser(ArticleRequestType.Article).Parse(
            220,
            "123 <123@poster.com>",
            buffer,
            length
        );

        await Assert.That(response.Success).IsTrue();
        await Assert.That(response.Number).IsEqualTo(123L);
        await Assert.That(response.MessageId.ToString()).IsEqualTo("<123@poster.com>");
        await Assert
            .That(response.Headers.GetValues("Path"))
            .Contains("pathost!demo!whitehouse!not-for-mail");
        await Assert
            .That(response.Headers.GetValues("From"))
            .Contains("\"Demo User\" <nobody@example.net>");
        await Assert.That(response.ReadBodyLines()).IsEquivalentTo(ArticleBodyLines);
    }

    [Test]
    public async Task BodyShouldExposeBytesAndLines()
    {
        string[] lines = ["This is just a test article (2).", "With two lines."];
        var (buffer, length) = BuildDataBlock(lines);

        using var response = new ArticleResponseParser(ArticleRequestType.Body).Parse(
            222,
            "123 <123@poster.com>",
            buffer,
            length
        );

        await Assert.That(response.Headers).IsEmpty();
        await Assert
            .That(UsenetEncoding.Default.GetString(response.Body.Span))
            .IsEqualTo("This is just a test article (2).\r\nWith two lines.\r\n");
        await Assert.That(response.ReadBodyLines()).IsEquivalentTo(BodyLines);
    }

    [Test]
    public async Task HeadShouldFoldContinuationLinesAndHaveEmptyBody()
    {
        string[] lines =
        [
            "Multi: line1",
            " line2",
            " line3",
            "Path: pathost!demo!whitehouse!not-for-mail",
        ];
        var (buffer, length) = BuildDataBlock(lines);

        using var response = new ArticleResponseParser(ArticleRequestType.Head).Parse(
            221,
            "123 <123@poster.com>",
            buffer,
            length
        );

        await Assert.That(response.Headers.GetValues("Multi")).Contains("line1 line2 line3");
        await Assert
            .That(response.Headers.GetValues("Path"))
            .Contains("pathost!demo!whitehouse!not-for-mail");
        await Assert.That(response.Body.Length).IsEqualTo(0);
        await Assert.That(response.ReadBodyLines()).IsEmpty();
    }

    [Test]
    public async Task HeadShouldSkipInvalidHeaderLine()
    {
        string[] lines = ["Invalid header line", "Path: pathost!demo!whitehouse!not-for-mail"];
        var (buffer, length) = BuildDataBlock(lines);

        using var response = new ArticleResponseParser(ArticleRequestType.Head).Parse(
            221,
            "123 <123@poster.com>",
            buffer,
            length
        );

        await Assert.That(response.Headers.Contains("Invalid header line")).IsFalse();
        await Assert
            .That(response.Headers.GetValues("Path"))
            .Contains("pathost!demo!whitehouse!not-for-mail");
    }

    [Test]
    public async Task ArticleShouldParseNewsgroupsHeaderIntoGroups()
    {
        string[] lines = ["Newsgroups: alt.test,alt.demo", "", "body"];
        var (buffer, length) = BuildDataBlock(lines);

        using var response = new ArticleResponseParser(ArticleRequestType.Article).Parse(
            220,
            "123 <123@poster.com>",
            buffer,
            length
        );

        await Assert.That(response.Groups.ToString()).Contains("alt.test");
        await Assert.That(response.Groups.ToString()).Contains("alt.demo");
    }

    public static IEnumerable<Func<(int, string, int)>> InvalidMultiLineParseData()
    {
        yield return () => (412, "No newsgroup selected", (int)ArticleRequestType.Article);
        yield return () => (420, "No current article selected", (int)ArticleRequestType.Article);
        yield return () => (423, "No article with that number", (int)ArticleRequestType.Article);
        yield return () => (430, "No such article found", (int)ArticleRequestType.Article);
    }

    [Test]
    [MethodDataSource(nameof(InvalidMultiLineParseData))]
    internal async Task InvalidResponseShouldNotBeSuccessful(
        int responseCode,
        string responseMessage,
        int requestType
    )
    {
        var parser = new ArticleResponseParser((ArticleRequestType)requestType);

        await Assert.That(parser.IsSuccessResponse(responseCode)).IsFalse();

        using var response = parser.ParseError(responseCode, responseMessage);
        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.Body.Length).IsEqualTo(0);
    }
}
