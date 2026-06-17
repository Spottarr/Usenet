using JetBrains.Annotations;

namespace Usenet.Nntp.Models;

/// <summary>
/// One field in the overview database layout returned by
/// <a href="https://tools.ietf.org/html/rfc3977#section-8.4">LIST OVERVIEW.FMT</a>, describing how
/// the corresponding column of an <c>OVER</c>/<c>XOVER</c> row is laid out.
/// </summary>
[PublicAPI]
public sealed class NntpOverviewField
{
    /// <summary>
    /// The name of the field, for example <c>Subject</c> for a header field or <c>bytes</c> for a
    /// metadata item, with the trailing colon and the leading metadata colon removed.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether the field value is prefixed with the header name and a colon (the <c>:full</c>
    /// form). When false the value contains only the header content.
    /// </summary>
    public required bool IncludesHeaderName { get; init; }

    /// <summary>
    /// Whether the field is a metadata item (a name beginning with a colon on the wire, such as
    /// <c>:bytes</c> or <c>:lines</c>) rather than a header field.
    /// </summary>
    public required bool IsMetadata { get; init; }
}
