using System.Diagnostics.CodeAnalysis;

namespace Usenet.Extensions;

internal static class QueueExtensions
{
    public static bool TryDequeue<T>(this Queue<T> queue, [MaybeNullWhen(false)] out T result)
    {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        return queue.TryDequeue(out result);
#else
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
#endif
    }
}
