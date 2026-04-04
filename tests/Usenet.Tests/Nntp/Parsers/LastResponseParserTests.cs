using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class LastResponseParserTests
{
    [Test]
    [Arguments(
        223,
        "123 <123@poster.com> retrieved",
        NntpLastResponseType.ArticleExists,
        123,
        "123@poster.com"
    )]
    [Arguments(412, "No newsgroup selected", NntpLastResponseType.NoGroupSelected, 0, "")]
    [Arguments(
        420,
        "No current article selected",
        NntpLastResponseType.CurrentArticleInvalid,
        0,
        ""
    )]
    [Arguments(
        422,
        "No previous article to retrieve",
        NntpLastResponseType.NoPreviousArticleInGroup,
        0,
        ""
    )]
    [Arguments(999, "Unspecified response", NntpLastResponseType.Unknown, 0, "")]
    internal async Task ResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        NntpLastResponseType expectedResponseType,
        long expectedArticleNumber,
        string expectedMessageId
    )
    {
        var lastResponse = new LastResponseParser().Parse(responseCode, responseMessage);
        await Assert.That(lastResponse.ResponseType).IsEqualTo(expectedResponseType);
        await Assert.That(lastResponse.Number).IsEqualTo(expectedArticleNumber);
        await Assert.That(lastResponse.MessageId.Value).IsEqualTo(expectedMessageId);
    }
}
