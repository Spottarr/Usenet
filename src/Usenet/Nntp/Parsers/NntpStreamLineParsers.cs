using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers;

/// <summary>
/// Per-line parsers for the streamed unbounded multi-line commands. Each parser turns a single
/// data-block line into a typed value (or skips it), so the range is never materialized.
/// </summary>
internal static class NntpStreamLineParsers
{
    private static readonly ILogger Log = Logger.Create<NntpClient>();

    /// <summary>
    /// Yields each data-block line verbatim. Used for commands whose lines are consumed as raw text
    /// (XOVER, XHDR, HDR, NEWNEWS, LIST NEWSGROUPS).
    /// </summary>
    public static bool Line(string line, out string value)
    {
        value = line;
        return true;
    }

    /// <summary>
    /// Parses a LISTGROUP data-block line into an article number, skipping unparseable lines.
    /// </summary>
    public static bool ArticleNumber(string line, out long value) => long.TryParse(line, out value);

    /// <summary>
    /// Parses a basic group information line (LIST ACTIVE) into an <see cref="NntpGroup"/>.
    /// </summary>
    public static bool BasicGroup(string line, out NntpGroup value)
    {
        var lineSplit = line.Split(' ');
        if (lineSplit.Length < 4)
        {
            Log.InvalidGroupBasicInformationLine(line);
            value = null!;
            return false;
        }

        _ = long.TryParse(lineSplit[1], out var highWaterMark);
        _ = long.TryParse(lineSplit[2], out var lowWaterMark);

        var postingStatus = PostingStatusParser.Parse(lineSplit[3], out var otherGroup);
        if (postingStatus == NntpPostingStatus.Unknown)
        {
            Log.InvalidPostingStatus(lineSplit[3], line);
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
