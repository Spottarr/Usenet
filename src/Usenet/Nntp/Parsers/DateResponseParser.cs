using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Usenet.Extensions;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers;

internal class DateResponseParser : IResponseParser<NntpDateResponse>
{
    private readonly ILogger _log;

    public DateResponseParser(ILoggerFactory? loggerFactory = null) =>
        _log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<DateResponseParser>();

    public bool IsSuccessResponse(int code) => code == 111;

    public NntpDateResponse Parse(int code, string message)
    {
        var responseSplit = message.Split(' ');

        if (
            IsSuccessResponse(code)
            && responseSplit.Length >= 1
            && DateTimeOffset.TryParseExact(
                responseSplit[0],
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var dateTime
            )
        )
        {
            return new NntpDateResponse(code, message, true, dateTime);
        }

        _log.InvalidDateResponseMessage(message);
        return new NntpDateResponse(code, message, false, DateTimeOffset.MinValue);
    }
}
