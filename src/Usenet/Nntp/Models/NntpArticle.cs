using System.Collections.Immutable;
using Usenet.Extensions;
using Usenet.Util;
using HashCode = Usenet.Util.HashCode;

namespace Usenet.Nntp.Models;

/// <summary>
/// Represents an NNTP article.
/// </summary>
public class NntpArticle : IEquatable<NntpArticle>
{
    /// <summary>
    /// The number of the <see cref="NntpArticle"/> in the currently selected newsgroup.
    /// </summary>
    public long Number { get; }

    /// <summary>
    /// The message-id of the <see cref="NntpArticle"/>.
    /// </summary>
    public NntpMessageId MessageId { get; }

    /// <summary>
    /// The NNTP newsgroups this <see cref="NntpArticle"/> is posted in.
    /// </summary>
    public NntpGroups Groups { get; }

    /// <summary>
    /// The header dictionary of the <see cref="NntpArticle"/>.
    /// </summary>
    public ImmutableDictionary<string, ImmutableList<string>> Headers { get; }

    /// <summary>
    /// The body of the <see cref="NntpArticle"/>.
    /// </summary>
    public IImmutableList<string> Body { get; }

    /// <summary>
    /// Creates a new <see cref="NntpArticle"/> object.
    /// </summary>
    /// <param name="number">The number of the <see cref="NntpArticle"/>.</param>
    /// <param name="messageId">The <see cref="NntpMessageId"/> of the <see cref="NntpArticle"/>.</param>
    /// <param name="groups">The NNTP newsgroups this <see cref="NntpArticle"/> is posted in.</param>
    /// <param name="headers">The headers of the <see cref="NntpArticle"/>.</param>
    /// <param name="body">The body of the <see cref="NntpArticle"/>.</param>
    public NntpArticle(
        long number,
        NntpMessageId messageId,
        NntpGroups groups,
        IDictionary<string, ICollection<string>> headers,
        IList<string> body)
    {
        Number = number;
        MessageId = messageId ?? NntpMessageId.Empty;
        Groups = groups ?? NntpGroups.Empty;
        Headers = (headers ?? MultiValueDictionary<string, string>.EmptyIgnoreCase)
            .ToImmutableDictionary(x => x.Key, x => x.Value.ToImmutableList(), keyComparer: StringComparer.OrdinalIgnoreCase);
        Body = (body ?? []).ToImmutableList();
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => HashCode.Start
        .Hash(Number)
        .Hash(MessageId)
        .Hash(Groups);

    /// <summary>
    /// Returns a value indicating whether this instance is equal to the specified <see cref="NntpArticle"/> value.
    /// </summary>
    /// <param name="other">A <see cref="NntpArticle"/> object to compare to this instance.</param>
    /// <returns>true if <paramref name="other" /> has the same value as this instance; otherwise, false.</returns>
    public bool Equals(NntpArticle other)
    {
        if ((object)other == null)
        {
            return false;
        }

        var equals =
            Number.Equals(other.Number) &&
            MessageId.Equals(other.MessageId) &&
            Groups.Equals(other.Groups);

        if (!equals)
        {
            return false;
        }

        // compare headers
        foreach (var pair in Headers)
        {
            if (!other.Headers.TryGetValue(pair.Key, out var value) ||
                !pair.Value.ToImmutableHashSet().SetEquals(value))
            {
                return false;
            }
        }

        // compare body
        return Body.SequenceEqual(other.Body);
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to the specified <see cref="NntpArticle"/> value.
    /// </summary>
    /// <param name="obj">An <see cref="object"/> to compare to this instance.</param>
    /// <returns>true if <paramref name="obj" /> has the same value as this instance; otherwise, false.</returns>
    public override bool Equals(object obj) => Equals(obj as NntpArticle);

    /// <summary>
    /// Returns a value indicating whether the frst <see cref="NntpArticle"/> value is equal to the second <see cref="NntpArticle"/> value.
    /// </summary>
    /// <param name="first">The first <see cref="NntpArticle"/>.</param>
    /// <param name="second">The second <see cref="NntpArticle"/>.</param>
    /// <returns>true if <paramref name="first"/> has the same value as <paramref name="second"/>; otherwise false.</returns>
    public static bool operator ==(NntpArticle first, NntpArticle second) =>
        (object)first == null ? (object)second == null : first.Equals(second);

    /// <summary>
    /// Returns a value indicating whether the frst <see cref="NntpArticle"/> value is unequal to the second <see cref="NntpArticle"/> value.
    /// </summary>
    /// <param name="first">The first <see cref="NntpArticle"/>.</param>
    /// <param name="second">The second <see cref="NntpArticle"/>.</param>
    /// <returns>true if <paramref name="first"/> has a different value than <paramref name="second"/>; otherwise false.</returns>
    public static bool operator !=(NntpArticle first, NntpArticle second) => !(first == second);
}
