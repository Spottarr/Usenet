﻿using System.Text;
using Usenet.Extensions;
using Usenet.Util;

namespace Usenet.Yenc;

/// <summary>
/// Represents a yEnc-encoded article decoder.
/// The article is completely decoded in memory.
/// Based on Kristian Hellang's yEnc project https://github.com/khellang/yEnc.
/// </summary>
public static class YencArticleDecoder
{
    private const string YEnd = YencKeywords.YEnd + " ";

    /// <summary>
    /// Decodes yEnc-encoded text into a <see cref="YencArticle"/>
    /// using the default Usenet character encoding.
    /// </summary>
    /// <param name="encodedLines">The yEnc-encoded lines to decode.</param>
    /// <returns>A <see cref="YencArticle"/> containing the decoded binary data and meta-data.</returns>
    public static YencArticle Decode(IEnumerable<string> encodedLines) =>
        Decode(encodedLines, UsenetEncoding.Default);

    /// <summary>
    /// Decodes yEnc-encoded text into a <see cref="YencArticle"/>
    /// using the specified charcter encoding.
    /// </summary>
    /// <param name="encodedLines">The yEnc-encoded lines to decode.</param>
    /// <param name="encoding">The charcter encoding to use.</param>
    /// <returns>A <see cref="YencArticle"/> containing the decoded binary data and meta-data.</returns>
    public static YencArticle Decode(IEnumerable<string> encodedLines, Encoding encoding)
    {
        Guard.ThrowIfNull(encodedLines, nameof(encodedLines));
        Guard.ThrowIfNull(encoding, nameof(encoding));

        using var enumerator = encodedLines.GetEnumerator();
        var headers = YencMeta.GetHeaders(enumerator);
        var part = headers.GetAndConvert(YencKeywords.Part, int.Parse);
        if (part > 0)
        {
            headers.Merge(YencMeta.GetPartHeaders(enumerator), false);
        }

        var header = YencMeta.ParseHeader(headers);
        YencFooter footer = null;

        // create buffer for part or entire file if single part
        var decodedBytes = new byte[header.PartSize];
        var decodedBytesIndex = 0;

        while (enumerator.MoveNext())
        {
            if (enumerator.Current == null)
            {
                continue;
            }

            if (enumerator.Current.StartsWith(YEnd, StringComparison.Ordinal))
            {
                footer = YencMeta.ParseFooter(YencMeta.ParseLine(enumerator.Current));

                // skip remainder if there is some
                while (enumerator.MoveNext())
                {
                }

                break;
            }

            var encodedBytes = encoding.GetBytes(enumerator.Current);
            decodedBytesIndex += YencLineDecoder.Decode(encodedBytes, decodedBytes, decodedBytesIndex);
        }

        return new YencArticle(header, footer, decodedBytes);
    }
}
