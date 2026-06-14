using JetBrains.Annotations;

namespace Usenet.Nntp.Models;

/// <summary>
/// One row of an <c>HDR</c>/<c>XHDR</c> result: the value of a single header field for one article,
/// as returned by <a href="https://tools.ietf.org/html/rfc3977#section-8.5">RFC 3977</a>.
/// </summary>
[PublicAPI]
public sealed class NntpHeaderField
{
    /// <summary>
    /// The number of the article the value belongs to, or 0 when the field was requested by message-id.
    /// </summary>
    public required long ArticleNumber { get; init; }

    /// <summary>The value of the requested header field for the article.</summary>
    public required string Value { get; init; }
}
