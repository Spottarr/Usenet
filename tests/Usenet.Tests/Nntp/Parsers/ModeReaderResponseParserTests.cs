using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class ModeReaderResponseParserTests
{
    [Test]
    [Arguments(200, "Reader mode, posting permitted", NntpModeReaderResponseType.PostingAllowed)]
    [Arguments(
        201,
        "NNTP Service Ready, posting prohibited",
        NntpModeReaderResponseType.PostingProhibited
    )]
    [Arguments(502, "Transit service only", NntpModeReaderResponseType.ReadingServiceUnavailable)]
    [Arguments(999, "", NntpModeReaderResponseType.Unknown)]
    internal async Task ResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        NntpModeReaderResponseType expectedResponseType
    )
    {
        var modeReaderResponse = new ModeReaderResponseParser().Parse(
            responseCode,
            responseMessage
        );
        await Assert.That(modeReaderResponse.ResponseType).IsEqualTo(expectedResponseType);
    }
}
