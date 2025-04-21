using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers
{
    internal class StatResponseParser : IResponseParser<NntpStatResponse>
    {
        private readonly ILogger _log = Logger.Create<StatResponseParser>();

        public bool IsSuccessResponse(int code) => code == (int) NntpStatResponseType.ArticleExists;

        public NntpStatResponse Parse(int code, string message)
        {
            var responseType = Enum.IsDefined(typeof(NntpStatResponseType), code)
                ? (NntpStatResponseType) code
                : NntpStatResponseType.Unknown;

            if (responseType == NntpStatResponseType.Unknown)
            {
                _log.InvalidResponseCode(code);
            }

            if (!IsSuccessResponse(code))
            {
                return new NntpStatResponse(code, message, false, responseType, 0, string.Empty);
            }

            // get stat
            var responseSplit = message.Split(' ');
            if (responseSplit.Length < 2)
            {
                _log.InvalidResponseMessage(message);
            }

            _ = long.TryParse(responseSplit.Length > 0 ? responseSplit[0] : null, out var number);
            var messageId = responseSplit.Length > 1 ? responseSplit[1] : string.Empty;

            return new NntpStatResponse(code, message, true, responseType, number, messageId);
        }
    }
}
