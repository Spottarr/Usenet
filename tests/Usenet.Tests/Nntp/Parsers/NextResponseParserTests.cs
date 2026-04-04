using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class NextResponseParserTests
{
    [Test]
    [Arguments(
        223,
        "123 <123@poster.com> retrieved",
        NntpNextResponseType.ArticleExists,
        123,
        "123@poster.com"
    )]
    [Arguments(412, "No newsgroup selected", NntpNextResponseType.NoGroupSelected, 0, "")]
    [Arguments(
        420,
        "No current article selected",
        NntpNextResponseType.CurrentArticleInvalid,
        0,
        ""
    )]
    [Arguments(
        421,
        "No next article to retrieve",
        NntpNextResponseType.NoNextArticleInGroup,
        0,
        ""
    )]
    [Arguments(999, "Unspecified response", NntpNextResponseType.Unknown, 0, "")]
    internal async Task ResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        NntpNextResponseType expectedResponseType,
        long expectedArticleNumber,
        string expectedMessageId
    )
    {
        var nextResponse = new NextResponseParser().Parse(responseCode, responseMessage);
        await Assert.That(nextResponse.ResponseType).IsEqualTo(expectedResponseType);
        await Assert.That(nextResponse.Number).IsEqualTo(expectedArticleNumber);
        await Assert.That(nextResponse.MessageId.Value).IsEqualTo(expectedMessageId);
    }
}
