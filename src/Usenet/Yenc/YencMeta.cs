using Usenet.Exceptions;
using Usenet.Extensions;
using Usenet.Util.Compatibility;

namespace Usenet.Yenc;

/// <summary>
/// Utiltiy class to retrieve yEnc metadata.
/// Based on Kristian Hellang's yEnc project https://github.com/khellang/yEnc.
/// </summary>
internal class YencMeta
{
    private const string YBegin = $"{YencKeywords.YBegin} ";
    private const string YPart = $"{YencKeywords.YPart} ";
    private const string NameSeparator = $"{YencKeywords.Name}=";

    public static IDictionary<string, string> GetHeaders(IEnumerator<string> enumerator)
    {
        if (enumerator == null)
        {
            throw new InvalidYencDataException(Resources.Yenc.MissingHeader);
        }

        while (enumerator.MoveNext())
        {
            if (enumerator.Current == null)
            {
                continue;
            }

            if (enumerator.Current.StartsWith(YBegin, StringComparison.Ordinal))
            {
                return ParseLine(enumerator.Current);
            }
        }

        throw new InvalidYencDataException(Resources.Yenc.MissingHeader);
    }

    public static IDictionary<string, string> GetPartHeaders(IEnumerator<string> enumerator)
    {
        if (enumerator == null)
        {
            throw new InvalidYencDataException(Resources.Yenc.MissingPartHeader);
        }

        if (
            enumerator.MoveNext()
            && enumerator.Current != null
            && enumerator.Current.StartsWith(YPart, StringComparison.Ordinal)
        )
        {
            return ParseLine(enumerator.Current);
        }

        throw new InvalidYencDataException(Resources.Yenc.MissingPartHeader);
    }

    public static YencHeader ParseHeader(IDictionary<string, string> headers)
    {
        var name = headers.GetOrDefault(YencKeywords.Name);
        var size = headers.GetAndConvert(YencKeywords.Size, long.Parse);
        var line = headers.GetAndConvert(YencKeywords.Line, int.Parse);
        var part = headers.GetAndConvert(YencKeywords.Part, int.Parse);
        var total = headers.GetAndConvert(YencKeywords.Total, int.Parse);
        var begin = headers.GetAndConvert(YencKeywords.Begin, long.Parse);
        var end = headers.GetAndConvert(YencKeywords.End, long.Parse);

        return new YencHeader(
            name,
            size > 0 ? size : 0,
            line > 0 ? line : 0,
            part > 0 ? part : 0,
            part > 0 ? total : 1,
            part > 0 ? end - begin + 1 : size,
            part > 0 ? begin - 1 : 0
        );
    }

    public static YencFooter ParseFooter(IDictionary<string, string> footer)
    {
        var size = footer.GetAndConvert(YencKeywords.Size, long.Parse);
        var part = footer.GetAndConvert(YencKeywords.Part, int.Parse);
        var crc32 = footer.GetAndConvert<uint?>(
            YencKeywords.Crc32,
            crc => Convert.ToUInt32(crc, 16)
        );
        var partCrc32 = footer.GetAndConvert<uint?>(
            YencKeywords.PartCrc32,
            crc => Convert.ToUInt32(crc, 16)
        );

        return new YencFooter(size > 0 ? size : 0, part > 0 ? part : 0, crc32, partCrc32);
    }

    public static Dictionary<string, string> ParseLine(string line)
    {
        if (line == null)
        {
            return new Dictionary<string, string>(0);
        }

        // name is always last item on the header line
        var nameSplit = line.Split(NameSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (nameSplit.Length == 0)
        {
            return new Dictionary<string, string>(0);
        }

        Dictionary<string, string> dictionary = new();
        if (nameSplit.Length > 1)
        {
            // found name
            dictionary.Add(YencKeywords.Name, nameSplit[1].Trim());
        }

        // parse other items
        var pairs = nameSplit[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (pairs.Length == 0)
        {
            return dictionary;
        }

        foreach (var pair in pairs)
        {
            var parts = pair.Split('=');
            if (parts.Length < 2)
            {
                continue;
            }

            dictionary.Add(parts[0], parts[1]);
        }

        return dictionary;
    }
}
