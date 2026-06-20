using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Usenet.Util;

namespace Usenet.Nntp.Client;

/// <summary>
/// Frames CRLF-terminated lines and multi-line data blocks off a <see cref="PipeReader"/>, undoing
/// dot-stuffing and detecting the terminating dot on the raw bytes without transcoding the whole
/// stream to <see cref="string"/>. This is pure byte→line logic over a reader: it has no knowledge of
/// sockets, SSL or compression, so it can be unit-tested over an in-memory pipe. The owning
/// <see cref="NntpConnection"/> swaps <see cref="Reader"/> whenever it rebuilds the transport (for
/// example when installing a compression layer).
/// </summary>
internal sealed class NntpLineFramer
{
    private const int StackAllocThreshold = 256;
    private const int InitialDataBlockBufferSize = 4096;

    private long _bytesRead;

    /// <summary>
    /// The reader lines are framed off. Rebuilt by the connection when the transport changes; never
    /// read before the connection has established it.
    /// </summary>
    public PipeReader? Reader { get; set; }

    /// <summary>
    /// The number of bytes framed off the reader. When compression is active the reader sits above the
    /// decompressing layer, so this counts decompressed (logical) bytes (ADR-0005).
    /// </summary>
    public long BytesRead => _bytesRead;

    /// <summary>
    /// Set when a read off the reader throws, so the connection can fault and discard itself.
    /// </summary>
    public bool HasFault { get; private set; }

    public void ResetCounter() => _bytesRead = 0;

    /// <summary>
    /// Reads a single CRLF-terminated line off the byte stream, counting the consumed bytes.
    /// </summary>
    public async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        var reader = Reader!;
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                if (TryReadLine(ref buffer, out var line, out var consumed))
                {
                    AddBytesRead(consumed);
                    reader.AdvanceTo(buffer.Start);
                    return line;
                }

