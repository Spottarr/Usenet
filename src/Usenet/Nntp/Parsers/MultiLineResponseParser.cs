using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers;

internal class MultiLineResponseParser : IMultiLineResponseParser<NntpMultiLineResponse>
{
    private readonly int[] _successCodes;

    public MultiLineResponseParser(params int[] successCodes)
    {
        _successCodes = successCodes ?? [];
    }

    public bool IsSuccessResponse(int code) => _successCodes.Contains(code);

    public NntpMultiLineResponse Parse(int code, string message, IEnumerable<string> dataBlock) => new(code, message, IsSuccessResponse(code), dataBlock);
}