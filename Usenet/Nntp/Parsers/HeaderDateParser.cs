using System.Globalization;
using System.Text.RegularExpressions;

namespace Usenet.Nntp.Parsers;

public static class HeaderDateParser
{
    private const string DateTimeRegexString =
        @"(?:\s*"
        + @"(?<dayName>Sun|Mon|Tue|Wed|Thu|Fri|Sat),)?\s*"
        + @"(?<day>\d{1,2})\s+"
        + @"(?<month>Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+"
        + @"(?<year>\d{2,4})\s+"
        + @"(?<hour>\d{1,2}):(?<min>\d{1,2})(?::(?<sec>\d{1,2}))?\s*"
        + @"(?<tz>[+-]\d+|(?:UT|UTC|GMT|Z|EDT|EST|CDT|CST|MDT|MST|PDT|PST|A|N|M|Y|[A-Z]+)"
        + @")?";

    private static readonly Regex _dateTimeRegex = new(DateTimeRegexString, RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Parses header date/time strings as described in the
    /// <a href="https://tools.ietf.org/html/rfc5322#section-3.3">Date and Time Specification</a>.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static DateTimeOffset? Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var matches = _dateTimeRegex.Match(value);
        if (!matches.Success)
            throw new FormatException(Resources.Nntp.BadHeaderDateFormat);

        var day = int.Parse(matches.Groups["day"].Value, CultureInfo.InvariantCulture);
        var month = matches.Groups["month"].Value;
        var year = int.Parse(matches.Groups["year"].Value, CultureInfo.InvariantCulture);
        var hour = int.Parse(matches.Groups["hour"].Value, CultureInfo.InvariantCulture);
        var minute = int.Parse(matches.Groups["min"].Value, CultureInfo.InvariantCulture);
        _ = int.TryParse(matches.Groups["sec"].Value, out var second);
        var tz = matches.Groups["tz"].Value;
        var zone = ParseZone(tz);

        var monthIndex = 1 + Array.FindIndex(DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthNames,
            m => string.Equals(m, month, StringComparison.OrdinalIgnoreCase));

        if (matches.Groups["year"].Value.Length < 4)
        {
            year += GetCentury(year, monthIndex, day) * 100;
        }

        return new DateTimeOffset(year, monthIndex, day, hour, minute, second, 0, zone);
    }

    private static int GetCentury(int year, int month, int day)
    {
        var today = DateTime.UtcNow.Date;
        var currentCentury = today.Year / 100;
        return new DateTime(currentCentury * 100 + year, month, day, 0, 0, 0, DateTimeKind.Utc) > today
            ? currentCentury - 1
            : currentCentury;
    }

    private static TimeSpan ParseZone(string value)
    {
        // The time zone must be as specified in RFC822, https://tools.ietf.org/html/rfc822#section-5

        if (!short.TryParse(value, out var zone) && !TryParseZoneText(value, out zone))
            throw new FormatException(Resources.Nntp.BadHeaderDateFormat);

        if (zone is < -9999 or > 9999)
            throw new FormatException(Resources.Nntp.BadHeaderDateFormat);

        var minute = zone % 100;
        var hour = zone / 100;

        return TimeSpan.FromMinutes(hour * 60 + minute);
    }

    private static bool TryParseZoneText(string value, out short zone)
    {
        zone = value switch
        {
            // UTC and empty are not specified in RFC822, but allowing them since they are commonly used
            "UTC" or "UT" or "GMT" or "Z" or "" => 0000,
            "EDT" => -0400,
            "EST" or "CDT" => -0500,
            "CST" or "MDT" => -0600,
            "MST" or "PDT" => -0700,
            "PST" => -0800,
            "A" => -0100,
            "N" => +0100,
            "M" => -1200,
            "Y" => +1200,
            _ => 9999
        };

        return zone != 9999;
    }
}
