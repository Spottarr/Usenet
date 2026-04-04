using Usenet.Nntp.Models;

namespace Usenet.Tests.Nntp.Models;

internal sealed class NntpGroupOriginTests
{
    public static IEnumerable<(NntpGroupOrigin, NntpGroupOrigin)> EqualsWithSameValues()
    {
        yield return (
            new NntpGroupOrigin(
                "group1",
                new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero),
                "me"
            ),
            new NntpGroupOrigin(
                "group1",
                new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero),
                "me"
            )
        );
    }

    [Test]
    [MethodDataSource(nameof(EqualsWithSameValues))]
    internal async Task EqualsWithSameValuesShouldReturnTrue(
        NntpGroupOrigin group1,
        NntpGroupOrigin group2
    )
    {
        await Assert.That(group2).IsEqualTo(group1);
    }

    public static IEnumerable<(NntpGroupOrigin, NntpGroupOrigin)> EqualsWithDifferentValues()
    {
        yield return (
            new NntpGroupOrigin(
                "group1",
                new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero),
                "me"
            ),
            new NntpGroupOrigin(
                "group2",
                new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero),
                "me"
            )
        );
        yield return (
            new NntpGroupOrigin(
                "group1",
                new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero),
                "me"
            ),
            new NntpGroupOrigin(
                "group1",
                new DateTimeOffset(2017, 5, 24, 15, 32, 11, TimeSpan.Zero),
                "me"
            )
        );
        yield return (
            new NntpGroupOrigin(
                "group1",
                new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero),
                "me"
            ),
            new NntpGroupOrigin(
                "group1",
                new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero),
                "not me"
            )
        );
    }

    [Test]
    [MethodDataSource(nameof(EqualsWithDifferentValues))]
    internal async Task EqualsWithDifferentValuesShouldReturnFalse(
        NntpGroupOrigin group1,
        NntpGroupOrigin group2
    )
    {
        await Assert.That(group2).IsNotEqualTo(group1);
    }
}
