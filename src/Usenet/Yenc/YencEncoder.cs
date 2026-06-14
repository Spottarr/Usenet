using System.Buffers;
using System.Globalization;
using System.IO.Hashing;
using System.Text;
using JetBrains.Annotations;
using Usenet.Util;

namespace Usenet.Yenc;

/// <summary>
/// Represents an yEnc encoder.
/// </summary>
[PublicAPI]
public static class YencEncoder
{
    // Number of source bytes read from the stream per block.
    private const int ReadBlockSize = 64 * 1024;

    // Precomputed table flagging which encoded values are critical characters that must
    // be escaped, and under which column condition. Indexed by the encoded value (0-255).
    private const byte EscapeAlways = 1;
    private const byte EscapeFirstColumn = 2;
    private const byte EscapeLastColumn = 4;
    private static readonly byte[] EscapeTable = CreateEscapeTable();

    /// <summary>
    /// Encodes the binary data in the specified stream as yEnc-encoded bytes,
    /// streaming the result into the specified <see cref="IBufferWriter{T}"/>
    /// using the default Usenet character encoding.
    /// </summary>
    /// <param name="header">The yEnc header.</param>
    /// <param name="stream">The stream containing the binary data to encode.</param>
    /// <param name="writer">The buffer writer that receives the yEnc-encoded bytes.</param>
    /// <returns>A task that completes once the data has been encoded.</returns>
    public static Task EncodeAsync(YencHeader header, Stream stream, IBufferWriter<byte> writer) =>
        EncodeAsync(header, stream, writer, UsenetEncoding.Default, CancellationToken.None);

    /// <summary>
    /// Encodes the binary data in the specified stream as yEnc-encoded bytes,
    /// streaming the result into the specified <see cref="IBufferWriter{T}"/>
    /// using the default Usenet character encoding.
    /// </summary>
    /// <param name="header">The yEnc header.</param>
    /// <param name="stream">The stream containing the binary data to encode.</param>
    /// <param name="writer">The buffer writer that receives the yEnc-encoded bytes.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <returns>A task that completes once the data has been encoded.</returns>
    public static Task EncodeAsync(
        YencHeader header,
        Stream stream,
        IBufferWriter<byte> writer,
        Encoding encoding
    ) => EncodeAsync(header, stream, writer, encoding, CancellationToken.None);

    /// <summary>
    /// Encodes the binary data in the specified stream as yEnc-encoded bytes,
    /// streaming the result into the specified <see cref="IBufferWriter{T}"/>
    /// using the default Usenet character encoding.
    /// </summary>
    /// <param name="header">The yEnc header.</param>
    /// <param name="stream">The stream containing the binary data to encode.</param>
    /// <param name="writer">The buffer writer that receives the yEnc-encoded bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes once the data has been encoded.</returns>
    public static Task EncodeAsync(
        YencHeader header,
        Stream stream,
        IBufferWriter<byte> writer,
        CancellationToken cancellationToken
    ) => EncodeAsync(header, stream, writer, UsenetEncoding.Default, cancellationToken);

    /// <summary>
    /// Encodes the binary data in the specified stream as yEnc-encoded bytes,
    /// streaming the result into the specified <see cref="IBufferWriter{T}"/>
    /// using the specified character encoding.
    /// </summary>
    /// <param name="header">The yEnc header.</param>
    /// <param name="stream">The stream containing the binary data to encode.</param>
    /// <param name="writer">The buffer writer that receives the yEnc-encoded bytes.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes once the data has been encoded.</returns>
    public static async Task EncodeAsync(
        YencHeader header,
        Stream stream,
        IBufferWriter<byte> writer,
        Encoding encoding,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(encoding);

        WriteLine(writer, GetHeaderLine(header), encoding);
        if (header.IsFilePart)
        {
            WriteLine(writer, GetPartHeaderLine(header), encoding);
        }

        var checksum = new Crc32();
        var column = 0;
        var lastColumn = header.LineLength - 1;
        var remaining = header.PartSize;

        var readBuffer = ArrayPool<byte>.Shared.Rent(ReadBlockSize);
        try
        {
            while (remaining > 0)
            {
                var toRead = (int)Math.Min(readBuffer.Length, remaining);
                var bytesRead = await stream
                    .ReadAsync(readBuffer.AsMemory(0, toRead), cancellationToken)
                    .ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    // end of stream
                    break;
                }

                remaining -= bytesRead;
                var block = readBuffer.AsSpan(0, bytesRead);

                // Hash the source block in one accelerated pass rather than per byte while encoding.
                checksum.Append(block);
                EncodeBlock(block, writer, header.LineLength, lastColumn, ref column);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }

        if (column > 0)
        {
            // terminate the last body line
            WriteCrlf(writer);
        }

        WriteLine(writer, GetFooterLine(header, checksum.GetCurrentHashAsUInt32()), encoding);
    }

