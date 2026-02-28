using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;
using Xunit;

namespace Usenet.Tests.Nntp.Parsers;

public class PostingStatusParserTests
{
    [Theory]
    [InlineData("y", NntpPostingStatus.PostingPermitted, "")]
    [InlineData("yes", NntpPostingStatus.PostingPermitted, "")]
    [InlineData("n", NntpPostingStatus.PostingNotPermitted, "")]
    [InlineData("no", NntpPostingStatus.PostingNotPermitted, "")]
    [InlineData("m", NntpPostingStatus.PostingsWillBeReviewed, "")]
    [InlineData("x", NntpPostingStatus.ArticlesFromPeersNotPermitted, "")]
    [InlineData("j", NntpPostingStatus.OnlyArticlesFromPeersPermittedNotFiledLocally, "")]
    [InlineData("=", NntpPostingStatus.OnlyArticlesFromPeersPermittedFiledLocally, "")]
    [InlineData(
        "=misc.test",
        NntpPostingStatus.OnlyArticlesFromPeersPermittedFiledLocally,
        "misc.test"
    )]
    [InlineData("b", NntpPostingStatus.Unknown, "")]
    [InlineData("bbbbb", NntpPostingStatus.Unknown, "")]
    [InlineData("", NntpPostingStatus.Unknown, "")]
    [InlineData(null, NntpPostingStatus.Unknown, "")]
    internal void InputShouldBeParsedCorrectly(
        string? input,
        NntpPostingStatus expectedStatus,
        string expectedOtherGroup
    )
    {
        var status = PostingStatusParser.Parse(input!, out var otherGroup);
        Assert.Equal(expectedStatus, status);
        Assert.Equal(expectedOtherGroup, otherGroup);
    }
}
