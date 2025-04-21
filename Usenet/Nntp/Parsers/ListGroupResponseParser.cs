using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers
{
    internal class ListGroupResponseParser : IMultiLineResponseParser<NntpGroupResponse>
    {
        private readonly ILogger log = Logger.Create<ListGroupResponseParser>();

        public bool IsSuccessResponse(int code) => code == 211;

        public NntpGroupResponse Parse(int code, string message, IEnumerable<string> dataBlock)
        {
            if (!IsSuccessResponse(code))
            {
                return new NntpGroupResponse(
                    code, message, false,
                    new NntpGroup(string.Empty, 0, 0, 0, NntpPostingStatus.Unknown,
                        string.Empty, []));
            }

            var responseSplit = message.Split(' ');
            if (responseSplit.Length < 4)
            {
                log.InvalidGroupResponseMessage(message);
            }

            _ = long.TryParse(responseSplit.Length > 0 ? responseSplit[0] : null, out var articleCount);
            _ = long.TryParse(responseSplit.Length > 1 ? responseSplit[1] : null, out var lowWaterMark);
            _ = long.TryParse(responseSplit.Length > 2 ? responseSplit[2] : null, out var highWaterMark);
            var name = responseSplit.Length > 3 ? responseSplit[3] : string.Empty;

            var articleNumbers = EnumerateArticleNumbers(dataBlock);
            if (dataBlock is ICollection<string>)
            {
                // no need to keep enumerator if input is not a stream
                // memoize the article numbers (https://en.wikipedia.org/wiki/Memoization)
                articleNumbers = articleNumbers.ToList();
            }

            return new NntpGroupResponse(code, message, true,
                new NntpGroup(
                    name, 
                    articleCount, 
                    lowWaterMark, 
                    highWaterMark, 
                    NntpPostingStatus.Unknown, 
                    string.Empty,
                    articleNumbers));
        }

        private static IEnumerable<long> EnumerateArticleNumbers(IEnumerable<string> dataBlock)
        {
            if (dataBlock == null)
            {
                yield break;
            }
            foreach (var line in dataBlock)
            {
                if (!long.TryParse(line, out var number))
                {
                    continue;
                }
                yield return number;
            }
        }
    }
}
