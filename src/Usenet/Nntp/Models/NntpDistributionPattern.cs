using JetBrains.Annotations;

namespace Usenet.Nntp.Models;

/// <summary>
/// One entry of a <a href="https://tools.ietf.org/html/rfc3977#section-7.6.5">LIST DISTRIB.PATS</a>
/// result: a weighted rule mapping a newsgroup pattern to a default <c>Distribution</c> header value.
/// The highest-weighted matching pattern wins.
/// </summary>
[PublicAPI]
public sealed class NntpDistributionPattern
{
    /// <summary>The weight of the rule; the highest-weighted matching pattern is used.</summary>
    public required int Weight { get; init; }

    /// <summary>The wildmat-style newsgroup pattern this rule matches against.</summary>
    public required string Wildmat { get; init; }

    /// <summary>The distribution value suggested when the pattern matches.</summary>
    public required string Value { get; init; }
}
