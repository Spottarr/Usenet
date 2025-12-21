using System.Diagnostics.CodeAnalysis;

namespace Usenet.Extensions;

internal static class QueueExtensions
{
    public static bool TryDequeue<T>(this Queue<T> queue, [MaybeNullWhen(false)] out T result)
    {
#if NETSTANDARD2_0
        try
        {
            result = queue.Dequeue();
            return true;
        }
        catch (InvalidOperationException)
        {
            result = default;
            return false;
        }
#else
        return queue.TryDequeue(out result);
#endif
    }
}
