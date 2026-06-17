using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class DistribPatsResponseParserTests
{
    [Test]
    public async Task ShouldParseWeightWildmatAndValue()
    {
        var patterns = new DistribPatsResponseParser().Parse(
            215,
            "information follows",
            ["10:local.*:local", "5:*:world", "notaweight:*:world", "missing:fields"]
        );

        // The non-numeric weight and the two-field line are skipped.
        await Assert.That(patterns.Count).IsEqualTo(2);
        await Assert.That(patterns[0].Weight).IsEqualTo(10);
        await Assert.That(patterns[0].Wildmat).IsEqualTo("local.*");
        await Assert.That(patterns[0].Value).IsEqualTo("local");
        await Assert.That(patterns[1].Weight).IsEqualTo(5);
    }

    [Test]
    public async Task ShouldReturnEmptyForNonSuccessResponse()
    {
        var patterns = new DistribPatsResponseParser().Parse(503, "program error", ["10:*:world"]);

        await Assert.That(patterns.Count).IsEqualTo(0);
    }
}
