using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usenet.Extensions;
using Usenet.Nntp.Models;

namespace Usenet.Nntp.Parsers;

internal class ModeratorsResponseParser : IMultiLineResponseParser<IImmutableList<NntpModerator>>
{
    private readonly ILogger _log;

    public ModeratorsResponseParser(ILoggerFactory? loggerFactory = null) =>
        _log = (
            loggerFactory ?? NullLoggerFactory.Instance
        ).CreateLogger<ModeratorsResponseParser>();

    public bool IsSuccessResponse(int code) => code == 215;

    public IImmutableList<NntpModerator> Parse(
        int code,
        string message,
        IEnumerable<string> dataBlock
    )
    {
        if (!IsSuccessResponse(code))
        {
            return [];
        }

        return EnumerateModerators(dataBlock).ToImmutableList();
    }

    private IEnumerable<NntpModerator> EnumerateModerators(IEnumerable<string> dataBlock)
    {
        foreach (var line in dataBlock)
        {
            var separatorIndex = line.IndexOf(':', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                _log.InvalidModeratorLine(line);
                continue;
            }

            yield return new NntpModerator
            {
                GroupPattern = line[..separatorIndex].Trim(),
                SubmissionAddress = line[(separatorIndex + 1)..].Trim(),
            };
        }
    }
}
