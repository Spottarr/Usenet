using JetBrains.Annotations;

namespace Usenet.Nntp.Models;

/// <summary>
/// One entry of a <a href="https://tools.ietf.org/html/rfc6048#section-2.4">LIST MODERATORS</a>
/// result: a newsgroup pattern paired with the submission address template used when an article is
/// locally posted to a matching moderated newsgroup.
/// </summary>
[PublicAPI]
public sealed class NntpModerator
{
    /// <summary>The wildmat-style newsgroup pattern this entry applies to.</summary>
    public required string GroupPattern { get; init; }

    /// <summary>
    /// The submission address template (a <c>%s</c> placeholder is replaced with the newsgroup
    /// name with dots changed to dashes).
    /// </summary>
    public required string SubmissionAddress { get; init; }
}
