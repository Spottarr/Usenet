using JetBrains.Annotations;

namespace Usenet.Nntp.Models;

/// <summary>
/// One row of a <c>LIST NEWSGROUPS</c> result: a newsgroup name paired with its description, as
/// returned by <a href="https://tools.ietf.org/html/rfc3977#section-7.6.6">RFC 3977</a>.
/// </summary>
[PublicAPI]
public sealed class NntpNewsgroupDescription
{
    /// <summary>The name of the newsgroup.</summary>
    public required string Newsgroup { get; init; }

    /// <summary>The description of the newsgroup, or an empty string when none was provided.</summary>
    public required string Description { get; init; }
}
