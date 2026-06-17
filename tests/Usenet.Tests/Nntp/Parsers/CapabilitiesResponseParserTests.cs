using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class CapabilitiesResponseParserTests
{
    private static readonly string[] DataBlock =
    [
        "VERSION 2",
        "READER",
        "OVER MSGID",
        "LIST ACTIVE NEWSGROUPS OVERVIEW.FMT",
        "",
        "   ",
        "IMPLEMENTATION INN 2.6.3",
    ];

    private static readonly string[] ExpectedOverVariants = ["MSGID"];
    private static readonly string[] ExpectedListVariants =
    [
        "ACTIVE",
        "NEWSGROUPS",
        "OVERVIEW.FMT",
    ];

    [Test]
    public async Task ShouldParseKeywordsAndArguments()
    {
        var capabilities = new CapabilitiesResponseParser().Parse(101, "capabilities", DataBlock);

        await Assert.That(capabilities.Supports("READER")).IsTrue();
        await Assert.That(capabilities.Supports("reader")).IsTrue();
        await Assert.That(capabilities.Supports("POST")).IsFalse();
        await Assert.That(capabilities.IsReader).IsTrue();
        await Assert.That(capabilities.Version).IsEqualTo("2");
        await Assert.That(capabilities.OverVariants).IsEquivalentTo(ExpectedOverVariants);
        await Assert.That(capabilities.ListVariants).IsEquivalentTo(ExpectedListVariants);
        await Assert.That(capabilities.Supports("OVER", "MSGID")).IsTrue();
        await Assert.That(capabilities.Supports("OVER", "ALL")).IsFalse();
    }

    [Test]
    public async Task ShouldSkipBlankLines()
    {
        var capabilities = new CapabilitiesResponseParser().Parse(101, "capabilities", DataBlock);

        await Assert.That(capabilities.Keywords.Count).IsEqualTo(5);
    }

    [Test]
    public async Task ShouldReturnEmptyForNonSuccessResponse()
    {
        var capabilities = new CapabilitiesResponseParser().Parse(
            500,
            "command not recognized",
            DataBlock
        );

        await Assert.That(capabilities.Keywords.Count).IsEqualTo(0);
        await Assert.That(capabilities.Supports("READER")).IsFalse();
    }
}
