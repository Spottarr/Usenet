using Usenet.Nzb;
using Usenet.Util;

namespace Usenet.Tests.Nzb;

internal sealed class NzbDocumentTests
{
    public static IEnumerable<(NzbDocument, NzbDocument)> EqualsWithSameValues()
    {
        yield return (
            new NzbDocument(MultiValueDictionary<string, string>.Empty, []),
            new NzbDocument(MultiValueDictionary<string, string>.Empty, [])
        );

        yield return (
            new NzbDocument(
                MultiValueDictionary<string, string>.Empty,
                [
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
                        "fileName2",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        []
                    ),
                ]
            ),
            new NzbDocument(
                MultiValueDictionary<string, string>.Empty,
                [
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
                        "fileName2",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        []
                    ),
                ]
            )
        );

        yield return (
            new NzbDocument(
                MultiValueDictionary<string, string>.Empty,
                [
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName3",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        []
                    ),
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName4",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        []
                    ),
                ]
            ),
            new NzbDocument(
                MultiValueDictionary<string, string>.Empty,
                [
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName4",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        []
                    ),
                    new NzbFile(
                        "poster",
                        "subject",
                        "fileName3",
                        new DateTimeOffset(2017, 12, 8, 22, 44, 0, TimeSpan.Zero),
                        "group1;group2",
                        []
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