                if (result.IsCompleted)
                {
                    if (buffer.IsEmpty)
                    {
                        reader.AdvanceTo(buffer.Start, buffer.End);
                        return null;
                    }

                    // The stream ended on a final line without a trailing CRLF.
                    AddBytesRead(buffer.Length);
                    var remaining = DecodeLine(buffer);
                    reader.AdvanceTo(buffer.End);
                    return remaining;
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
            }
        }
        catch
        {
            HasFault = true;
            throw;
        }
    }

    /// <summary>
    /// Yields the undot-stuffed lines of a multi-line data block until the terminating dot.
    /// </summary>
    public async IAsyncEnumerable<string> ReadDataBlockLinesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        while (true)
        {
            var line = await ReadLineAsync(cancellationToken).ConfigureAwait(false);
            var processed = ProcessLine(line);
            if (processed == null)
            {
                yield break;
            }

            yield return processed;
        }
    }

    /// <summary>
    /// Reads a complete multi-line data block off the byte stream into a single contiguous buffer
    /// rented from <see cref="ArrayPool{T}"/>. Dot-stuffing is undone and the terminating dot is
    /// detected on the raw bytes, without transcoding the body to <see cref="string"/>. Each line is
    /// stored with a normalized CRLF terminator. The caller takes ownership of the returned buffer.
    /// </summary>
    public async Task<(byte[] Buffer, int Length)> ReadDataBlockToBufferAsync(
        CancellationToken cancellationToken
    )
    {
        var reader = Reader!;
        var buffer = ArrayPool<byte>.Shared.Rent(InitialDataBlockBufferSize);
        var length = 0;
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var segment = result.Buffer;

                var consumed = AppendLines(segment, ref buffer, ref length, out var completed);
                AddBytesRead(consumed);
                var consumedPosition = segment.GetPosition(consumed);

                if (completed || result.IsCompleted)
                {
                    reader.AdvanceTo(consumedPosition);
                    return (buffer, length);
                }

                reader.AdvanceTo(consumedPosition, segment.End);
            }
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);
            HasFault = true;
            throw;
        }
    }

    /// <summary>
    /// Undoes dot-stuffing and detects the terminating dot of a multi-line data block, following the
    /// <a href="https://tools.ietf.org/html/rfc3977#section-3.1.1">RFC 3977</a> rules. The terminating
    /// line (".") and the end of input both map to <see langword="null"/>. This is the
    /// <see cref="string"/> counterpart of the byte-oriented <see cref="AppendLine(ReadOnlySpan{byte}, ref byte[], ref int)"/>;
    /// the two must stay in sync.
    /// </summary>
    public static string? ProcessLine(string? line)
    {
        if (line == null)
        {
            return null;
        }

        if (line.Length == 0 || line[0] != '.')
        {
            return line;
        }

        if (line.Length == 1)
        {
            return null;
        }

        return line[1] == '.' ? line[1..] : line;
    }

    private void AddBytesRead(long count)
    {
        unchecked
        {
            _bytesRead += count;
            if (_bytesRead < 0)
            {
                _bytesRead = 0;
            }
        }
    }

    private static bool TryReadLine(
        ref ReadOnlySequence<byte> buffer,
        out string line,
        out long consumed
    )
    {
        var reader = new SequenceReader<byte>(buffer);
        if (
            reader.TryReadTo(
                out ReadOnlySequence<byte> lineSequence,
                (byte)'\n',
                advancePastDelimiter: true
            )
        )
        {
            consumed = reader.Consumed;
            line = DecodeLine(lineSequence);
            buffer = buffer.Slice(reader.Position);
            return true;
        }

        line = string.Empty;
        consumed = 0;
        return false;
    }

    private static string DecodeLine(in ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsSingleSegment)
        {
            return DecodeSpan(sequence.FirstSpan);
        }

        var length = (int)sequence.Length;
        byte[]? rented = null;
        var buffer =
            length <= StackAllocThreshold
                ? stackalloc byte[StackAllocThreshold]
                : (rented = ArrayPool<byte>.Shared.Rent(length));
        try
        {
            sequence.CopyTo(buffer);
            return DecodeSpan(buffer[..length]);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static string DecodeSpan(ReadOnlySpan<byte> span)
    {
        // Strip the trailing CR of the CRLF terminator.
        if (span.Length > 0 && span[^1] == (byte)'\r')
        {
            span = span[..^1];
        }

        return UsenetEncoding.Default.GetString(span);
    }

    private static long AppendLines(
        in ReadOnlySequence<byte> segment,
        ref byte[] buffer,
        ref int length,
        out bool completed
    )
    {
        completed = false;
        var reader = new SequenceReader<byte>(segment);
        while (
            reader.TryReadTo(
                out ReadOnlySequence<byte> lineSequence,
                (byte)'\n',
                advancePastDelimiter: true
            )
        )
        {
            if (AppendLine(lineSequence, ref buffer, ref length))
            {
                completed = true;
                break;
            }
        }

        return reader.Consumed;
    }

    /// <summary>
    /// Appends a single framed line to the data block buffer, returning <see langword="true"/> when the
    /// line is the terminating dot of the data block.
    /// </summary>
    private static bool AppendLine(
        in ReadOnlySequence<byte> lineSequence,
        ref byte[] buffer,
        ref int length
    )
    {
        if (lineSequence.IsSingleSegment)
        {
            return AppendLine(lineSequence.FirstSpan, ref buffer, ref length);
        }

        var lineLength = (int)lineSequence.Length;
        byte[]? rented = null;
        var line =
            lineLength <= StackAllocThreshold
                ? stackalloc byte[StackAllocThreshold]
                : (rented = ArrayPool<byte>.Shared.Rent(lineLength));
        try
        {
            lineSequence.CopyTo(line);
            return AppendLine(line[..lineLength], ref buffer, ref length);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    /// <summary>
    /// Appends a single framed line to the data block buffer, applying the same RFC 3977 dot-unstuffing
    /// and terminating-dot detection as the <see cref="string"/>-oriented <see cref="ProcessLine"/>;
    /// the two must stay in sync.
    /// </summary>
    private static bool AppendLine(ReadOnlySpan<byte> line, ref byte[] buffer, ref int length)
    {
        // Strip the trailing CR of the CRLF terminator.
        if (line.Length > 0 && line[^1] == (byte)'\r')
        {
            line = line[..^1];
        }

        // A line containing only "." terminates the data block.
        if (line.Length == 1 && line[0] == (byte)'.')
        {
            return true;
        }

        // Undo dot-stuffing of a line that begins with ".".
        if (line.Length > 0 && line[0] == (byte)'.')
        {
            line = line[1..];
        }

        EnsureCapacity(ref buffer, length, length + line.Length + 2);
        line.CopyTo(buffer.AsSpan(length));
        length += line.Length;
        buffer[length++] = (byte)'\r';
        buffer[length++] = (byte)'\n';
        return false;
    }

    private static void EnsureCapacity(ref byte[] buffer, int length, int required)
    {
        if (buffer.Length >= required)
        {
            return;
        }

        var next = ArrayPool<byte>.Shared.Rent(Math.Max(buffer.Length * 2, required));
        buffer.AsSpan(0, length).CopyTo(next);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = next;
    }
}
