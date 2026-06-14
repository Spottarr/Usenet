using System.Collections;
using JetBrains.Annotations;

namespace Usenet.Nntp.Models;

/// <summary>
/// Represents an order-preserving, case-insensitive collection of NNTP header fields.
/// A header key may occur more than once; every occurrence is kept in the order it was parsed.
/// The collection is backed by a flat array of key/value pairs and looks up values without allocating.
/// </summary>
[PublicAPI]
public sealed class NntpHeaderCollection
    : IReadOnlyList<KeyValuePair<string, string>>,
        IEquatable<NntpHeaderCollection>
{
    private readonly KeyValuePair<string, string>[] _headers;

    /// <summary>
    /// Represents the empty <see cref="NntpHeaderCollection"/>.
    /// </summary>
    public static NntpHeaderCollection Empty { get; } = new([]);

    internal NntpHeaderCollection(IReadOnlyCollection<KeyValuePair<string, string>> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        _headers = headers as KeyValuePair<string, string>[] ?? [.. headers];
    }

    /// <summary>
    /// Gets the number of header fields in the collection.
    /// </summary>
    public int Count => _headers.Length;

    /// <summary>
    /// Gets the header field at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the header field to get.</param>
    /// <returns>The header field at the specified index.</returns>
    public KeyValuePair<string, string> this[int index] => _headers[index];

    /// <summary>
    /// Determines whether the collection contains a header field with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true"/> if a header field with the key exists; otherwise, <see langword="false"/>.</returns>
    public bool Contains(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        foreach (var header in _headers)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(header.Key, key))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the value of the first header field with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">When this method returns, contains the value of the first matching header field, if found.</param>
    /// <returns><see langword="true"/> if a header field with the key exists; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(string key, out string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        foreach (var header in _headers)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(header.Key, key))
            {
                value = header.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Gets all values of the header fields with the specified key, in the order they occur.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>The values of every matching header field.</returns>
    public IEnumerable<string> GetValues(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        foreach (var header in _headers)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(header.Key, key))
                yield return header.Value;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the header fields in order.
    /// </summary>
    /// <returns>An enumerator for the header fields.</returns>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
        ((IEnumerable<KeyValuePair<string, string>>)_headers).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _headers.GetEnumerator();

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        // Order-independent so that two collections that are equal regardless of header order hash the same.
        var hash = 0;
        foreach (var header in _headers)
        {
            hash += HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(header.Key),
                StringComparer.Ordinal.GetHashCode(header.Value)
            );
        }

        return hash;
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to the specified <see cref="NntpHeaderCollection"/> value.
    /// Header order does not affect equality, but keys are compared case-insensitively and values are compared as a multiset per key.
    /// </summary>
    /// <param name="other">A <see cref="NntpHeaderCollection"/> object to compare to this instance.</param>
    /// <returns>true if <paramref name="other" /> has the same value as this instance; otherwise, false.</returns>
    public bool Equals(NntpHeaderCollection? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (_headers.Length != other._headers.Length)
            return false;

        // Multiset comparison: every header in this collection must be matched by a distinct header in the other.
        var matched = new bool[other._headers.Length];
        foreach (var header in _headers)
        {
            var found = false;
            for (var i = 0; i < other._headers.Length; i++)
            {
                if (matched[i] || !PairEquals(header, other._headers[i]))
                    continue;

                matched[i] = true;
                found = true;
                break;
            }

            if (!found)
                return false;
        }

        return true;
    }

    private static bool PairEquals(
        KeyValuePair<string, string> first,
        KeyValuePair<string, string> second
    ) =>
        StringComparer.OrdinalIgnoreCase.Equals(first.Key, second.Key)
        && StringComparer.Ordinal.Equals(first.Value, second.Value);

    /// <summary>
    /// Returns a value indicating whether this instance is equal to the specified <see cref="NntpHeaderCollection"/> value.
    /// </summary>
    /// <param name="obj">An <see cref="object"/> to compare to this instance.</param>
    /// <returns>true if <paramref name="obj" /> has the same value as this instance; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as NntpHeaderCollection);

    /// <summary>
    /// Returns a value indicating whether the first <see cref="NntpHeaderCollection"/> value is equal to the second <see cref="NntpHeaderCollection"/> value.
    /// </summary>
    /// <param name="first">The first <see cref="NntpHeaderCollection"/>.</param>
    /// <param name="second">The second <see cref="NntpHeaderCollection"/>.</param>
    /// <returns>true if <paramref name="first"/> has the same value as <paramref name="second"/>; otherwise false.</returns>
    public static bool operator ==(NntpHeaderCollection? first, NntpHeaderCollection? second) =>
        first?.Equals(second) ?? second is null;

    /// <summary>
    /// Returns a value indicating whether the first <see cref="NntpHeaderCollection"/> value is unequal to the second <see cref="NntpHeaderCollection"/> value.
    /// </summary>
    /// <param name="first">The first <see cref="NntpHeaderCollection"/>.</param>
    /// <param name="second">The second <see cref="NntpHeaderCollection"/>.</param>
    /// <returns>true if <paramref name="first"/> has a different value than <paramref name="second"/>; otherwise false.</returns>
    public static bool operator !=(NntpHeaderCollection? first, NntpHeaderCollection? second) =>
        !(first == second);
}
