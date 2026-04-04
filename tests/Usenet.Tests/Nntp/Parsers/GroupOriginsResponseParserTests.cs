using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class GroupOriginsResponseParserTests
{
    public static IEnumerable<Func<(int, string, string[], NntpGroupOrigin[])>> MultiLineParseData()
    {
        yield return () =>
            (
                215,
                "information follows",
                new[]
                {
                    "misc.test 930445408 <creatme@isc.org>",
                    "alt.rfc-writers.recovery 930562309 <m@example.com>",
                    "tx.natives.recovery 930678923 <sob@academ.com>",
                },
                [
                    new NntpGroupOrigin(
                        "misc.test",
                        new DateTimeOffset(1999, 6, 27, 1, 3, 28, 0, TimeSpan.Zero),
                        "<creatme@isc.org>"
                    ),
                    new NntpGroupOrigin(
                        "alt.rfc-writers.recovery",
                        new DateTimeOffset(1999, 6, 28, 9, 31, 49, 0, TimeSpan.Zero),
                        "<m@example.com>"
                    ),
                    new NntpGroupOrigin(
                        "tx.natives.recovery",
                        new DateTimeOffset(1999, 6, 29, 17, 55, 23, 0, TimeSpan.Zero),
                        "<sob@academ.com>"
                    ),
                ]
            );
    }

    [Test]
    [MethodDataSource(nameof(MultiLineParseData))]
    internal async Task ResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        string[] lines,
        NntpGroupOrigin[] expectedGroupOrigins
    )
    {
        var response = new GroupOriginsResponseParser().Parse(
            responseCode,
            responseMessage,
            lines.ToList()
        );
        await Assert.That(response.GroupOrigins).IsEquivalentTo(expectedGroupOrigins);
    }
}
