using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers;

internal class NextResponseParser : IResponseParser<NntpNextResponse>
{
    private readonly ILogger _log = Logger.Create<NextResponseParser>();

    public bool IsSuccessResponse(int code) => code == (int)NntpNextResponseType.ArticleExists;

    public NntpNextResponse Parse(int code, string message)
    {
        var responseType = Enum.IsDefined(typeof(NntpNextResponseType), code)
            ? (NntpNextResponseType)code
            : NntpNextResponseType.Unknown;

        if (responseType == NntpNextResponseType.Unknown)
        {
            _log.InvalidResponseCode(code);
        }

        if (!IsSuccessResponse(code))
        {
            return new NntpNextResponse(code, message, false, responseType, 0, string.Empty);
        }

        // get stat
        var responseSplit = message.Split(' ');
        if (responseSplit.Length < 2)
        {
            _log.InvalidResponseMessage(message);
        }

        _ = long.TryParse(responseSplit.Length > 0 ? responseSplit[0] : null, out var number);
        var messageId = responseSplit.Length > 1 ? responseSplit[1] : string.Empty;

        return new NntpNextResponse(code, message, true, responseType, number, messageId);
    }
}
