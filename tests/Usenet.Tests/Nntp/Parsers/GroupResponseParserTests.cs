using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class GroupResponseParserTests
{
    public static IEnumerable<Func<(int, string, NntpGroup)>> ParseData()
    {
        yield return () =>
            (
                211,
                "1234 3000234 3002322 misc.test",
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
                411,
                "example.is.sob.bradner.or.barber is unknown",
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
    [MethodDataSource(nameof(ParseData))]
    internal async Task ResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        NntpGroup expectedGroup
    )
    {
        var groupResponse = new GroupResponseParser().Parse(responseCode, responseMessage);
        await Assert.That(groupResponse.Group).IsEqualTo(expectedGroup);
    }
}
