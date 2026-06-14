using System.Buffers;
using System.IO.Hashing;
using System.Text;
using JetBrains.Annotations;
using Usenet.Exceptions;
using Usenet.Extensions;
using Usenet.Util;

namespace Usenet.Yenc;

/// <summary>
/// Decodes a yEnc-encoded body straight from bytes into a single pooled <c>Data</c> buffer,
/// avoiding the per-line <see cref="string"/> round-trip of the text-based decoders.
/// The decode is line-aware (keyword lines, escape handling, CRLF skipping), verifies the
/// per-part <c>pcrc32</c> (or <c>crc32</c> for a single-part file) and is a pure transform,
/// independent of the NNTP transport.
/// Based on Kristian Hellang's yEnc project https://github.com/khellang/yEnc.
/// </summary>
[PublicAPI]
public static class YencDecoder
{
    /// <summary>
    /// Decodes a yEnc-encoded body into a <see cref="YencPart"/> backed by pooled <c>Data</c>
    /// using the default Usenet character encoding.
    /// </summary>
    /// <param name="encoded">The yEnc-encoded body bytes.</param>
    /// <returns>A <see cref="YencPart"/> owning the decoded data and meta-data. Dispose it to return the pooled buffer.</returns>
    public static YencPart Decode(ReadOnlyMemory<byte> encoded) =>
        Decode(encoded, UsenetEncoding.Default);

