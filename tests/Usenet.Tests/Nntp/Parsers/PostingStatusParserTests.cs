using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class PostingStatusParserTests
{
    [Test]
    [Arguments("y", NntpPostingStatus.PostingPermitted, "")]
    [Arguments("yes", NntpPostingStatus.PostingPermitted, "")]
    [Arguments("n", NntpPostingStatus.PostingNotPermitted, "")]
    [Arguments("no", NntpPostingStatus.PostingNotPermitted, "")]
    [Arguments("m", NntpPostingStatus.PostingsWillBeReviewed, "")]
    [Arguments("x", NntpPostingStatus.ArticlesFromPeersNotPermitted, "")]
    [Arguments("j", NntpPostingStatus.OnlyArticlesFromPeersPermittedNotFiledLocally, "")]
    [Arguments("=", NntpPostingStatus.OnlyArticlesFromPeersPermittedFiledLocally, "")]
    [Arguments(
        "=misc.test",
        NntpPostingStatus.OnlyArticlesFromPeersPermittedFiledLocally,
        "misc.test"
    )]
    [Arguments("b", NntpPostingStatus.Unknown, "")]
    [Arguments("bbbbb", NntpPostingStatus.Unknown, "")]
    [Arguments("", NntpPostingStatus.Unknown, "")]
    internal async Task InputShouldBeParsedCorrectly(
        string? input,
        NntpPostingStatus expectedStatus,
        string expectedOtherGroup
    )
    {
        var status = PostingStatusParser.Parse(input!, out var otherGroup);
        await Assert.That(status).IsEqualTo(expectedStatus);
        await Assert.That(otherGroup).IsEqualTo(expectedOtherGroup);
    }

    [Test]
    internal async Task NullInputShouldBeParsedCorrectly()
    {
        var status = PostingStatusParser.Parse(null!, out var otherGroup);
        await Assert.That(status).IsEqualTo(NntpPostingStatus.Unknown);
        await Assert.That(otherGroup).IsEqualTo("");
    }
}
