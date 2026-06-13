using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Usenet.Util;

/// <summary>
/// Utility class for guarding input.
/// Based on Kristian Hellang's yEnc project https://github.com/khellang/yEnc.
/// </summary>
internal static class Guard
{
    /// <summary>
    /// Throws an <exception cref="ArgumentNullException">ArgumentNullException</exception> if obj is null.
    /// </summary>
    /// <param name="obj">The object to check</param>
    /// <param name="name">The name of the object</param>
    /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
    public static void ThrowIfNull(
        [NotNull] object? obj,
        [CallerArgumentExpression(nameof(obj))] string? name = null
    )
    {
        ArgumentNullException.ThrowIfNull(obj, name);
    }

    /// <summary>
    /// Throws an <exception cref="ArgumentNullException">ArgumentNullException</exception> if the specified string is null.
    /// Throws an <exception cref="ArgumentException">ArgumentException</exception> if the specified string is empty.
    /// </summary>
    /// <param name="str">The string to check</param>
    /// <param name="name">The name of the string</param>
    /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
    /// <exception cref="ArgumentException">ArgumentException</exception>
    public static void ThrowIfNullOrEmpty(string str, string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(str, name);
    }

    /// <summary>
    /// Throws an <exception cref="ArgumentNullException">ArgumentNullException</exception> if the specified string is null.
    /// Throws an <exception cref="ArgumentException">ArgumentException</exception> if the specified string is empty or if it consists only of white-space characters.
    /// </summary>
    /// <param name="str">The string to check</param>
    /// <param name="name">The name of the string</param>
    /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
    /// <exception cref="ArgumentException">ArgumentException</exception>
    public static void ThrowIfNullOrWhiteSpace(string str, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(str, name);
    }

    /// <summary>
    /// Throws an <exception cref="ArgumentOutOfRangeException">ArgumentNullException</exception> if the value is negative or 0
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <param name="paramName">The name of the value</param>
    /// <exception cref="ArgumentOutOfRangeException">ArgumentOutOfRangeException</exception>
    public static void ThrowIfNegativeOrZero(long value, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
    }
}
