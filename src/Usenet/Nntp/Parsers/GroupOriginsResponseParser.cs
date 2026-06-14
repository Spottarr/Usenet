using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usenet.Extensions;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers;

internal class GroupOriginsResponseParser : IMultiLineResponseParser<NntpGroupOriginsResponse>
{
    private readonly ILogger _log;

    public GroupOriginsResponseParser(ILoggerFactory? loggerFactory = null) =>
        _log = (
            loggerFactory ?? NullLoggerFactory.Instance
        ).CreateLogger<GroupOriginsResponseParser>();

    public bool IsSuccessResponse(int code) => code == 215;

    public NntpGroupOriginsResponse Parse(int code, string message, IEnumerable<string> dataBlock)
    {
        if (!IsSuccessResponse(code))
        {
            return new NntpGroupOriginsResponse(code, message, false, []);
        }

        var groupOrigins = EnumerateGroupOrigins(dataBlock).ToImmutableList();

        return new NntpGroupOriginsResponse(code, message, true, groupOrigins);
    }

    private IEnumerable<NntpGroupOrigin> EnumerateGroupOrigins(IEnumerable<string> dataBlock)
    {
        foreach (var line in dataBlock)
        {
            var lineSplit = line.Split(' ');
            if (lineSplit.Length < 3)
            {
                _log.InvalidGroupOriginLine(line);
                continue;
            }

            _ = long.TryParse(lineSplit[1], out var createdAtTimestamp);

            yield return new NntpGroupOrigin(
                lineSplit[0],
                DateTimeOffset.FromUnixTimeSeconds(createdAtTimestamp),
                lineSplit[2]
            );
        }
    }
}
