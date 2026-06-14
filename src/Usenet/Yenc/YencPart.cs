using System.Buffers;
using JetBrains.Annotations;

namespace Usenet.Yenc;

/// <summary>
/// Represents a single yEnc-encoded part decoded into a pooled <c>Data</c> buffer.
/// The decoded data is backed by a buffer rented from <see cref="ArrayPool{T}"/>; the
/// <see cref="Data"/> view is only valid until the part is disposed, after which the
/// buffer is returned to the pool. Reading <see cref="Data"/> after disposal is undefined.
/// </summary>
[PublicAPI]
public sealed class YencPart : IDisposable
{
    private byte[]? _buffer;
    private readonly int _length;

    /// <summary>
    /// Contains the information obtained from the =ybegin header line and =ypart part-header
    /// line if present.
    /// </summary>
    public YencHeader Header { get; }

    /// <summary>
    /// Contains the information obtained from the =yend footer line.
    /// </summary>
    public YencFooter? Footer { get; }

    /// <summary>
    /// The binary data obtained by decoding the yEnc-encoded part. The returned memory is
    /// backed by a pooled buffer and is only valid until this <see cref="YencPart"/> is disposed.
    /// </summary>
    public ReadOnlyMemory<byte> Data =>
        _buffer is null
            ? throw new ObjectDisposedException(nameof(YencPart))
            : _buffer.AsMemory(0, _length);

    internal YencPart(YencHeader header, YencFooter? footer, byte[] buffer, int length)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(buffer);

        Header = header;
        Footer = footer;
        _buffer = buffer;
        _length = length;
    }

    /// <summary>
    /// Returns the pooled <c>Data</c> buffer to the <see cref="ArrayPool{T}"/> it was rented from.
    /// After disposal the <see cref="Data"/> view is no longer valid.
    /// </summary>
    public void Dispose()
    {
        var buffer = _buffer;
        if (buffer is null)
        {
            return;
        }

        _buffer = null;
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
