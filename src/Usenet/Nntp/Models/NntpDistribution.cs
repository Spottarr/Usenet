using JetBrains.Annotations;

namespace Usenet.Nntp.Models;

/// <summary>
/// One entry of a <a href="https://tools.ietf.org/html/rfc6048#section-2.3">LIST DISTRIBUTIONS</a>
/// result: a value that is valid for the <c>Distribution</c> header of a posted article, paired
/// with a human-readable description of what it means.
/// </summary>
[PublicAPI]
public sealed class NntpDistribution
{
    /// <summary>The distribution value (the token used in the <c>Distribution</c> header).</summary>
    public required string Value { get; init; }

    /// <summary>The description of the distribution, or an empty string when none was provided.</summary>
    public required string Description { get; init; }
}
