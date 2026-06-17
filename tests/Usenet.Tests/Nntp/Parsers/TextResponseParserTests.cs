using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class TextResponseParserTests
{
    private static readonly string[] Commands = ["ARTICLE", "BODY", "HEAD"];

    [Test]
    public async Task ShouldExposeLinesAndJoinedText()
    {
        var response = new TextResponseParser(100).Parse(100, "help text follows", Commands);

        await Assert.That(response.Success).IsTrue();
        await Assert.That(response.Lines).IsEquivalentTo(Commands);
        await Assert.That(response.Text).IsEqualTo("ARTICLE\nBODY\nHEAD");
    }

    [Test]
    public async Task ShouldMarkNonSuccessResponse()
    {
        var response = new TextResponseParser(100).Parse(500, "command not recognized", []);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.Lines.Count).IsEqualTo(0);
        await Assert.That(response.Text).IsEqualTo(string.Empty);
    }
}
