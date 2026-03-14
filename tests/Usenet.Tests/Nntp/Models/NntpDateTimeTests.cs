using Usenet.Nntp.Models;
using Xunit;

namespace Usenet.Tests.Nntp.Models;

public class NntpDateTimeTests
{
    public static readonly IEnumerable<object[]> DateTimeData =
    [
        ["20170523 153211 GMT", new DateTime(2017, 5, 23, 15, 32, 11, DateTimeKind.Utc)],
        [
            "20170523 153211 GMT",
            new DateTime(2017, 5, 23, 15, 32, 11, DateTimeKind.Utc).ToLocalTime(),
        ],
    ];

    [Theory]
    [MemberData(nameof(DateTimeData))]
    internal void DateTimeShouldBeConvertedToUsenetString(string expected, DateTime dateTime)
    {
        var actual = (NntpDateTime)dateTime;
        Assert.Equal(expected, actual);
    }

    public static readonly IEnumerable<object[]> DateTimeOffsetData =
    [
        ["20170523 153211 GMT", new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero)],
        [
            "20170523 153211 GMT",
            new DateTimeOffset(2017, 5, 23, 17, 32, 11, TimeSpan.FromHours(+2)),
        ],
    ];

    [Theory]
    [MemberData(nameof(DateTimeOffsetData))]
    internal void DateTimeOffsetShouldBeConvertedToUsenetString(
        string expected,
        DateTimeOffset dateTime
    )
    {
        var actual = (NntpDateTime)dateTime;
        Assert.Equal(expected, actual);
    }
}
