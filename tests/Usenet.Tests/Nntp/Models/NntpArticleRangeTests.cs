using Usenet.Nntp.Models;

namespace Usenet.Tests.Nntp.Models;

internal sealed class NntpArticleRangeTests
{
    [Test]
    internal async Task SingleArticleShouldHaveCorrectStringRepresentation()
    {
        const string expected = "8";
        var actual = NntpArticleRange.SingleArticle(8).ToString();
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    internal async Task AllFollowingShouldHaveCorrectStringRepresentation()
    {
        const string expected = "8-";
        var actual = NntpArticleRange.AllFollowing(8).ToString();
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    internal async Task RangeShouldHaveCorrectStringRepresentation()
    {
        const string expected = "8-88";
        var actual = NntpArticleRange.Range(8, 88).ToString();
        await Assert.That(actual).IsEqualTo(expected);
    }

    public static IEnumerable<(NntpArticleRange, NntpArticleRange)> EqualsWithSameValues()
    {
        yield return (NntpArticleRange.SingleArticle(8), NntpArticleRange.SingleArticle(8));
        yield return (NntpArticleRange.Range(8, 88), NntpArticleRange.Range(8, 88));
        yield return (NntpArticleRange.AllFollowing(8), NntpArticleRange.AllFollowing(8));
    }

    [Test]
    [MethodDataSource(nameof(EqualsWithSameValues))]
    internal async Task EqualsWithSameValuesShouldReturnTrue(
        NntpArticleRange range1,
        NntpArticleRange range2
    )
    {
        await Assert.That(range2).IsEqualTo(range1);
    }

    public static IEnumerable<(NntpArticleRange, NntpArticleRange)> EqualsWithDifferentValues()
    {
        yield return (NntpArticleRange.SingleArticle(8), NntpArticleRange.SingleArticle(9));
        yield return (NntpArticleRange.Range(8, 88), NntpArticleRange.Range(9, 88));
        yield return (NntpArticleRange.AllFollowing(8), NntpArticleRange.AllFollowing(9));
    }

    [Test]
    [MethodDataSource(nameof(EqualsWithDifferentValues))]
    internal async Task EqualsWithDifferentValuesShouldReturnFalse(
        NntpArticleRange range1,
        NntpArticleRange range2
    )
    {
        await Assert.That(range2).IsNotEqualTo(range1);
    }
}
