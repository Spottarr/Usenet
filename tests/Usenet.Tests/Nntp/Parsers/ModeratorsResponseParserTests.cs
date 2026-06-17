using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class ModeratorsResponseParserTests
{
    [Test]
    public async Task ShouldParseGroupPatternAndAddress()
    {
        var moderators = new ModeratorsResponseParser().Parse(
            215,
            "information follows",
            ["comp.*:%s@example.com", "*:%s@moderators.example.net", "no-colon-line"]
        );

        // The line without a colon separator is skipped.
        await Assert.That(moderators.Count).IsEqualTo(2);
        await Assert.That(moderators[0].GroupPattern).IsEqualTo("comp.*");
        await Assert.That(moderators[0].SubmissionAddress).IsEqualTo("%s@example.com");
        await Assert.That(moderators[1].GroupPattern).IsEqualTo("*");
    }

    [Test]
    public async Task ShouldReturnEmptyForNonSuccessResponse()
    {
        var moderators = new ModeratorsResponseParser().Parse(503, "program error", ["*:%s@x"]);

        await Assert.That(moderators.Count).IsEqualTo(0);
    }
}
