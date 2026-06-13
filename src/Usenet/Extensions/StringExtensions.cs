using System.Text.RegularExpressions;

namespace Usenet.Extensions;

/// <summary>
/// String extension methods.
/// </summary>
internal static class StringExtensions
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Converts a string safely to an integer. If the string does not represent a valid integer the result is null.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The integer value obtained from the string or null if the string does not represent a valid integer.</returns>
    public static int? ToIntSafe(this string? str) =>
        str != null && int.TryParse(str, out var value) ? value : null;

    /// <summary>
    /// Removes all whitespace from a string.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static string Pack(this string source) => WhitespaceRegex.Replace(source, string.Empty);
}
