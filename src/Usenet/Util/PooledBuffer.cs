using System.Buffers;
using System.Runtime.InteropServices;

namespace Usenet.Util;

/// <summary>
/// Owns a contiguous region of a buffer rented from <see cref="ArrayPool{T}"/> and exposes it as a
/// fail-fast <see cref="ReadOnlyMemory{T}"/> view. The view is backed by this <see cref="MemoryManager{T}"/>,
/// so once the owner returns the buffer (on dispose, or via the finalizer safety net) any access to a
/// previously handed-out <see cref="ReadOnlyMemory{T}"/> — even one captured into a variable beforehand —
/// throws <see cref="ObjectDisposedException"/> instead of silently reading a recycled buffer.
/// </summary>
/// <remarks>This is the single place the pooled rent/return/leak-count contract lives; the finalizer that
/// triggers <see cref="ReturnLeaked"/> must stay on each owning type, since a finalizer cannot be factored
/// out into a helper.</remarks>
internal sealed class PooledBuffer : MemoryManager<byte>
{
    private readonly int _start;
    private readonly int _length;
    private byte[]? _array;

    /// <summary>
    /// Takes ownership of <paramref name="array"/>, exposing the <paramref name="length"/> bytes starting
    /// at <paramref name="start"/> as the buffer's content.
    /// </summary>
    public PooledBuffer(byte[] array, int start, int length)
    {
        ArgumentNullException.ThrowIfNull(array);
        _array = array;
        _start = start;
        _length = length;
    }

    /// <summary>The content region as a fail-fast memory view.</summary>
    public ReadOnlyMemory<byte> Content => CreateMemory(_length);

    /// <inheritdoc/>
    public override Span<byte> GetSpan() => ActiveArray.AsSpan(_start, _length);

    /// <inheritdoc/>
    public override unsafe MemoryHandle Pin(int elementIndex = 0)
    {
        var handle = GCHandle.Alloc(ActiveArray, GCHandleType.Pinned);
        var pointer = (byte*)handle.AddrOfPinnedObject() + _start + elementIndex;
        return new MemoryHandle(pointer, handle, this);
    }

    /// <inheritdoc/>
    public override void Unpin() { }

    /// <summary>
    /// Returns the buffer to the pool because the owner was finalized without being disposed, and records
    /// the leak via <see cref="PooledBufferDiagnostics"/>.
    /// </summary>
    public void ReturnLeaked()
    {
        if (TryTakeArray(out var array))
        {
            ReturnToPool(array);
            PooledBufferDiagnostics.IncrementLeakedBufferCount();
        }
    }

    /// <summary>Returns the buffer to the pool on the normal dispose path and invalidates the view.</summary>
    protected override void Dispose(bool disposing)
    {
        if (TryTakeArray(out var array))
        {
            ReturnToPool(array);
        }
    }

    private byte[] ActiveArray =>
        _array
        ?? throw new ObjectDisposedException(
            nameof(PooledBuffer),
            "The pooled buffer view is no longer valid because its owner was disposed."
        );

    private bool TryTakeArray(out byte[] array)
    {
        var current = _array;
        _array = null;
        array = current!;
        return current is not null;
    }

    private static void ReturnToPool(byte[] array)
    {
#if DEBUG
        // Poison the buffer so a use-after-dispose that slips past the fail-fast view (e.g. raw array
        // access) surfaces as obviously-wrong bytes in the library's own tests. Stripped from Release.
        array.AsSpan().Fill(0xDD);
#endif
        ArrayPool<byte>.Shared.Return(array);
    }
}
