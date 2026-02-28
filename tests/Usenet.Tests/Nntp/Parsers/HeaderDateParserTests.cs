using System.Globalization;
using Usenet.Nntp.Parsers;
using Xunit;

namespace Usenet.Tests.Nntp.Parsers;

public class HeaderDateParserTests
{
    public static readonly IEnumerable<object[]> ParseData =
    [
        ["Mon, 1 May 2017 1:55", new DateTimeOffset(2017, 5, 1, 1, 55, 0, TimeSpan.Zero)],
        ["1 May 2017 1:55:33", new DateTimeOffset(2017, 5, 1, 1, 55, 33, TimeSpan.Zero)],
        ["01 May 2017 13:55", new DateTimeOffset(2017, 5, 1, 13, 55, 0, TimeSpan.Zero)],
        ["01 May 2017 13:55:33", new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.Zero)],
        ["01 May 2017 13:55:33 +0000", new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.Zero)],
        ["01 May 2017 13:55:33 -0000", new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.Zero)],
        [
            "01 May 2017 13:55:33 +0000 (UTC)",
            new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.Zero),
        ],
        [
            "01 May 2017 13:55:33 -0000 (UTC)",
            new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.Zero),
        ],
        [
            "01 May 2017 13:55:33 +0100",
            new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.FromHours(1)),
        ],
        [
            "01 May 2017 13:55:33 -0100",
            new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.FromHours(-1)),
        ],
        [
            "01 May 2017 13:55 +1030",
            new DateTimeOffset(2017, 5, 1, 13, 55, 0, TimeSpan.FromMinutes(10 * 60 + 30)),
        ],
        [
            "01 May 2017 13:55 -1030",
            new DateTimeOffset(2017, 5, 1, 13, 55, 0, -TimeSpan.FromMinutes(10 * 60 + 30)),
        ],
        [
            "01 May 2017 13:55+1030",
            new DateTimeOffset(2017, 5, 1, 13, 55, 0, TimeSpan.FromMinutes(10 * 60 + 30)),
        ],
        [
            "01 May 2017 13:55-1030",
            new DateTimeOffset(2017, 5, 1, 13, 55, 0, -TimeSpan.FromMinutes(10 * 60 + 30)),
        ],
        ["1 Jan 2017 00:00:00 +0000", new DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Feb 2017 00:00:00 +0000", new DateTimeOffset(2017, 2, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Mar 2017 00:00:00 +0000", new DateTimeOffset(2017, 3, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Apr 2017 00:00:00 +0000", new DateTimeOffset(2017, 4, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 May 2017 00:00:00 +0000", new DateTimeOffset(2017, 5, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Jun 2017 00:00:00 +0000", new DateTimeOffset(2017, 6, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Jul 2017 00:00:00 +0000", new DateTimeOffset(2017, 7, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Aug 2017 00:00:00 +0000", new DateTimeOffset(2017, 8, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Sep 2017 00:00:00 +0000", new DateTimeOffset(2017, 9, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Oct 2017 00:00:00 +0000", new DateTimeOffset(2017, 10, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Nov 2017 00:00:00 +0000", new DateTimeOffset(2017, 11, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Dec 2017 00:00:00 +0000", new DateTimeOffset(2017, 12, 1, 0, 0, 0, TimeSpan.Zero)],
        // Valid timezone formats
        ["1 Jan 2020 00:00:00 UTC", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Jan 2020 00:00:00 UT", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Jan 2020 00:00:00 GMT", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)],
        ["1 Jan 2020 00:00:00 Z", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)],
        [
            "1 Jan 2020 00:00:00 EDT",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-04)),
        ],
        [
            "1 Jan 2020 00:00:00 EST",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-05)),
        ],
        [
            "1 Jan 2020 00:00:00 CDT",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-05)),
        ],
        [
            "1 Jan 2020 00:00:00 CST",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-06)),
        ],
        [
            "1 Jan 2020 00:00:00 MDT",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-06)),
        ],
        [
            "1 Jan 2020 00:00:00 MST",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-07)),
        ],
        [
            "1 Jan 2020 00:00:00 PDT",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-07)),
        ],
        [
            "1 Jan 2020 00:00:00 PST",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-08)),
        ],
        ["1 Jan 2020 00:00:00 A", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-01))],
        ["1 Jan 2020 00:00:00 N", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(+01))],
        ["1 Jan 2020 00:00:00 M", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-12))],
    ];

    public static readonly IEnumerable<object[]> TimezoneParseFailureData =
    [
        ["1 Jan 2020 00:00:00 BAD", typeof(FormatException)],
        ["1 Jan 2020 00:00:00 -10000", typeof(FormatException)],
        ["1 Jan 2020 00:00:00 +10000", typeof(FormatException)],
    ];

    public static readonly IEnumerable<object[]> CenturyData =
    [
        ["01 May 16 13:55:33 +0000", new DateTimeOffset(2016, 5, 1, 13, 55, 33, TimeSpan.Zero)],
        ["01 May 99 13:55:33 +0000", new DateTimeOffset(1999, 5, 1, 13, 55, 33, TimeSpan.Zero)],
    ];

    [Theory]
    [MemberData(nameof(ParseData))]
    internal void HeaderDateShouldBeParsedCorrectly(
        string headerDate,
        DateTimeOffset expectedDateTime
    )
    {
        var actualDateTime = HeaderDateParser.Parse(headerDate);
        Assert.Equal(expectedDateTime, actualDateTime);
    }

    [Theory]
    [MemberData(nameof(TimezoneParseFailureData))]
    internal void HeaderDateShouldNotBeParsedCorrectly(string headerDate, Type exceptionType)
    {
        Assert.Throws(exceptionType, () => HeaderDateParser.Parse(headerDate));
    }

    [Fact]
    internal void ObsoleteTwoDigitYearBeforeCurrentDateShouldBeParsedCorrectly()
    {
        DateTimeOffset yesterday = new(DateTime.UtcNow.Date.AddDays(-1), TimeSpan.Zero);
        var headerValue =
            yesterday.ToString("dd MMM yy HH:mm:ss", CultureInfo.InvariantCulture) + " +0000";
        var actualDate = HeaderDateParser.Parse(headerValue).GetValueOrDefault();
        Assert.Equal(yesterday, actualDate);
    }

    [Fact]
    internal void ObsoleteTwoDigitYearAfterCurrentDateShouldBeParsedCorrectly()
    {
        DateTimeOffset tomorrow = new(DateTime.UtcNow.Date.AddDays(+1), TimeSpan.Zero);
        var expectedDate = tomorrow.AddYears(-100);
        var headerValue =
            tomorrow.ToString("dd MMM yy HH:mm:ss", CultureInfo.InvariantCulture) + " +0000";
        var actualDate = HeaderDateParser.Parse(headerValue).GetValueOrDefault();
        Assert.Equal(expectedDate, actualDate);
    }

    [Fact]
    internal void ObsoleteTwoDigitYearOnCurrentDateShouldBeParsedCorrectly()
    {
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var headerValue =
            today.ToString("dd MMM yy HH:mm:ss", CultureInfo.InvariantCulture) + " +0000";
        var actualDate = HeaderDateParser.Parse(headerValue).GetValueOrDefault();
        Assert.Equal(today, actualDate);
    }
}
