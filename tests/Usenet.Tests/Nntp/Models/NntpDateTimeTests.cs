using Usenet.Nntp.Models;

namespace Usenet.Tests.Nntp.Models;

internal sealed class NntpDateTimeTests
{
    public static IEnumerable<(string, DateTime)> DateTimeData()
    {
        yield return (
            "20170523 153211 GMT",
            new DateTime(2017, 5, 23, 15, 32, 11, DateTimeKind.Utc)
        );
        yield return (
            "20170523 153211 GMT",
            new DateTime(2017, 5, 23, 15, 32, 11, DateTimeKind.Utc).ToLocalTime()
        );
    }

    [Test]
    [MethodDataSource(nameof(DateTimeData))]
    internal async Task DateTimeShouldBeConvertedToUsenetString(string expected, DateTime dateTime)
    {
        var actual = (NntpDateTime)dateTime;
        await Assert.That((string)actual).IsEqualTo(expected);
    }

    public static IEnumerable<(string, DateTimeOffset)> DateTimeOffsetData()
    {
        yield return (
            "20170523 153211 GMT",
            new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero)
        );
        yield return (
            "20170523 153211 GMT",
            new DateTimeOffset(2017, 5, 23, 17, 32, 11, TimeSpan.FromHours(+2))
        );
    }

    [Test]
    [MethodDataSource(nameof(DateTimeOffsetData))]
    internal async Task DateTimeOffsetShouldBeConvertedToUsenetString(
        string expected,
        DateTimeOffset dateTime
    )
    {
        var actual = (NntpDateTime)dateTime;
        await Assert.That((string)actual).IsEqualTo(expected);
    }
}
