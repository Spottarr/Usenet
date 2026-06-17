using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usenet.Extensions;
using Usenet.Nntp.Models;

namespace Usenet.Nntp.Parsers;

internal class DistribPatsResponseParser
    : IMultiLineResponseParser<IImmutableList<NntpDistributionPattern>>
{
    private readonly ILogger _log;

    public DistribPatsResponseParser(ILoggerFactory? loggerFactory = null) =>
        _log = (
            loggerFactory ?? NullLoggerFactory.Instance
        ).CreateLogger<DistribPatsResponseParser>();

    public bool IsSuccessResponse(int code) => code == 215;

    public IImmutableList<NntpDistributionPattern> Parse(
        int code,
        string message,
        IEnumerable<string> dataBlock
    )
    {
        if (!IsSuccessResponse(code))
        {
            return [];
        }

        return EnumeratePatterns(dataBlock).ToImmutableList();
    }

    private IEnumerable<NntpDistributionPattern> EnumeratePatterns(IEnumerable<string> dataBlock)
    {
        foreach (var line in dataBlock)
        {
            var parts = line.Split(':', 3);
            if (parts.Length < 3 || !int.TryParse(parts[0], out var weight))
            {
                _log.InvalidDistribPatLine(line);
                continue;
            }

            yield return new NntpDistributionPattern
            {
                Weight = weight,
                Wildmat = parts[1],
                Value = parts[2],
            };
        }
    }
}
