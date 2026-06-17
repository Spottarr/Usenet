using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class OverviewFormatResponseParserTests
{
    [Test]
    public async Task ShouldParseHeaderMetadataAndFullFields()
    {
        var format = new OverviewFormatResponseParser().Parse(
            215,
            "overview format",
            ["Subject:", "From:", ":bytes", ":lines", "Xref:full", "", ":"]
        );

        // The trailing blank and the lone ":" line are skipped.
        await Assert.That(format.Fields.Count).IsEqualTo(5);

        await Assert.That(format.Fields[0].Name).IsEqualTo("Subject");
        await Assert.That(format.Fields[0].IsMetadata).IsFalse();
        await Assert.That(format.Fields[0].IncludesHeaderName).IsFalse();

        await Assert.That(format.Fields[2].Name).IsEqualTo("bytes");
        await Assert.That(format.Fields[2].IsMetadata).IsTrue();

        await Assert.That(format.Fields[4].Name).IsEqualTo("Xref");
        await Assert.That(format.Fields[4].IncludesHeaderName).IsTrue();
        await Assert.That(format.Fields[4].IsMetadata).IsFalse();
    }

    [Test]
    public async Task ShouldReturnEmptyForNonSuccessResponse()
    {
        var format = new OverviewFormatResponseParser().Parse(503, "program error", ["Subject:"]);

        await Assert.That(format.Fields.Count).IsEqualTo(0);
    }
}
