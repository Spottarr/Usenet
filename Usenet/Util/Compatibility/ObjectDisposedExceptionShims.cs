namespace Usenet.Util.Compatibility;

internal static class ObjectDisposedExceptionShims
{
    public static void ThrowIf(bool condition, object instance)
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(condition, instance);
#else
        if (condition) throw new ObjectDisposedException(instance?.GetType().FullName);
#endif
    }
}
