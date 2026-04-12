using System.Collections.Immutable;
using System.Text.Json;
using Usenet.Nntp.Models;

namespace Usenet.Tests.Nntp.Models;

internal sealed class NntpGroupTests
{
    public static IEnumerable<
        Func<(string, long, long, long, NntpPostingStatus, string, long[])>
    > SerializationData()
    {
        yield return () => ("test", 10, 2, 11, NntpPostingStatus.PostingPermitted, "", []);
        yield return () =>
            ("alt.rfc-writers.recovery", 0, 1, 4, NntpPostingStatus.PostingPermitted, "", []);
        yield return () =>
            ("tx.natives.recovery", 0, 56, 89, NntpPostingStatus.PostingPermitted, "", []);
        yield return () =>
            ("misc.test", 1234, 3000234, 3002322, NntpPostingStatus.PostingPermitted, "", []);
        yield return () =>
            ("rec.food.drink.tea", 3, 51, 100, NntpPostingStatus.PostingPermitted, "", []);
    }

    [Test]
    [MethodDataSource(nameof(SerializationData))]
    public async Task SerializedInstanceShouldBeDeserializedCorrectly(
        string name,
        long articleCount,
        long lowWaterMark,
        long highWaterMark,
        NntpPostingStatus postingStatus,
        string otherGroup,
        long[] articleNumbers
    )
    {
        var expected = new NntpGroup(
            name,
            articleCount,
            lowWaterMark,
            highWaterMark,
            postingStatus,
            otherGroup,
            articleNumbers.ToImmutableList()!
        );

        var json = JsonSerializer.Serialize(expected);
        var actual = JsonSerializer.Deserialize<NntpGroup>(json);
        await Assert.That(actual).IsEqualTo(expected);
    }

    public static IEnumerable<(NntpGroup, NntpGroup)> EqualsWithSameValues()
    {
        yield return (
            new NntpGroup(
                "group1",
                10,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                string.Empty,
                [1, 2, 3]
            ),
            new NntpGroup(
                "group1",
                10,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                string.Empty,
                [1, 2, 3]
            )
        );
        yield return (
            new NntpGroup("group1", 10, 1, 10, NntpPostingStatus.PostingPermitted, "other", []),
            new NntpGroup("group1", 10, 1, 10, NntpPostingStatus.PostingPermitted, "other", [])
        );
    }

    [Test]
    [MethodDataSource(nameof(EqualsWithSameValues))]
    internal async Task EqualsWithSameValuesShouldReturnTrue(NntpGroup group1, NntpGroup group2)
    {
        await Assert.That(group2).IsEqualTo(group1);
    }

    public static IEnumerable<(NntpGroup, NntpGroup)> EqualsWithDifferentValues()
    {
        yield return (
            new NntpGroup(
                "group1",
                10,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                "other",
                [1, 2, 3]
            ),
            new NntpGroup(
                "group2",
                10,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                "other",
                [1, 2, 3]
            )
        );
        yield return (
            new NntpGroup(
                "group1",
                10,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                "other",
                [1, 2, 3]
            ),
            new NntpGroup(
                "group1",
                11,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                "other",
                [1, 2, 3]
            )
        );
        yield return (
            new NntpGroup(
                "group1",
                10,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                "other",
                [1, 2, 3]
            ),
            new NntpGroup(
                "group1",
                10,
                2,
                10,
                NntpPostingStatus.PostingPermitted,
                "other",
                [1, 2, 3]
            )
        );
        yield return (
            new NntpGroup(
                "group1",
                10,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                "other",
                [1, 2, 3]
            ),
            new NntpGroup(
                "group1",
                10,
                1,
                11,
                NntpPostingStatus.PostingPermitted,
                "other",
                [1, 2, 3]
            )
        );
        yield return (
            new NntpGroup(
                "group1",
                10,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                "other",
                [1, 2, 3]
            ),
            new NntpGroup(
                "group1",
                10,
                1,
                10,
                NntpPostingStatus.ArticlesFromPeersNotPermitted,
                "other",
                [1, 2, 3]
            )
        );
        yield return (
            new NntpGroup(
                "group1",
                10,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                "other",
                [1, 2, 3]
            ),
            new NntpGroup(
                "group1",
                10,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                "aaaaa",
                [1, 2, 3]
            )
        );
        yield return (
            new NntpGroup(
                "group1",
                10,
                1,
                10,
                NntpPostingStatus.PostingPermitted,
                "other",
                [1, 2, 3]
            ),
            new NntpGroup("group1", 10, 1, 10, NntpPostingStatus.PostingPermitted, "other", [])
        );
    }

    [Test]
    [MethodDataSource(nameof(EqualsWithDifferentValues))]
    internal async Task EqualsWithDifferentValuesShouldReturnFalse(
        NntpGroup group1,
        NntpGroup group2
    )
    {
        await Assert.That(group2).IsNotEqualTo(group1);
    }
}
