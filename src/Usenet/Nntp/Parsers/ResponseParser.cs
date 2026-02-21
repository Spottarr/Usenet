using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers;

internal class ResponseParser : IResponseParser<NntpResponse>
{
    private readonly int[] _successCodes;

    public ResponseParser(params int[] successCodes)
    {
        _successCodes = successCodes ?? [];
    }

    public bool IsSuccessResponse(int code) => _successCodes.Contains(code);

    public NntpResponse Parse(int code, string message) => new(code, message, IsSuccessResponse(code));
}
