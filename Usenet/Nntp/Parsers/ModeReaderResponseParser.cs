using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers
{
    internal class ModeReaderResponseParser : IResponseParser<NntpModeReaderResponse>
    {
        private readonly ILogger _log = Logger.Create<ModeReaderResponseParser>();

        public bool IsSuccessResponse(int code) => GetResponseType(code) != NntpModeReaderResponseType.Unknown;

        public NntpModeReaderResponse Parse(int code, string message)
        {
            var responseType = GetResponseType(code);
            var success = responseType != NntpModeReaderResponseType.Unknown;
            if (!success)
            {
                _log.InvalidResponseCode(code);
            }

            return new NntpModeReaderResponse(code, message, success, responseType);
        }

        private static NntpModeReaderResponseType GetResponseType(int code)
        {
            return Enum.IsDefined(typeof(NntpModeReaderResponseType), code)
                ? (NntpModeReaderResponseType)code
                : NntpModeReaderResponseType.Unknown;
        }
    }
}
