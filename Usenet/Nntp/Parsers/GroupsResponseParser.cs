using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers
{
    internal enum GroupStatusRequestType
    {
        Basic,
        Extended
    }

    internal class GroupsResponseParser : IMultiLineResponseParser<NntpGroupsResponse>
    {
        private readonly int successCode;
        private readonly GroupStatusRequestType requestType;
        private readonly ILogger log = Logger.Create<GroupsResponseParser>();

        public GroupsResponseParser(int successCode, GroupStatusRequestType requestType)
        {
            this.successCode = successCode;
            this.requestType = requestType;
        }

        public bool IsSuccessResponse(int code) => code == successCode;

        public NntpGroupsResponse Parse(int code, string message, IEnumerable<string> dataBlock)
        {
            if (!IsSuccessResponse(code) || dataBlock == null)
            {
                return new NntpGroupsResponse(code, message, false, new NntpGroup[0]);
            }

            IEnumerable<NntpGroup> groups = EnumerateGroups(dataBlock);
            if (dataBlock is ICollection<string>)
            {
                // no need to keep enumerator if input is not a stream
                // memoize the items (https://en.wikipedia.org/wiki/Memoization)
                groups = groups.ToList();
            }

            return new NntpGroupsResponse(code, message, true, groups);
        }

        private IEnumerable<NntpGroup> EnumerateGroups(IEnumerable<string> dataBlock)
        {
            if (dataBlock == null)
            {
                yield break;
            }

            int checkParameterCount = requestType == GroupStatusRequestType.Basic ? 4 : 5;

            foreach (string line in dataBlock)
            {
                string[] lineSplit = line.Split(' ');
                if (lineSplit.Length < checkParameterCount)
                {
                    if (requestType == GroupStatusRequestType.Basic)
                    {
                        log.InvalidGroupBasicInformationLine(line);
                    }
                    else
                    {
                        log.InvalidGroupExtendedInformationLine(line);
                    }
                    
                    continue;
                }

                var argCount = 1;
                long.TryParse(lineSplit[argCount++], out long highWaterMark);
                long.TryParse(lineSplit[argCount++], out long lowWaterMark);

                var articleCount = 0L;
                if (requestType == GroupStatusRequestType.Extended)
                {
                    long.TryParse(lineSplit[argCount++], out articleCount);
                }

                NntpPostingStatus postingStatus = PostingStatusParser.Parse(lineSplit[argCount], out string otherGroup);
                if (postingStatus == NntpPostingStatus.Unknown)
                {
                    log.InvalidPostingStatus(lineSplit[argCount], line);
                }

                yield return new NntpGroup(
                    lineSplit[0],
                    articleCount,
                    lowWaterMark,
                    highWaterMark,
                    postingStatus,
                    otherGroup,
                    new long[0]);
            }
        }
    }
}
