using JetBrains.Annotations;

namespace Usenet.Util;

/// <summary>
/// Diagnostics shared by every type that owns a pooled buffer (for example
/// <see cref="Usenet.Nntp.Responses.NntpArticleResponse"/> and <see cref="Usenet.Yenc.YencPart"/>).
/// </summary>
[PublicAPI]
public static class PooledBufferDiagnostics
{
    private static long _leakedBufferCount;

    /// <summary>
    /// The number of pooled buffers reclaimed by a finalizer because their owner was not disposed.
    /// A non-zero value is a diagnostic signal that a caller forgot to dispose a pooled-buffer owner
    /// (an article response or a decoded yEnc part); the buffer was recovered, but relying on the
    /// finalizer defeats the pooling and adds GC pressure.
    /// </summary>
    public static long LeakedBufferCount => Interlocked.Read(ref _leakedBufferCount);

    internal static void IncrementLeakedBufferCount() =>
        Interlocked.Increment(ref _leakedBufferCount);
}
