using System.Globalization;
using System.Text;
using Usenet.Extensions;
using Usenet.Util;

namespace Usenet.Yenc;

/// <summary>
/// Represents an yEnc encoder.
/// </summary>
public static class YencEncoder
{
    /// <summary>
    /// Encodes the binary data in the specified stream into yEnc-encoded text
    /// using the default Usenet character encoding.
    /// </summary>
    /// <param name="header">The yEnc header.</param>
    /// <param name="stream">The stream containing the binary data to encode.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the yEnc-encoded text lines.</returns>
    public static Task<IReadOnlyList<string>> EncodeAsync(
        YencHeader header,
        Stream stream,
        CancellationToken cancellationToken = default
    ) => EncodeAsync(header, stream, UsenetEncoding.Default, cancellationToken);

    /// <summary>
    /// Encodes the binary data in the specified stream into yEnc-encoded text
    /// using the specified character encoding.
    /// </summary>
    /// <param name="header">The yEnc header.</param>
    /// <param name="stream">The stream containing the binary data to encode.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the yEnc-encoded text lines.</returns>
    public static async Task<IReadOnlyList<string>> EncodeAsync(
        YencHeader header,
        Stream stream,
        Encoding encoding,
        CancellationToken cancellationToken = default
    )
    {
        Guard.ThrowIfNull(header, nameof(header));
        Guard.ThrowIfNull(stream, nameof(stream));
        Guard.ThrowIfNull(encoding, nameof(encoding));

        var lines = new List<string>();

        lines.Add(GetHeaderLine(header));
        if (header.IsFilePart)
        {
            lines.Add(GetPartHeaderLine(header));
        }

        var encodedBytes = new byte[1024];
        var encodedOffset = 0;
        var lastCol = header.LineLength - 1;
        var checksum = Crc32.Initialize();
        var readBuffer = new byte[1];

        for (var offset = 0; offset < header.PartSize; offset++)
        {
            var bytesRead = await stream
                .ReadByteAsync(readBuffer, cancellationToken)
                .ConfigureAwait(false);
            if (bytesRead == 0)
            {
                // end of stream
                break;
            }

            var @byte = readBuffer[0];
            checksum = Crc32.Calculate(checksum, @byte);
            var val = (@byte + 42) % 256;

            // encode dot only in first column
            var encodeDot = encodedOffset == 0;

            // encode white space only in first and last column
            var encodeWhitespace = encodedOffset == 0 || encodedOffset == lastCol;

            // encode critical characters
            if (
                val == YencCharacters.Null
                || val == YencCharacters.Lf
                || val == YencCharacters.Cr
                || val == YencCharacters.Equal
                || val == YencCharacters.Dot && encodeDot
                || val == YencCharacters.Space && encodeWhitespace
                || val == YencCharacters.Tab && encodeWhitespace
            )
            {
                encodedBytes[encodedOffset++] = YencCharacters.Equal;
                val = (val + 64) % 256;
            }

            encodedBytes[encodedOffset++] = (byte)val;
            if (encodedOffset < header.LineLength)
            {
                continue;
            }

            // return encoded line
            lines.Add(encoding.GetString(encodedBytes, 0, encodedOffset));

            // reset offset
            encodedOffset = 0;
        }

        if (encodedOffset > 0)
        {
            // return remainder
            lines.Add(encoding.GetString(encodedBytes, 0, encodedOffset));
        }

        checksum = Crc32.Finalize(checksum);
        lines.Add(GetFooterLine(header, checksum));

        return lines;
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
