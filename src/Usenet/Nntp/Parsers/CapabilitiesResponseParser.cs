using System.Collections.Immutable;
using Usenet.Nntp.Models;

namespace Usenet.Nntp.Parsers;

internal class CapabilitiesResponseParser : IMultiLineResponseParser<NntpCapabilities>
{
    public bool IsSuccessResponse(int code) => code == 101;

    public NntpCapabilities Parse(int code, string message, IEnumerable<string> dataBlock)
    {
        if (!IsSuccessResponse(code))
        {
            return NntpCapabilities.Empty;
        }

        var capabilities = new Dictionary<string, ImmutableArray<string>>(
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var line in dataBlock)
        {
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                continue;
            }

            var keyword = tokens[0];
            var arguments = tokens.Length > 1 ? ImmutableArray.Create(tokens.AsSpan(1)) : [];
            capabilities[keyword] = arguments;
        }

        return new NntpCapabilities(capabilities);
    }
}
