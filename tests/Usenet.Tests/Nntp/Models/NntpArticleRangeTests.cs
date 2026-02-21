using Usenet.Nntp.Models;
using Usenet.Tests.TestHelpers;
using Xunit;

namespace Usenet.Tests.Nntp.Models;

public class NntpArticleRangeTests
{
    [Fact]
    internal void SingleArticleShouldHaveCorrectStringRepresentation()
    {
        const string expected = "8";
        var actual = NntpArticleRange.SingleArticle(8).ToString();
        Assert.Equal(expected, actual);
    }

    [Fact]
    internal void AllFollowingShouldHaveCorrectStringRepresentation()
    {
        const string expected = "8-";
        var actual = NntpArticleRange.AllFollowing(8).ToString();
        Assert.Equal(expected, actual);
    }

    [Fact]
    internal void RangeShouldHaveCorrectStringRepresentation()
    {
        const string expected = "8-88";
        var actual = NntpArticleRange.Range(8, 88).ToString();
        Assert.Equal(expected, actual);
    }

    public static readonly IEnumerable<object[]> EqualsWithSameValues =
    [
        [
            new XSerializable<NntpArticleRange>(NntpArticleRange.SingleArticle(8)),
            new XSerializable<NntpArticleRange>(NntpArticleRange.SingleArticle(8))
        ],
        [
            new XSerializable<NntpArticleRange>(NntpArticleRange.Range(8, 88)), new XSerializable<NntpArticleRange>(NntpArticleRange.Range(8, 88))
        ],
        [
            new XSerializable<NntpArticleRange>(NntpArticleRange.AllFollowing(8)),
            new XSerializable<NntpArticleRange>(NntpArticleRange.AllFollowing(8))
        ]
    ];

    [Theory]
    [MemberData(nameof(EqualsWithSameValues))]
    internal void EqualsWithSameValuesShouldReturnTrue(XSerializable<NntpArticleRange> range1, XSerializable<NntpArticleRange> range2)
    {
        Assert.Equal(range1.Object, range2.Object);
    }

    public static readonly IEnumerable<object[]> EqualsWithDifferentValues =
    [
        [
            new XSerializable<NntpArticleRange>(NntpArticleRange.SingleArticle(8)),
            new XSerializable<NntpArticleRange>(NntpArticleRange.SingleArticle(9))
        ],
        [
            new XSerializable<NntpArticleRange>(NntpArticleRange.Range(8, 88)), new XSerializable<NntpArticleRange>(NntpArticleRange.Range(9, 88))
        ],
        [
            new XSerializable<NntpArticleRange>(NntpArticleRange.AllFollowing(8)),
            new XSerializable<NntpArticleRange>(NntpArticleRange.AllFollowing(9))
        ]
    ];

    [Theory]
    [MemberData(nameof(EqualsWithDifferentValues))]
    internal void EqualsWithDifferentValuesShouldReturnFalse(XSerializable<NntpArticleRange> range1, XSerializable<NntpArticleRange> range2)
    {
        Assert.NotEqual(range1.Object, range2.Object);
    }
}
