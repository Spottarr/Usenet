using System.Text.RegularExpressions;
using Usenet.Util;

namespace Usenet.Extensions;

/// <summary>
/// String extension methods.
/// </summary>
internal static class StringExtensions
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Throws an <exception cref="ArgumentNullException">ArgumentNullException</exception> if the specified string is null.
    /// Throws an <exception cref="ArgumentException">ArgumentException</exception> if the specified string is empty.
    /// </summary>
    /// <param name="str">The string to check</param>
    /// <param name="name">The name of the string</param>
    /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
    /// <exception cref="ArgumentException">ArgumentException</exception>
    public static string ThrowIfNullOrEmpty(this string str, string name)
    {
        Guard.ThrowIfNullOrEmpty(str, name);
        return str;
    }

    /// <summary>
    /// Throws an <exception cref="ArgumentNullException">ArgumentNullException</exception> if the specified string is null.
    /// Throws an <exception cref="ArgumentException">ArgumentException</exception> if the specified string is empty or if it consists only of white-space characters.
    /// </summary>
    /// <param name="str">The string to check</param>
    /// <param name="name">The name of the string</param>
    /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
    /// <exception cref="ArgumentException">ArgumentException</exception>
    public static string ThrowIfNullOrWhiteSpace(this string str, string name)
    {
        Guard.ThrowIfNullOrWhiteSpace(str, name);
        return str;
    }

    /// <summary>
    /// Converts a string safely to an integer. If the string does not represent a valid integer the result is null.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The integer value obtained from the string or null if the string does not represent a valid integer.</returns>
    public static int? ToIntSafe(this string? str) =>
        str != null && int.TryParse(str, out var value) ? value : null;

    /// <summary>
    /// Converts a string safely to an integer. If the string does not represent a valid integer the specified default value will be returned.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <param name="defaultValue">The default value to use when the string is not a valid integer.</param>
    /// <returns>The integer value obtained from the string or the specified default value if the string does not represent a valid integer.</returns>
    public static int ToIntSafe(this string? str, int defaultValue) =>
        str != null && int.TryParse(str, out var value) ? value : defaultValue;

    /// <summary>
    /// Removes all whitespace from a string.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static string Pack(this string source) => WhitespaceRegex.Replace(source, string.Empty);

    public static int IndexOf(this string source, char value, StringComparison comparisonType)
    {
#if NETSTANDARD2_0
        return source.IndexOf(value);
#else
        return source.IndexOf(value, comparisonType);
#endif
    }
}
