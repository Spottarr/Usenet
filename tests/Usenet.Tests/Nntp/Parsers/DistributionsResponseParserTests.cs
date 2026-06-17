using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class DistributionsResponseParserTests
{
    [Test]
    public async Task ShouldParseValueAndDescription()
    {
        var distributions = new DistributionsResponseParser().Parse(
            215,
            "information follows",
            ["world Every site in the world", "na North America", "   ", "local"]
        );

        // The blank line is skipped; "local" carries no description.
        await Assert.That(distributions.Count).IsEqualTo(3);
        await Assert.That(distributions[0].Value).IsEqualTo("world");
        await Assert.That(distributions[0].Description).IsEqualTo("Every site in the world");
        await Assert.That(distributions[2].Value).IsEqualTo("local");
        await Assert.That(distributions[2].Description).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ShouldReturnEmptyForNonSuccessResponse()
    {
        var distributions = new DistributionsResponseParser().Parse(
            503,
            "program error",
            ["world x"]
        );

        await Assert.That(distributions.Count).IsEqualTo(0);
    }
}