    private static void EncodeBlock(
        ReadOnlySpan<byte> source,
        IBufferWriter<byte> writer,
        int lineLength,
        int lastColumn,
        ref int column
    )
    {
        // Worst case (line=1): every source byte escapes to two bytes and wraps, costing the
        // escape, the value and a CRLF. Requesting the upper bound up front lets the whole
        // block be written into a single contiguous span without re-checking capacity per byte.
        var destination = writer.GetSpan(source.Length * 4 + 4);
        var written = 0;

        // The yEnc line length counts encoded output bytes, so an escape pair advances the
        // column by two. The escaping of a dot or whitespace depends on that column.
        var col = column;

        foreach (var @byte in source)
        {
            var val = (byte)((@byte + YencCharacters.EncodeOffset) % 256);

            var flags = EscapeTable[val];
            if (
                (flags & EscapeAlways) != 0
                || ((flags & EscapeFirstColumn) != 0 && col == 0)
                || ((flags & EscapeLastColumn) != 0 && col == lastColumn)
            )
            {
                destination[written++] = YencCharacters.Equal;
                col++;
                val = (byte)((val + YencCharacters.EscapeOffset) % 256);
            }

            destination[written++] = val;
            if (++col < lineLength)
            {
                continue;
            }

            destination[written++] = YencCharacters.Cr;
            destination[written++] = YencCharacters.Lf;
            col = 0;
        }

        writer.Advance(written);
        column = col;
    }

    private static void WriteLine(IBufferWriter<byte> writer, string line, Encoding encoding)
    {
        var byteCount = encoding.GetByteCount(line);
        var span = writer.GetSpan(byteCount + 2);
        var written = encoding.GetBytes(line, span);
        span[written++] = YencCharacters.Cr;
        span[written++] = YencCharacters.Lf;
        writer.Advance(written);
    }

    private static void WriteCrlf(IBufferWriter<byte> writer)
    {
        var span = writer.GetSpan(2);
        span[0] = YencCharacters.Cr;
        span[1] = YencCharacters.Lf;
        writer.Advance(2);
    }

    private static byte[] CreateEscapeTable()
    {
        var table = new byte[256];
        table[YencCharacters.Null] = EscapeAlways;
        table[YencCharacters.Lf] = EscapeAlways;
        table[YencCharacters.Cr] = EscapeAlways;
        table[YencCharacters.Equal] = EscapeAlways;
        table[YencCharacters.Dot] = EscapeFirstColumn;
        table[YencCharacters.Space] = EscapeFirstColumn | EscapeLastColumn;
        table[YencCharacters.Tab] = EscapeFirstColumn | EscapeLastColumn;
        return table;
    }

    private static string GetHeaderLine(YencHeader header)
    {
        var builder = new StringBuilder(YencKeywords.YBegin);

        if (header.IsFilePart)
        {
            builder.Append(' ').Append(YencKeywords.Part).Append('=').Append(header.PartNumber);
            builder.Append(' ').Append(YencKeywords.Total).Append('=').Append(header.TotalParts);
        }

        builder.Append(' ').Append(YencKeywords.Line).Append('=').Append(header.LineLength);
        builder.Append(' ').Append(YencKeywords.Size).Append('=').Append(header.FileSize);
        builder.Append(' ').Append(YencKeywords.Name).Append('=').Append(header.FileName);

        return builder.ToString();
    }

    private static string GetPartHeaderLine(YencHeader header)
    {
        var begin = header.PartOffset + 1;
        var end = header.PartOffset + header.PartSize;
        return $"{YencKeywords.YPart} {YencKeywords.Begin}={begin} {YencKeywords.End}={end}";
    }

    private static string GetFooterLine(YencHeader header, uint checksum)
    {
        var builder = new StringBuilder(YencKeywords.YEnd);

        if (header.IsFilePart)
        {
            builder.Append(' ').Append(YencKeywords.Size).Append('=').Append(header.PartSize);
            builder.Append(' ').Append(YencKeywords.Part).Append('=').Append(header.PartNumber);
            builder
                .Append(' ')
                .Append(YencKeywords.PartCrc32)
                .Append('=')
                .AppendFormat(CultureInfo.InvariantCulture, "{0:x}", checksum);
        }
        else
        {
            builder.Append(' ').Append(YencKeywords.Size).Append('=').Append(header.FileSize);
            builder
                .Append(' ')
                .Append(YencKeywords.Crc32)
                .Append('=')
                .AppendFormat(CultureInfo.InvariantCulture, "{0:x}", checksum);
        }

        return builder.ToString();
    }
}
