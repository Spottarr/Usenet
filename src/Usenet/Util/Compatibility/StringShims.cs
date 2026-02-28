namespace Usenet.Util.Compatibility;

internal static class StringShims
{
    internal static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1)
    {
#if NET5_0_OR_GREATER
        return string.Concat(str0, str1);
#else
        return string.Concat(str0.ToString(), str1.ToString());
#endif
    }

    internal static string[] Split(
        this string input,
        string separator,
        StringSplitOptions options = StringSplitOptions.None
    )
    {
#if NETSTANDARD2_0
        return input.Split([separator], options);
#else
        return input.Split(separator, options);
#endif
    }

    internal static string[] Split(
        this string input,
        char separator,
        StringSplitOptions options = StringSplitOptions.None
    )
    {
#if NETSTANDARD2_0
        return input.Split([separator], options);
#else
        return input.Split(separator, options);
#endif
    }
}
