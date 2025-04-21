namespace Usenet.Util;

internal static class IntShims
{
    internal static bool TryParse(ReadOnlySpan<char> s, out int result)
    {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        return int.TryParse(s, out result);
#else
        return int.TryParse(s.ToString(), out result);
#endif
    }
}