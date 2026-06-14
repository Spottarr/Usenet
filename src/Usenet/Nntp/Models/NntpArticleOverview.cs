using JetBrains.Annotations;

namespace Usenet.Nntp.Models;

/// <summary>
/// One row of an <c>OVER</c>/<c>XOVER</c> overview database result: the summary metadata for a single
/// article, parsed from the standard
/// <a href="https://tools.ietf.org/html/rfc3977#section-8.3">RFC 3977</a> overview format.
/// </summary>
[PublicAPI]
public sealed class NntpArticleOverview
{
    /// <summary>The number of the article in the currently selected newsgroup.</summary>
    public required long Number { get; init; }

    /// <summary>The article subject (the <c>Subject</c> header).</summary>
    public required string Subject { get; init; }

    /// <summary>The poster of the article (the <c>From</c> header).</summary>
    public required string From { get; init; }

    /// <summary>The date the article was posted (the <c>Date</c> header).</summary>
    public required DateTimeOffset Date { get; init; }

    /// <summary>The message-id of the article.</summary>
    public required NntpMessageId MessageId { get; init; }

    /// <summary>The message-ids this article references (the <c>References</c> header), as transmitted.</summary>
    public required string References { get; init; }

    /// <summary>The article byte count (the overview <c>:bytes</c> metadata item), or 0 when absent.</summary>
    public required long Bytes { get; init; }

    /// <summary>The article line count (the overview <c>:lines</c> metadata item), or 0 when absent.</summary>
    public required int Lines { get; init; }
}
