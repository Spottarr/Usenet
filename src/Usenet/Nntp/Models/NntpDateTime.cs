using Usenet.Util;

namespace Usenet.Nntp.Models;

/// <summary>
/// Represents an NNTP datetime object.
/// </summary>
public class NntpDateTime : IEquatable<NntpDateTime>
{
    /// <summary>
    /// The value of the <see cref="NntpDateTime"/> object.
    /// </summary>
    public DateTimeOffset Value { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="NntpDateTime"/> class.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public NntpDateTime(DateTimeOffset value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="NntpDateTime"/> class.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public NntpDateTime(DateTime value)
    {
        Value = new DateTimeOffset(value);
    }

    /// <summary>
    /// Returns the text representation of the value formatted according to the NNTP specifications.
    /// </summary>
    /// <returns>The text representation of the value formatted according to the NNTP specifications.</returns>
    public override string ToString() => $"{Value.ToUniversalTime():yyyyMMdd HHmmss} GMT";

    /// <summary>
    /// Converts a <see cref="NntpDateTime"/> implicitly to a string.
    /// </summary>
    /// <param name="dateTime">The <see cref="NntpDateTime"/> to convert.</param>
    public static implicit operator string(NntpDateTime dateTime)
    {
        Guard.ThrowIfNull(dateTime);
        return dateTime.ToString();
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> implicitly to a <see cref="NntpDateTime"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator NntpDateTime(DateTime value) => FromDateTime(value);

    /// <summary>
    /// Converts a <see cref="DateTime"/> implicitly to a <see cref="NntpDateTime"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static NntpDateTime FromDateTime(DateTime value) => new(value);

    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> implicitly to a <see cref="NntpDateTime"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator NntpDateTime(DateTimeOffset value) => FromDateTimeOffset(value);

    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> implicitly to a <see cref="NntpDateTime"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static NntpDateTime FromDateTimeOffset(DateTimeOffset value) => new(value);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => EqualityComparer<DateTimeOffset>.Default.GetHashCode(Value);

    /// <summary>
    /// Returns a value indicating whether this instance is equal to the specified <see cref="NntpDateTime"/> value.
    /// </summary>
    /// <param name="other">A <see cref="NntpDateTime"/> object to compare to this instance.</param>
    /// <returns>true if <paramref name="other" /> has the same value as this instance; otherwise, false.</returns>
    public bool Equals(NntpDateTime? other) => other is not null && Value == other.Value;

    /// <summary>
    /// Returns a value indicating whether this instance is equal to the specified <see cref="object"/> value.
    /// </summary>
    /// <param name="obj">An <see cref="object"/> to compare to this instance.</param>
    /// <returns>true if <paramref name="obj" /> has the same value as this instance; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as NntpDateTime);

    /// <summary>
    /// Returns a value indicating whether the frst <see cref="NntpDateTime"/> value is equal to the second <see cref="NntpDateTime"/> value.
    /// </summary>
    /// <param name="first">The first <see cref="NntpDateTime"/>.</param>
    /// <param name="second">The second <see cref="NntpDateTime"/>.</param>
    /// <returns>true if <paramref name="first"/> has the same value as <paramref name="second"/>; otherwise false.</returns>
    public static bool operator ==(NntpDateTime? first, NntpDateTime? second) =>
        first?.Equals(second) ?? second is null;

    /// <summary>
    /// Returns a value indicating whether the frst <see cref="NntpDateTime"/> value is unequal to the second <see cref="NntpDateTime"/> value.
    /// </summary>
    /// <param name="first">The first <see cref="NntpDateTime"/>.</param>
    /// <param name="second">The second <see cref="NntpDateTime"/>.</param>
    /// <returns>true if <paramref name="first"/> has a different value than <paramref name="second"/>; otherwise false.</returns>
    public static bool operator !=(NntpDateTime? first, NntpDateTime? second) => !(first == second);
}
