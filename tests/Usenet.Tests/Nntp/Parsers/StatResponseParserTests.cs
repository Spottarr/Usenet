using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class StatResponseParserTests
{
    [Test]
    [Arguments(
        223,
        "123 <123@poster.com>",
        NntpStatResponseType.ArticleExists,
        123,
        "123@poster.com"
    )]
    [Arguments(412, "No newsgroup selected", NntpStatResponseType.NoGroupSelected, 0, "")]
    [Arguments(
        420,
        "No current article selected",
        NntpStatResponseType.CurrentArticleInvalid,
        0,
        ""
    )]
    [Arguments(
        423,
        "No article with that number",
        NntpStatResponseType.NoArticleWithThatNumber,
        0,
        ""
    )]
    [Arguments(
        430,
        "No such article found",
        NntpStatResponseType.NoArticleWithThatMessageId,
        0,
        ""
    )]
    [Arguments(999, "Unspecified response", NntpStatResponseType.Unknown, 0, "")]
    internal async Task ResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        NntpStatResponseType expectedResponseType,
        long expectedArticleNumber,
        string expectedMessageId
    )
    {
        var statResponse = new StatResponseParser().Parse(responseCode, responseMessage);
        await Assert.That(statResponse.ResponseType).IsEqualTo(expectedResponseType);
        await Assert.That(statResponse.Number).IsEqualTo(expectedArticleNumber);
        await Assert.That(statResponse.MessageId.Value).IsEqualTo(expectedMessageId);
    }
}