    /// <summary>
    /// Decodes a yEnc-encoded body into a <see cref="YencPart"/> backed by pooled <c>Data</c>
    /// using the specified character encoding for the keyword lines.
    /// </summary>
    /// <param name="encoded">The yEnc-encoded body bytes.</param>
    /// <param name="encoding">The character encoding used to read the keyword lines.</param>
    /// <returns>A <see cref="YencPart"/> owning the decoded data and meta-data. Dispose it to return the pooled buffer.</returns>
    public static YencPart Decode(ReadOnlyMemory<byte> encoded, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        var reader = new LineReader(encoded);
        var headers = GetHeaders(ref reader, encoding);
        var part = headers.GetAndConvert(YencKeywords.Part, int.Parse);
        if (part > 0)
        {
            headers.Merge(GetPartHeaders(ref reader, encoding), false);
        }

        var header = YencMeta.ParseHeader(headers);
        var buffer = ArrayPool<byte>.Shared.Rent((int)header.PartSize);
        try
        {
            var decoded = 0;
            YencFooter? footer = null;

            while (reader.MoveNext())
            {
                var line = reader.Current.Span;
                if (line.StartsWith(YEndPrefix))
                {
                    footer = YencMeta.ParseFooter(YencMeta.ParseLine(encoding.GetString(line)));
                    break;
                }

                decoded += DecodeLine(line, buffer.AsSpan(decoded));
            }

            // Verify the part checksum in one accelerated pass over the whole decoded buffer
            // rather than folding a scalar CRC into the per-byte decode loop.
            VerifyChecksum(header, footer, Crc32.HashToUInt32(buffer.AsSpan(0, decoded)));
            return new YencPart(header, footer, buffer, decoded);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

    /// <summary>
    /// Decodes a yEnc-encoded body into a <see cref="YencPart"/> backed by pooled <c>Data</c>
    /// using the default Usenet character encoding.
    /// </summary>
    /// <param name="encoded">The yEnc-encoded body bytes.</param>
    /// <returns>A <see cref="YencPart"/> owning the decoded data and meta-data. Dispose it to return the pooled buffer.</returns>
    public static YencPart Decode(ReadOnlySequence<byte> encoded) =>
        Decode(encoded, UsenetEncoding.Default);

    /// <summary>
    /// Decodes a yEnc-encoded body into a <see cref="YencPart"/> backed by pooled <c>Data</c>
    /// using the specified character encoding for the keyword lines.
    /// </summary>
    /// <param name="encoded">The yEnc-encoded body bytes.</param>
    /// <param name="encoding">The character encoding used to read the keyword lines.</param>
    /// <returns>A <see cref="YencPart"/> owning the decoded data and meta-data. Dispose it to return the pooled buffer.</returns>
    public static YencPart Decode(ReadOnlySequence<byte> encoded, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        if (encoded.IsSingleSegment)
        {
            return Decode(encoded.First, encoding);
        }

        // The canonical body shape is a contiguous buffer; copy the (rare) multi-segment
        // sequence into one pooled buffer before decoding.
        var length = checked((int)encoded.Length);
        var contiguous = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            encoded.CopyTo(contiguous);
            return Decode(contiguous.AsMemory(0, length), encoding);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(contiguous);
        }
    }

    private static readonly byte[] YBeginPrefix = Encoding.ASCII.GetBytes(
        $"{YencKeywords.YBegin} "
    );
    private static readonly byte[] YPartPrefix = Encoding.ASCII.GetBytes($"{YencKeywords.YPart} ");
    private static readonly byte[] YEndPrefix = Encoding.ASCII.GetBytes($"{YencKeywords.YEnd} ");

    private static Dictionary<string, string> GetHeaders(ref LineReader reader, Encoding encoding)
    {
        while (reader.MoveNext())
        {
            var line = reader.Current.Span;
            if (line.StartsWith(YBeginPrefix))
            {
                return YencMeta.ParseLine(encoding.GetString(line));
            }
        }

        throw new InvalidYencDataException(Resources.Yenc.MissingHeader);
    }

    private static Dictionary<string, string> GetPartHeaders(
        ref LineReader reader,
        Encoding encoding
    )
    {
        if (reader.MoveNext() && reader.Current.Span.StartsWith(YPartPrefix))
        {
            return YencMeta.ParseLine(encoding.GetString(reader.Current.Span));
        }

        throw new InvalidYencDataException(Resources.Yenc.MissingPartHeader);
    }

    private static int DecodeLine(ReadOnlySpan<byte> line, Span<byte> output)
    {
        var written = 0;
        var isEscaped = false;
        foreach (var @byte in line)
        {
            if (@byte == YencCharacters.Equal && !isEscaped)
            {
                isEscaped = true;
                continue;
            }

            byte decoded;
            if (isEscaped)
            {
                isEscaped = false;
                decoded = (byte)(@byte - YencCharacters.EncodeOffset - YencCharacters.EscapeOffset);
            }
            else
            {
                decoded = (byte)(@byte - YencCharacters.EncodeOffset);
            }

            output[written++] = decoded;
        }

        return written;
    }

    private static void VerifyChecksum(YencHeader header, YencFooter? footer, uint calculated)
    {
        if (footer == null)
        {
            return;
        }

        // A multi-part body is verified against the per-part pcrc32; a single-part body
        // against crc32. When the relevant checksum is absent there is nothing to verify.
        if (header.IsFilePart)
        {
            if (footer.PartCrc32 is { } partCrc32 && partCrc32 != calculated)
            {
                throw new InvalidYencDataException(Resources.Yenc.PartChecksumMismatch);
            }
        }
        else if (footer.Crc32 is { } crc32 && crc32 != calculated)
        {
            throw new InvalidYencDataException(Resources.Yenc.ChecksumMismatch);
        }
    }

    /// <summary>
    /// Forward-only reader over the lines of a yEnc body. Empty lines are skipped and each
    /// returned line has its terminating CR/LF stripped, mirroring the text-based decoders.
    /// </summary>
    private struct LineReader(ReadOnlyMemory<byte> data)
    {
        private int _position;

        public ReadOnlyMemory<byte> Current { get; private set; }

        public bool MoveNext()
        {
            while (_position < data.Length)
            {
                var rest = data.Span[_position..];
                var newline = rest.IndexOf(YencCharacters.Lf);

                int length;
                int advance;
                if (newline < 0)
                {
                    length = rest.Length;
                    advance = rest.Length;
                }
                else
                {
                    length = newline;
                    advance = newline + 1;
                }

                if (length > 0 && rest[length - 1] == YencCharacters.Cr)
                {
                    length--;
                }

                Current = data.Slice(_position, length);
                _position += advance;

                if (length > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
