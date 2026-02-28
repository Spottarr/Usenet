namespace Usenet.Util;

/// <summary>
/// Represents an enumerable stream. Can be used to stream an enumerable collection of
/// byte buffers.
/// </summary>
public class EnumerableStream : AbstractBaseStream
{
    private readonly IEnumerator<byte[]> _enumerator;
    private byte[] _currentChunk;
    private int _currentOffset;

    /// <summary>
    /// Creates a new instance of the <see cref="EnumerableStream"/> class.
    /// </summary>
    /// <param name="input">An enumerable collection of byte buffers.</param>
    public EnumerableStream(IEnumerable<byte[]> input)
    {
        Guard.ThrowIfNull(input, nameof(input));
        _enumerator = input.GetEnumerator();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                _enumerator?.Dispose();
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        Guard.ThrowIfNull(buffer, nameof(buffer));
        if (offset < 0 || offset >= buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (count < 0 || offset + count > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var total = 0;
        while (count > 0)
        {
            if (_currentChunk == null || _currentOffset >= _currentChunk.Length)
            {
                // need a new chunk
                if (!_enumerator.MoveNext())
                {
                    // no more chunks available
                    return total;
                }

                _currentChunk = _enumerator.Current;
                _currentOffset = 0;
            }

            if (_currentChunk == null)
            {
                continue;
            }

            var copyCount = Math.Min(count, _currentChunk.Length - _currentOffset);
            Buffer.BlockCopy(_currentChunk, _currentOffset, buffer, offset, copyCount);
            _currentOffset += copyCount;
            offset += copyCount;
            total += copyCount;
            count -= copyCount;
        }

        return total;
    }

    /// <inheritdoc/>
    public override bool CanRead => true;
}
