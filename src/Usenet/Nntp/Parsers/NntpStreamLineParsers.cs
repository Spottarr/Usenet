using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usenet.Extensions;
using Usenet.Nntp.Models;

namespace Usenet.Nntp.Parsers;

/// <summary>
/// Per-line parsers for the streamed unbounded multi-line commands. Each parser turns a single
/// data-block line into a typed row (or skips it), so the range is never materialized.
/// </summary>
internal sealed class NntpStreamLineParsers
{
    private readonly ILogger _log;

    public NntpStreamLineParsers(ILoggerFactory? loggerFactory = null) =>
        _log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<NntpStreamLineParsers>();

    /// <summary>
    /// Parses a LISTGROUP data-block line into an article number, skipping unparseable lines.
    /// </summary>
    public static bool ArticleNumber(string line, out long value) => long.TryParse(line, out value);

    /// <summary>
    /// Parses a NEWNEWS data-block line into a <see cref="NntpMessageId"/>.
    /// </summary>
    public static bool MessageId(string line, out NntpMessageId value)
    {
        value = new NntpMessageId(line.Trim());
        return value.HasValue;
    }

    /// <summary>
    /// Parses an OVER/XOVER data-block line into an <see cref="NntpArticleOverview"/> using the
    /// standard RFC 3977 overview format, skipping (and logging) lines that cannot be parsed.
    /// </summary>
    public bool Overview(string line, out NntpArticleOverview value)
    {
        value = null!;

        // Article number, subject, from, date, message-id, references, :bytes, :lines. Only a
        // structurally broken line (too few fields or a non-numeric article number) is skipped; a
        // malformed date or byte/line count defaults rather than dropping the whole article.
        var fields = line.Split('\t');
        if (fields.Length < 8 || !long.TryParse(fields[0], out var number))
        {
            _log.InvalidOverviewLine(line);
            return false;
        }

        DateTimeOffset date = default;
        try
        {
            date = HeaderDateParser.Parse(fields[3]) ?? default;
        }
        catch (FormatException)
        {
            // A bad date should not lose the article; leave it at the default.
        }

        _ = long.TryParse(
            fields[6],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var bytes
        );
        _ = int.TryParse(
            fields[7],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var lines
        );

        value = new NntpArticleOverview
        {
            Number = number,
            Subject = fields[1],
            From = fields[2],
            Date = date,
            MessageId = new NntpMessageId(fields[4]),
            References = fields[5],
            Bytes = bytes,
            Lines = lines,
        };
        return true;
    }

    /// <summary>
    /// Parses an HDR/XHDR data-block line into an <see cref="NntpHeaderField"/> ("article-number value",
    /// where the number is 0 when the field was requested by message-id).
    /// </summary>
    public bool HeaderField(string line, out NntpHeaderField value)
    {
        value = null!;

        var separator = line.IndexOf(' ', StringComparison.Ordinal);
        if (separator < 0)
        {
            _log.InvalidHeaderFieldLine(line);
            return false;
        }

        if (!long.TryParse(line.AsSpan(0, separator), out var articleNumber))
        {
            _log.InvalidHeaderFieldLine(line);
            return false;
        }

        value = new NntpHeaderField
        {
            ArticleNumber = articleNumber,
            Value = line[(separator + 1)..],
        };
        return true;
    }

    /// <summary>
    /// Parses a LIST NEWSGROUPS data-block line into an <see cref="NntpNewsgroupDescription"/>
    /// ("group description", separated by whitespace).
    /// </summary>
    public bool NewsgroupDescription(string line, out NntpNewsgroupDescription value)
    {
        value = null!;

        var separator = line.AsSpan().IndexOfAny(' ', '\t');
        if (separator < 0)
        {
            _log.InvalidNewsgroupDescriptionLine(line);
            return false;
        }

        value = new NntpNewsgroupDescription
        {
            Newsgroup = line[..separator],
            Description = line[(separator + 1)..].Trim(),
        };
        return true;
    }

    /// <summary>
    /// Parses a basic group information line (LIST ACTIVE) into an <see cref="NntpGroup"/>.
    /// </summary>
    public bool BasicGroup(string line, out NntpGroup value)
    {
        var lineSplit = line.Split(' ');
        if (lineSplit.Length < 4)
        {
            _log.InvalidGroupBasicInformationLine(line);
            value = null!;
            return false;
        }

        _ = long.TryParse(lineSplit[1], out var highWaterMark);
        _ = long.TryParse(lineSplit[2], out var lowWaterMark);

        var postingStatus = PostingStatusParser.Parse(lineSplit[3], out var otherGroup);
        if (postingStatus == NntpPostingStatus.Unknown)
        {
            _log.InvalidPostingStatus(lineSplit[3], line);
        }

        value = new NntpGroup(
            lineSplit[0],
            0,
            lowWaterMark,
            highWaterMark,
            postingStatus,
            otherGroup,
            []
        );
        return true;
    }
}
