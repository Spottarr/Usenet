using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers
{
    internal class GroupResponseParser : IResponseParser<NntpGroupResponse>
    {
        private readonly ILogger _log = Logger.Create<GroupResponseParser>();

        public bool IsSuccessResponse(int code) => code == 211;

        public NntpGroupResponse Parse(int code, string message)
        {
            if (!IsSuccessResponse(code))
            {
                return new NntpGroupResponse(
                    code, message, false,
                    new NntpGroup(string.Empty, 0, 0, 0, NntpPostingStatus.Unknown, string.Empty, []));
            }

            var responseSplit = message.Split(' ');
            if (responseSplit.Length < 4)
            {
                _log.InvalidGroupResponseMessage(message);
            }

            _ = long.TryParse(responseSplit.Length > 0 ? responseSplit[0] : null, out var articleCount);
            _ = long.TryParse(responseSplit.Length > 1 ? responseSplit[1] : null, out var lowWaterMark);
            _ = long.TryParse(responseSplit.Length > 2 ? responseSplit[2] : null, out var highWaterMark);
            var name = responseSplit.Length > 3 ? responseSplit[3] : string.Empty;

            return new NntpGroupResponse(
                code, message, true,
                new NntpGroup(
                    name, 
                    articleCount, 
                    lowWaterMark, 
                    highWaterMark, 
                    NntpPostingStatus.Unknown,
                    string.Empty, 
                    []));
        }
    }
}
