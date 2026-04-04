using Usenet.Nzb;

namespace Usenet.Tests.Nzb;

internal sealed class NzbDocumentTests
{
    public static IEnumerable<(NzbDocument, NzbDocument)> EqualsWithSameValues()
    {
        yield return (new NzbDocument(null, null), new NzbDocument(null, null));

        yield return (
            new NzbDocument(
                null,
                [
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName1",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        null!
                    ),
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName2",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        null!
                    ),
                ]
            ),
            new NzbDocument(
                null,
                [
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName1",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        null!
                    ),
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName2",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        null!
                    ),
                ]
            )
        );

        yield return (
            new NzbDocument(
                null,
                [
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName3",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        null!
                    ),
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName4",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        null!
                    ),
                ]
            ),
            new NzbDocument(
                null,
                [
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName4",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        null!
                    ),
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName3",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        null!
                    ),
                ]
            )
        );
    }

    [Test]
    [MethodDataSource(nameof(EqualsWithSameValues))]
    internal async Task EqualsWithSameValuesShouldReturnTrue(
        NzbDocument expected,
        NzbDocument actual
    )
    {
        await Assert.That(actual).IsEqualTo(expected);
    }
}
