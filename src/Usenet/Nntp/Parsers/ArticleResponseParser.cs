using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Builders;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers;

[Flags]
internal enum ArticleRequestType
{
    Head = 0x01,
    Body = 0x02,
    Article = 0x03,
}

internal class ArticleResponseParser : IMultiLineResponseParser<NntpArticleResponse>
{
    private readonly ILogger _log = Logger.Create<ArticleResponseParser>();
    private readonly ArticleRequestType _requestType;
    private readonly int _successCode;

    public ArticleResponseParser(ArticleRequestType requestType)
    {
        _successCode = (_requestType = requestType) switch
        {
            ArticleRequestType.Head => 221,
            ArticleRequestType.Body => 222,
            ArticleRequestType.Article => 220,
            _ => throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null),
        };
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
        NntpMessageId messageId = responseSplit.Length > 1 ? responseSplit[1] : NntpMessageId.Empty;

        using var enumerator = dataBlock.GetEnumerator();

        // get headers if requested
        var headers =
            (_requestType & ArticleRequestType.Head) == ArticleRequestType.Head
                ? GetHeaders(enumerator)
                : NntpHeaderCollection.Empty;

        // get groups
        var groups = headers.Contains(NntpHeaders.Newsgroups)
            ? new NntpGroupsBuilder().Add(headers.GetValues(NntpHeaders.Newsgroups)).Build()
            : NntpGroups.Empty;

        // get body if requested
        var bodyLines =
            (_requestType & ArticleRequestType.Body) == ArticleRequestType.Body
                ? GetBody(enumerator).ToList()
                : [];

        return new NntpArticleResponse(
            code,
            message,
            true,
            new NntpArticle(number, messageId, groups, headers, bodyLines)
        );
    }

    private NntpHeaderCollection GetHeaders(IEnumerator<string> enumerator)
    {
        // Parse each header line once into a flat list of key/value pairs, preserving order.
        // Folded continuation lines are appended onto the previous pair's value in place.
        var headers = new List<KeyValuePair<string, string>>();
        while (enumerator.MoveNext())
        {
            var line = enumerator.Current;
            if (string.IsNullOrEmpty(line))
            {
                // no more headers (skip empty line)
                break;
            }

            if (char.IsWhiteSpace(line[0]) && headers.Count > 0)
            {
                var previous = headers[^1];
                headers[^1] = new KeyValuePair<string, string>(
                    previous.Key,
                    previous.Value + " " + line.Trim()
                );
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
                    headers.Add(new(line[..splitPos], line[(splitPos + 1)..].Trim()));
                }
            }
        }

        return headers.Count == 0 ? NntpHeaderCollection.Empty : new NntpHeaderCollection(headers);
    }

    private static IEnumerable<string> GetBody(IEnumerator<string> enumerator)
    {
        while (enumerator.MoveNext())
        {
            if (enumerator.Current is not null)
                yield return enumerator.Current;
        }
    }
}
