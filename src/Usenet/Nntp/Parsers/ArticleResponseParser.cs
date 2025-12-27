using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Builders;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;
using Usenet.Util;

namespace Usenet.Nntp.Parsers;

[Flags]
internal enum ArticleRequestType
{
    Head = 0x01,
    Body = 0x02,
    Article = 0x03
}

internal class ArticleResponseParser : IMultiLineResponseParser<NntpArticleResponse>
{
    private readonly ILogger _log = Logger.Create<ArticleResponseParser>();
    private readonly ArticleRequestType _requestType;
    private readonly int _successCode;

    public ArticleResponseParser(ArticleRequestType requestType)
    {
        switch (_requestType = requestType)
        {
            case ArticleRequestType.Head:
                _successCode = 221;
                break;

            case ArticleRequestType.Body:
                _successCode = 222;
                break;

            case ArticleRequestType.Article:
                _successCode = 220;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null);
        }
    }

    public bool IsSuccessResponse(int code) => code == _successCode;

    public NntpArticleResponse Parse(int code, string message, IEnumerable<string> dataBlock)
    {
        if (!IsSuccessResponse(code))
        {
            return new NntpArticleResponse(code, message, false, null);
        }

        // get response line
        var responseSplit = message.Split(' ');
        if (responseSplit.Length < 2)
        {
            _log.InvalidResponseMessage(message);
        }

        _ = long.TryParse(responseSplit.Length > 0 ? responseSplit[0] : null, out var number);
        var messageId = responseSplit.Length > 1 ? responseSplit[1] : string.Empty;

        if (dataBlock == null)
        {
            // no headers and no body
            return new NntpArticleResponse(code, message, true, new NntpArticle(number, messageId, null, null, null));
        }

        using var enumerator = dataBlock.GetEnumerator();

        // get headers if requested
        var headers = (_requestType & ArticleRequestType.Head) == ArticleRequestType.Head
            ? GetHeaders(enumerator)
            : MultiValueDictionary<string, string>.EmptyIgnoreCase;

        // get groups
        var groups = headers.TryGetValue(NntpHeaders.Newsgroups, out var values)
            ? new NntpGroupsBuilder().Add(values).Build()
            : null;

        // get body if requested
        var bodyLines = (_requestType & ArticleRequestType.Body) == ArticleRequestType.Body
            ? GetBody(enumerator).ToList()
            : [];

        return new NntpArticleResponse(
            code, message, true,
            new NntpArticle(number, messageId, groups, headers, bodyLines));
    }

    private MultiValueDictionary<string, string> GetHeaders(IEnumerator<string> enumerator)
    {
        var headers = new List<Header>();
        Header prevHeader = null;
        while (enumerator.MoveNext())
        {
            var line = enumerator.Current;
            if (string.IsNullOrEmpty(line))
            {
                // no more headers (skip empty line)
                break;
            }

            if (char.IsWhiteSpace(line[0]) && prevHeader != null)
            {
                prevHeader.Value += " " + line.Trim();
            }
            else
            {
                var splitPos = line.IndexOf(':', StringComparison.Ordinal);
                if (splitPos < 0)
                {
                    _log.InvalidHeaderLine(line);
                }
                else
                {
                    prevHeader = new Header(line[..splitPos], line[(splitPos + 1)..].Trim());
                    headers.Add(prevHeader);
                }
            }
        }

        var dict = MultiValueDictionary<string, string>.EmptyIgnoreCase;
        foreach (var header in headers)
        {
            dict.Add(header.Key, header.Value);
        }

        return dict;
    }

    private static IEnumerable<string> GetBody(IEnumerator<string> enumerator)
    {
        while (enumerator.MoveNext())
            yield return enumerator.Current;
    }

    private class Header
    {
        public string Key { get; }
        public string Value { get; set; }

        public Header(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
