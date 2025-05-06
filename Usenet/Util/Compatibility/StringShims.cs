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
}
