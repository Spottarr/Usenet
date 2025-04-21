using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers
{
    internal class LastResponseParser : IResponseParser<NntpLastResponse>
    {
        private readonly ILogger _log = Logger.Create<LastResponseParser>();

        public bool IsSuccessResponse(int code) => code == (int) NntpLastResponseType.ArticleExists;

        public NntpLastResponse Parse(int code, string message)
        {
            var responseType = Enum.IsDefined(typeof(NntpLastResponseType), code)
                ? (NntpLastResponseType)code
                : NntpLastResponseType.Unknown;

            if (responseType == NntpLastResponseType.Unknown)
            {
                _log.InvalidResponseCode(code);
            }

            if (!IsSuccessResponse(code))
            {
                return new NntpLastResponse(code, message, false, responseType, 0, string.Empty);
            }

            // get stat
            var responseSplit = message.Split(' ');
            if (responseSplit.Length < 2)
            {
                _log.InvalidResponseMessage(message);
            }

            _ = long.TryParse(responseSplit.Length > 0 ? responseSplit[0] : null, out var number);
            var messageId = responseSplit.Length > 1 ? responseSplit[1] : string.Empty;

            return new NntpLastResponse(code, message, true, responseType, number, messageId);
        }
    }
}
