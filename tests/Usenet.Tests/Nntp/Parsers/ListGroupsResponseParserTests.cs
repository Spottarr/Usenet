using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class ListGroupsResponseParserTests
{
    public static IEnumerable<Func<(int, string, string[], NntpGroup)>> MultiLineParseData()
    {
        yield return () =>
            (
                211,
                "1234 3000234 3002322 misc.test list follows",
                Array.Empty<string>(),
                new NntpGroup(
                    "misc.test",
                    1234,
                    3000234,
                    3002322,
                    NntpPostingStatus.Unknown,
                    string.Empty,
                    new List<long>(0)
                )
            );

        yield return () =>
            (
                211,
                "1234 3000234 3000236 misc.test list follows",
                new[] { "3000234", "3000235", "3000236" },
                new NntpGroup(
                    "misc.test",
                    1234,
                    3000234,
                    3000236,
                    NntpPostingStatus.Unknown,
                    string.Empty,
                    [3000234L, 3000235L, 3000236L]
                )
            );

        yield return () =>
            (
                411,
                "example.is.sob.bradner.or.barber is unknown",
                Array.Empty<string>(),
                new NntpGroup(
                    "",
                    0,
                    0,
                    0,
                    NntpPostingStatus.Unknown,
                    string.Empty,
                    new List<long>(0)
                )
            );

        yield return () =>
            (
                412,
                "no newsgroup selected",
                Array.Empty<string>(),
                new NntpGroup(
                    "",
                    0,
                    0,
                    0,
                    NntpPostingStatus.Unknown,
                    string.Empty,
                    new List<long>(0)
                )
            );
    }

    [Test]
    [MethodDataSource(nameof(MultiLineParseData))]
    internal async Task MultiLineResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        string[] lines,
        NntpGroup expectedGroup
    )
    {
        var groupResponse = new ListGroupResponseParser().Parse(
            responseCode,
            responseMessage,
            lines.ToList()
        );
        await Assert.That(groupResponse.Group).IsEqualTo(expectedGroup);
    }
}
