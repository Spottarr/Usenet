using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers;

internal enum GroupStatusRequestType
{
    Basic,
    Extended
}

internal class GroupsResponseParser : IMultiLineResponseParser<NntpGroupsResponse>
{
    private readonly int _successCode;
    private readonly GroupStatusRequestType _requestType;
    private readonly ILogger _log = Logger.Create<GroupsResponseParser>();

    public GroupsResponseParser(int successCode, GroupStatusRequestType requestType)
    {
        _successCode = successCode;
        _requestType = requestType;
    }

    public bool IsSuccessResponse(int code) => code == _successCode;

    public NntpGroupsResponse Parse(int code, string message, IEnumerable<string> dataBlock)
    {
        if (!IsSuccessResponse(code) || dataBlock == null)
        {
            return new NntpGroupsResponse(code, message, false, []);
        }

        var groups = EnumerateGroups(dataBlock);
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

        var checkParameterCount = _requestType == GroupStatusRequestType.Basic ? 4 : 5;

        foreach (var line in dataBlock)
        {
            var lineSplit = line.Split(' ');
            if (lineSplit.Length < checkParameterCount)
            {
                if (_requestType == GroupStatusRequestType.Basic)
                {
                    _log.InvalidGroupBasicInformationLine(line);
                }
                else
                {
                    _log.InvalidGroupExtendedInformationLine(line);
                }

                continue;
            }

            var argCount = 1;
            _ = long.TryParse(lineSplit[argCount++], out var highWaterMark);
            _ = long.TryParse(lineSplit[argCount++], out var lowWaterMark);

            var articleCount = 0L;
            if (_requestType == GroupStatusRequestType.Extended)
            {
                _ = long.TryParse(lineSplit[argCount++], out articleCount);
            }

            var postingStatus = PostingStatusParser.Parse(lineSplit[argCount], out var otherGroup);
            if (postingStatus == NntpPostingStatus.Unknown)
            {
                _log.InvalidPostingStatus(lineSplit[argCount], line);
            }

            yield return new NntpGroup(
                lineSplit[0],
                articleCount,
                lowWaterMark,
                highWaterMark,
                postingStatus,
                otherGroup,
                []);
        }
    }
}
