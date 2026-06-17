using Usenet.Nntp.Models;

namespace Usenet.Nntp.Parsers;

internal class SubscriptionsResponseParser : IMultiLineResponseParser<NntpGroups>
{
    public bool IsSuccessResponse(int code) => code == 215;

    public NntpGroups Parse(int code, string message, IEnumerable<string> dataBlock) =>
        IsSuccessResponse(code) ? new NntpGroups(dataBlock) : NntpGroups.Empty;
}
