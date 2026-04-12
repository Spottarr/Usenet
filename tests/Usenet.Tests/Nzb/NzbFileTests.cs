using Usenet.Nzb;

namespace Usenet.Tests.Nzb;

internal sealed class NzbFileTests
{
    public static IEnumerable<(NzbFile, NzbFile)> EqualsWithSameValues()
    {
        yield return (
            new NzbFile(
                "poster",
                "subject",
                "fileName1",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                []
            ),
            new NzbFile(
                "poster",
                "subject",
                "fileName1",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                []
            )
        );

        yield return (
            new NzbFile(
                "poster",
                "subject",
                "fileName2",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                [
                    new NzbSegment(1, 1000, 1200, "1234567890@base.msg"),
                    new NzbSegment(2, 2000, 2200, "aaaaaaaaaa@base.msg"),
                ]
            ),
            new NzbFile(
                "poster",
                "subject",
                "fileName2",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                [
                    new NzbSegment(1, 1000, 1200, "1234567890@base.msg"),
                    new NzbSegment(2, 2000, 2200, "aaaaaaaaaa@base.msg"),
                ]
            )
        );

        yield return (
            new NzbFile(
                "poster",
                "subject",
                "fileName3",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                [
                    new NzbSegment(1, 1000, 1200, "1234567890@base.msg"),
                    new NzbSegment(2, 2000, 2200, "aaaaaaaaaa@base.msg"),
                ]
            ),
            new NzbFile(
                "poster",
                "subject",
                "fileName3",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                [
                    new NzbSegment(2, 2000, 2200, "aaaaaaaaaa@base.msg"),
                    new NzbSegment(1, 1000, 1200, "1234567890@base.msg"),
                ]
            )
        );
    }

    [Test]
    [MethodDataSource(nameof(EqualsWithSameValues))]
    internal async Task EqualsWithSameValuesShouldReturnTrue(NzbFile expected, NzbFile actual)
    {
        await Assert.That(actual).IsEqualTo(expected);
    }

    public static IEnumerable<(NzbFile, NzbFile)> EqualsWithDifferentValues()
    {
        yield return (
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                []
            ),
            new NzbFile(
                "blabla",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                []
            )
        );

        yield return (
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                []
            ),
            new NzbFile(
                "poster",
                "blabla",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                []
            )
        );

        yield return (
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                []
            ),
            new NzbFile(
                "poster",
                "subject",
                "blabla",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                []
            )
        );

        yield return (
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                []
            ),
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                DateTimeOffset.MinValue,
                "group1;group2",
                []
            )
        );

        yield return (
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                []
            ),
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "blabla",
                []
            )
        );

        yield return (
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                [
                    new NzbSegment(1, 1000, 1200, "1234567890@base.msg"),
                    new NzbSegment(2, 2000, 2200, "aaaaaaaaaa@base.msg"),
                ]
            ),
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                [new NzbSegment(1, 1000, 1200, "1234567890@base.msg")]
            )
        );

        yield return (
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                [new NzbSegment(2, 2000, 2200, "aaaaaaaaaa@base.msg")]
            ),
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                [
                    new NzbSegment(2, 2000, 2200, "aaaaaaaaaa@base.msg"),
                    new NzbSegment(1, 1000, 1200, "1234567890@base.msg"),
                ]
            )
        );

        yield return (
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                [new NzbSegment(2, 2000, 2200, "aaaaaaaaaa@base.msg")]
            ),
            new NzbFile(
                "poster",
                "subject",
                "fileName",
                new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                "group1;group2",
                [new NzbSegment(2, 2000, 2200, "bbbbbbbbbb@base.msg")]
            )
        );
    }

    [Test]
    [MethodDataSource(nameof(EqualsWithDifferentValues))]
    internal async Task EqualsWithDifferentValuesShouldReturnFalse(NzbFile expected, NzbFile actual)
    {
        await Assert.That(actual).IsNotEqualTo(expected);
    }
}
