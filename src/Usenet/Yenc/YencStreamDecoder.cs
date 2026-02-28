using System.Text;
using Usenet.Extensions;
using Usenet.Util;

namespace Usenet.Yenc;

/// <summary>
/// Represents a yEnc-encoded article decoder.
/// The article is decoded streaming.
/// Based on Kristian Hellang's yEnc project https://github.com/khellang/yEnc.
/// </summary>
public static class YencStreamDecoder
{
    private const string YEnd = YencKeywords.YEnd + " ";
    private const int BufferSize = 4096;

    /// <summary>
    /// Decodes yEnc-encoded text into a <see cref="YencStream"/>
    /// using the default Usenet character encoding.
    /// </summary>
    /// <param name="encodedLines">The yEnc-encoded lines to decode.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="YencStream"/> with decoded binary data and meta-data.</returns>
    public static YencStream Decode(
        IEnumerable<string> encodedLines,
        CancellationToken cancellationToken = default
    ) => Decode(encodedLines, UsenetEncoding.Default, cancellationToken);

    /// <summary>
    /// Decodes yEnc-encoded text into a <see cref="YencStream"/>
    /// using the specified character encoding.
    /// </summary>
    /// <param name="encodedLines">The yEnc-encoded lines to decode.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="YencStream"/> with decoded binary data and meta-data.</returns>
    public static YencStream Decode(
        IEnumerable<string> encodedLines,
        Encoding encoding,
        CancellationToken cancellationToken = default
    )
    {
        Guard.ThrowIfNull(encodedLines, nameof(encodedLines));
        Guard.ThrowIfNull(encoding, nameof(encoding));

        cancellationToken.ThrowIfCancellationRequested();

        using var enumerator = encodedLines.GetEnumerator();
        var headers = YencMeta.GetHeaders(enumerator);
        var part = headers.GetAndConvert(YencKeywords.Part, int.Parse);
        if (part > 0)
        {
            headers.Merge(YencMeta.GetPartHeaders(enumerator), false);
        }

        return new YencStream(YencMeta.ParseHeader(headers), EnumerateData(enumerator, encoding));
    }

    private static IEnumerable<byte[]> EnumerateData(
        IEnumerator<string> enumerator,
        Encoding encoding
    )
    {
        var buffer = new byte[BufferSize];
        while (enumerator.MoveNext())
        {
            if (enumerator.Current == null)
            {
                continue;
            }

            if (enumerator.Current.StartsWith(YEnd, StringComparison.Ordinal))
            {
                // skip rest if there is some
                while (enumerator.MoveNext()) { }

                yield break;
            }

            var encodedBytes = encoding.GetBytes(enumerator.Current);
            var decodedCount = YencLineDecoder.Decode(encodedBytes, buffer, 0);
            var decodedBytes = new byte[decodedCount];
            Buffer.BlockCopy(buffer, 0, decodedBytes, 0, decodedCount);
            yield return decodedBytes;
        }
    }
}
