using System.Buffers;
using JetBrains.Annotations;
using Usenet.Util;

namespace Usenet.Yenc;

/// <summary>
/// Represents a single yEnc-encoded part decoded into a pooled <c>Data</c> buffer.
/// The decoded data is backed by a buffer rented from <see cref="ArrayPool{T}"/>; the
/// <see cref="Data"/> view is only valid until the part is disposed, after which the buffer is
/// returned to the pool. Always dispose the part (preferably with <c>using</c>/<c>await using</c>);
/// reading <see cref="Data"/> after disposal throws <see cref="ObjectDisposedException"/> rather than
/// returning recycled bytes, and a forgotten part is recovered by a finalizer that returns the buffer
/// and increments <see cref="PooledBufferDiagnostics.LeakedBufferCount"/>.
/// </summary>
[PublicAPI]
public sealed class YencPart : IDisposable, IAsyncDisposable
{
    private readonly PooledBuffer _buffer;
    private bool _disposed;

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
    /// The binary data obtained by decoding the yEnc-encoded part. The returned memory is backed by a
    /// pooled buffer and is only valid until this <see cref="YencPart"/> is disposed; reading it
    /// afterwards throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    public ReadOnlyMemory<byte> Data
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _buffer.Content;
        }
    }

    internal YencPart(YencHeader header, YencFooter? footer, byte[] buffer, int length)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(buffer);

        Header = header;
        Footer = footer;
        _buffer = new PooledBuffer(buffer, 0, length);
    }

    /// <summary>
    /// Returns the pooled <c>Data</c> buffer to the <see cref="ArrayPool{T}"/> it was rented from.
    /// After disposal the <see cref="Data"/> view is no longer valid.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ((IDisposable)_buffer).Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose"/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Safety net for a part that was never disposed: returns the pooled buffer and increments
    /// <see cref="PooledBufferDiagnostics.LeakedBufferCount"/> so a forgotten part cannot permanently
    /// starve the pool.
    /// </summary>
    ~YencPart() => _buffer.ReturnLeaked();
}
