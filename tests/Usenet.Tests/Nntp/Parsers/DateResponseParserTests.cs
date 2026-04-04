using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class DateResponseParserTests
{
    public static IEnumerable<(int, string, DateTimeOffset)> ParseData()
    {
        yield return (
            111,
            "20170614110733",
            new DateTimeOffset(2017, 6, 14, 11, 7, 33, TimeSpan.Zero)
        );
        yield return (111, "2017xxxxx10733", DateTimeOffset.MinValue);
        yield return (111, "", DateTimeOffset.MinValue);
        yield return (999, "20170614110733", DateTimeOffset.MinValue);
    }

    [Test]
    [MethodDataSource(nameof(ParseData))]
    internal async Task ResponseShouldBeParsedCorrectly(
        int responseCode,
        string responseMessage,
        DateTimeOffset expectedDateTime
    )
    {
        var dateResponse = new DateResponseParser().Parse(responseCode, responseMessage);
        await Assert.That(dateResponse.DateTime).IsEqualTo(expectedDateTime);
    }
}
