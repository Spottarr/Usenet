namespace Usenet.Util.Compatibility;

internal static class IntShims
{
    internal static bool TryParse(ReadOnlySpan<char> s, out int result)
    {
#if NETSTANDARD2_0
        return int.TryParse(s.ToString(), out result);
#else
        return int.TryParse(s, out result);
#endif
    }
}
