using System.Globalization;
using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class HeaderDateParserTests
{
    public static IEnumerable<(string, DateTimeOffset)> ParseData()
    {
        yield return (
            "Mon, 1 May 2017 1:55",
            new DateTimeOffset(2017, 5, 1, 1, 55, 0, TimeSpan.Zero)
        );
        yield return (
            "1 May 2017 1:55:33",
            new DateTimeOffset(2017, 5, 1, 1, 55, 33, TimeSpan.Zero)
        );
        yield return (
            "01 May 2017 13:55",
            new DateTimeOffset(2017, 5, 1, 13, 55, 0, TimeSpan.Zero)
        );
        yield return (
            "01 May 2017 13:55:33",
            new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.Zero)
        );
        yield return (
            "01 May 2017 13:55:33 +0000",
            new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.Zero)
        );
        yield return (
            "01 May 2017 13:55:33 -0000",
            new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.Zero)
        );
        yield return (
            "01 May 2017 13:55:33 +0000 (UTC)",
            new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.Zero)
        );
        yield return (
            "01 May 2017 13:55:33 -0000 (UTC)",
            new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.Zero)
        );
        yield return (
            "01 May 2017 13:55:33 +0100",
            new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.FromHours(1))
        );
        yield return (
            "01 May 2017 13:55:33 -0100",
            new DateTimeOffset(2017, 5, 1, 13, 55, 33, TimeSpan.FromHours(-1))
        );
        yield return (
            "01 May 2017 13:55 +1030",
            new DateTimeOffset(2017, 5, 1, 13, 55, 0, TimeSpan.FromMinutes(10 * 60 + 30))
        );
        yield return (
            "01 May 2017 13:55 -1030",
            new DateTimeOffset(2017, 5, 1, 13, 55, 0, -TimeSpan.FromMinutes(10 * 60 + 30))
        );
        yield return (
            "01 May 2017 13:55+1030",
            new DateTimeOffset(2017, 5, 1, 13, 55, 0, TimeSpan.FromMinutes(10 * 60 + 30))
        );
        yield return (
            "01 May 2017 13:55-1030",
            new DateTimeOffset(2017, 5, 1, 13, 55, 0, -TimeSpan.FromMinutes(10 * 60 + 30))
        );
        yield return (
            "1 Jan 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Feb 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 2, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Mar 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 3, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Apr 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 4, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 May 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 5, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Jun 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 6, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Jul 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 7, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Aug 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 8, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Sep 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 9, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Oct 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 10, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Nov 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 11, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Dec 2017 00:00:00 +0000",
            new DateTimeOffset(2017, 12, 1, 0, 0, 0, TimeSpan.Zero)
        );
        // Valid timezone formats
        yield return (
            "1 Jan 2020 00:00:00 UTC",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Jan 2020 00:00:00 UT",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Jan 2020 00:00:00 GMT",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Jan 2020 00:00:00 Z",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)
        );
        yield return (
            "1 Jan 2020 00:00:00 EDT",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-04))
        );
        yield return (
            "1 Jan 2020 00:00:00 EST",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-05))
        );
        yield return (
            "1 Jan 2020 00:00:00 CDT",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-05))
        );
        yield return (
            "1 Jan 2020 00:00:00 CST",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-06))
        );
        yield return (
            "1 Jan 2020 00:00:00 MDT",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-06))
        );
        yield return (
            "1 Jan 2020 00:00:00 MST",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-07))
        );
        yield return (
            "1 Jan 2020 00:00:00 PDT",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-07))
        );
        yield return (
            "1 Jan 2020 00:00:00 PST",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-08))
        );
        yield return (
            "1 Jan 2020 00:00:00 A",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-01))
        );
        yield return (
            "1 Jan 2020 00:00:00 N",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(+01))
        );
        yield return (
            "1 Jan 2020 00:00:00 M",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-12))
        );
    }

    public static IEnumerable<string> TimezoneParseFailureData()
    {
        yield return "1 Jan 2020 00:00:00 BAD";
        yield return "1 Jan 2020 00:00:00 -10000";
        yield return "1 Jan 2020 00:00:00 +10000";
    }

    [Test]
    [MethodDataSource(nameof(ParseData))]
    internal async Task HeaderDateShouldBeParsedCorrectly(
        string headerDate,
        DateTimeOffset expectedDateTime
    )
    {
        var actualDateTime = HeaderDateParser.Parse(headerDate);
        await Assert.That(actualDateTime).IsEqualTo(expectedDateTime);
    }

    [Test]
    [MethodDataSource(nameof(TimezoneParseFailureData))]
    internal async Task HeaderDateShouldNotBeParsedCorrectly(string headerDate)
    {
        await Assert
            .That(() => HeaderDateParser.Parse(headerDate))
            .ThrowsExactly<FormatException>();
    }

    [Test]
    internal async Task ObsoleteTwoDigitYearBeforeCurrentDateShouldBeParsedCorrectly()
    {
        DateTimeOffset yesterday = new(DateTime.UtcNow.Date.AddDays(-1), TimeSpan.Zero);
        var headerValue =
            yesterday.ToString("dd MMM yy HH:mm:ss", CultureInfo.InvariantCulture) + " +0000";
        var actualDate = HeaderDateParser.Parse(headerValue).GetValueOrDefault();
        await Assert.That(actualDate).IsEqualTo(yesterday);
    }

    [Test]
    internal async Task ObsoleteTwoDigitYearAfterCurrentDateShouldBeParsedCorrectly()
    {
        DateTimeOffset tomorrow = new(DateTime.UtcNow.Date.AddDays(+1), TimeSpan.Zero);
        var expectedDate = tomorrow.AddYears(-100);
        var headerValue =
            tomorrow.ToString("dd MMM yy HH:mm:ss", CultureInfo.InvariantCulture) + " +0000";
        var actualDate = HeaderDateParser.Parse(headerValue).GetValueOrDefault();
        await Assert.That(actualDate).IsEqualTo(expectedDate);
    }

    [Test]
    internal async Task ObsoleteTwoDigitYearOnCurrentDateShouldBeParsedCorrectly()
    {
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var headerValue =
            today.ToString("dd MMM yy HH:mm:ss", CultureInfo.InvariantCulture) + " +0000";
        var actualDate = HeaderDateParser.Parse(headerValue).GetValueOrDefault();
        await Assert.That(actualDate).IsEqualTo(today);
    }
}
