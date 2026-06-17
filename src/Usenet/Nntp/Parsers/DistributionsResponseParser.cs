using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usenet.Extensions;
using Usenet.Nntp.Models;

namespace Usenet.Nntp.Parsers;

internal class DistributionsResponseParser
    : IMultiLineResponseParser<IImmutableList<NntpDistribution>>
{
    private readonly ILogger _log;

    public DistributionsResponseParser(ILoggerFactory? loggerFactory = null) =>
        _log = (
            loggerFactory ?? NullLoggerFactory.Instance
        ).CreateLogger<DistributionsResponseParser>();

    public bool IsSuccessResponse(int code) => code == 215;

    public IImmutableList<NntpDistribution> Parse(
        int code,
        string message,
        IEnumerable<string> dataBlock
    )
    {
        if (!IsSuccessResponse(code))
        {
            return [];
        }

        return EnumerateDistributions(dataBlock).ToImmutableList();
    }

    private IEnumerable<NntpDistribution> EnumerateDistributions(IEnumerable<string> dataBlock)
    {
        foreach (var line in dataBlock)
        {
            var parts = line.Split(
                (char[]?)null,
                2,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            if (parts.Length == 0)
            {
                _log.InvalidDistributionLine(line);
                continue;
            }

            yield return new NntpDistribution
            {
                Value = parts[0],
                Description = parts.Length > 1 ? parts[1] : string.Empty,
            };
        }
    }
}
