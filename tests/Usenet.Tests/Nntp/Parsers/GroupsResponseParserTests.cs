using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class GroupsResponseParserTests
{
    public static IEnumerable<
        Func<(int, string, int, int, string[], NntpGroup[])>
    > MultiLineParseData()
    {
        yield return () =>
            (
                231,
                "list of new newsgroups follows",
                231,
                (int)GroupStatusRequestType.Basic,
                ["alt.rfc-writers.recovery 4 1 y", "tx.natives.recovery 89 56 y"],
                [
                    new(
                        "alt.rfc-writers.recovery",
                        0,
                        1,
                        4,
                        NntpPostingStatus.PostingPermitted,
                        string.Empty,
                        []
                    ),
                    new(
                        "tx.natives.recovery",
                        0,
                        56,
                        89,
                        NntpPostingStatus.PostingPermitted,
                        string.Empty,
                        []
                    ),
                ]
            );

        yield return () =>
            (231, "list of new newsgroups follows", 231, (int)GroupStatusRequestType.Basic, [], []);

        yield return () =>
            (
                215,
                "list of new newsgroups follows",
                215,
                (int)GroupStatusRequestType.Extended,
                ["misc.test 3002322 3000234 1234 y", "rec.food.drink.tea 100 51 3 y"],
                [
                    new(
                        "misc.test",
                        1234,
                        3000234,
                        3002322,
                        NntpPostingStatus.PostingPermitted,
                        string.Empty,
                        []
                    ),
                    new(
                        "rec.food.drink.tea",
                        3,
                        51,
                        100,
                        NntpPostingStatus.PostingPermitted,
                        string.Empty,
                        []
                    ),
                ]
            );

        yield return () =>
            (
                215,
                "list of new newsgroups follows",
                215,
                (int)GroupStatusRequestType.Extended,
                [],
                []
            );
    }

    [Test]
    [MethodDataSource(nameof(MultiLineParseData))]
    internal async Task MultiLineResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        int validCode,
        int requestType,
        string[] lines,
        NntpGroup[] expectedGroups
    )
    {
        var response = new GroupsResponseParser(
            validCode,
            (GroupStatusRequestType)requestType
        ).Parse(responseCode, responseMessage, lines.ToList());

        await Assert.That(response.Groups).IsEquivalentTo(expectedGroups);
    }
}
