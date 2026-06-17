using System.IO.Compression;
using Usenet.Exceptions;

namespace Usenet.Nntp;

/// <summary>
/// Inflates the compressed data block of an <c>XFEATURE COMPRESS GZIP</c> multi-line response into the
/// raw bytes the line framer expects. This is the transport's inflate stage: it sits ahead of line
/// framing and is scoped to the data block, so the clear-text status line never reaches it.
/// </summary>
/// <remarks>
/// Providers are inconsistent about how the "GZIP" payload is wrapped: some send a gzip member (RFC
/// 1952), some a zlib stream (RFC 1950) and some a bare DEFLATE stream (RFC 1951). The wrapper is
/// detected from the leading bytes so a single configured mode rides whichever a given server speaks.
/// A truncated or corrupt payload surfaces as an <see cref="NntpException"/> on the affected command
/// rather than as silently dropped rows. See
/// <a href="https://github.com/Spottarr/Usenet/blob/main/docs/adr/0005-compressed-overview-transport-and-connection-options.md">ADR-0005</a>.
/// </remarks>
internal static class NntpCompressedBlock
{
    /// <summary>
    /// Inflates the first <paramref name="length"/> bytes of <paramref name="compressed"/> into the
    /// decompressed data-block bytes.
    /// </summary>
    /// <param name="compressed">A buffer holding the compressed payload (may be oversized).</param>
    /// <param name="length">The number of valid bytes in <paramref name="compressed"/>.</param>
    /// <returns>The decompressed bytes.</returns>
    /// <exception cref="NntpException">The payload could not be inflated (truncated or corrupt).</exception>
    public static byte[] Inflate(byte[] compressed, int length)
    {
        ArgumentNullException.ThrowIfNull(compressed);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        using var input = new MemoryStream(compressed, 0, length, writable: false);
        using var output = new MemoryStream();
        try
        {
            using var decompressor = CreateDecompressor(input, compressed.AsSpan(0, length));
            decompressor.CopyTo(output);
        }
        catch (InvalidDataException ex)
        {
            throw new NntpException("Failed to inflate the compressed multi-line data block.", ex);
        }

        return output.ToArray();
    }

    private static Stream CreateDecompressor(Stream input, ReadOnlySpan<byte> payload)
    {
        // gzip member: magic 0x1f 0x8b (RFC 1952).
        if (payload.Length >= 2 && payload[0] == 0x1f && payload[1] == 0x8b)
        {
            return new GZipStream(input, CompressionMode.Decompress);
        }

        // zlib stream: the CMF byte's low nibble is 8 (DEFLATE) and (CMF*256 + FLG) is a multiple of
        // 31 (RFC 1950). This is what most XFEATURE COMPRESS GZIP providers actually emit.
        if (
            payload.Length >= 2
            && (payload[0] & 0x0f) == 0x08
            && ((payload[0] << 8) | payload[1]) % 31 == 0
        )
        {
            return new ZLibStream(input, CompressionMode.Decompress);
        }

        // Otherwise assume a bare DEFLATE stream (RFC 1951) with no wrapper.
        return new DeflateStream(input, CompressionMode.Decompress);
    }
}
