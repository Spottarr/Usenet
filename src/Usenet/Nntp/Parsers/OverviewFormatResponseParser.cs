using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usenet.Extensions;
using Usenet.Nntp.Models;

namespace Usenet.Nntp.Parsers;

internal class OverviewFormatResponseParser : IMultiLineResponseParser<NntpOverviewFormat>
{
    private const string FullSuffix = ":full";

    private readonly ILogger _log;

    public OverviewFormatResponseParser(ILoggerFactory? loggerFactory = null) =>
        _log = (
            loggerFactory ?? NullLoggerFactory.Instance
        ).CreateLogger<OverviewFormatResponseParser>();

    public bool IsSuccessResponse(int code) => code == 215;

    public NntpOverviewFormat Parse(int code, string message, IEnumerable<string> dataBlock)
    {
        if (!IsSuccessResponse(code))
        {
            return NntpOverviewFormat.Empty;
        }

        return new NntpOverviewFormat(EnumerateFields(dataBlock));
    }

    private IEnumerable<NntpOverviewField> EnumerateFields(IEnumerable<string> dataBlock)
    {
        foreach (var line in dataBlock)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var includesHeaderName = trimmed.EndsWith(
                FullSuffix,
                StringComparison.OrdinalIgnoreCase
            );
            var core = includesHeaderName ? trimmed[..^FullSuffix.Length] : trimmed;
            var isMetadata = core.StartsWith(':');
            var name = isMetadata ? core.TrimStart(':') : core.TrimEnd(':');

            if (name.Length == 0)
            {
                _log.InvalidOverviewFormatLine(line);
                continue;
            }

            yield return new NntpOverviewField
            {
                Name = name,
                IncludesHeaderName = includesHeaderName,
                IsMetadata = isMetadata,
            };
        }
    }
}
