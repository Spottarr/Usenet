using Usenet.Util;

namespace Usenet.Extensions;

internal static class StreamExtensions
{
    public static byte[] ReadAllBytes(this Stream stream)
    {
        Guard.ThrowIfNull(stream, nameof(stream));

        if (stream.CanSeek)
        {
            stream.Seek(0L, SeekOrigin.Begin);
        }

        if (stream is MemoryStream memoryStream)
        {
            return memoryStream.ToArray();
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public static ValueTask<int> ReadByteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<int>(stream.ReadAsync(buffer, 0, 1, cancellationToken));
#else
        return stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
#endif
    }
}
