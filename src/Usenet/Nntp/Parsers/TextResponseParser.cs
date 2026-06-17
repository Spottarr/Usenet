using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers;

internal class TextResponseParser : IMultiLineResponseParser<NntpTextResponse>
{
    private readonly int[] _successCodes;

    public TextResponseParser(params int[] successCodes) => _successCodes = successCodes;

    public bool IsSuccessResponse(int code) => _successCodes.Contains(code);

    public NntpTextResponse Parse(int code, string message, IEnumerable<string> dataBlock) =>
        new(code, message, IsSuccessResponse(code), dataBlock);
}
