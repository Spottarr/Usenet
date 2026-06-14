using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

internal sealed class ArticleResponseParser : IBufferedMultiLineResponseParser<NntpArticleResponse>
{
    private readonly ILogger _log;
    private readonly ArticleRequestType _requestType;
    private readonly int _successCode;

    public ArticleResponseParser(
        ArticleRequestType requestType,
        ILoggerFactory? loggerFactory = null
    )
    {
        _log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ArticleResponseParser>();
        _successCode = (_requestType = requestType) switch
        {
            ArticleRequestType.Head => 221,
            ArticleRequestType.Body => 222,
            ArticleRequestType.Article => 220,
            _ => throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null),
        };
    }

    public bool IsSuccessResponse(int code) => code == _successCode;

    public NntpArticleResponse ParseError(int code, string message) => new(code, message, false);

    public NntpArticleResponse Parse(int code, string message, byte[] buffer, int length)
    {
        var (number, messageId) = ParseResponseLine(message);

        var bodyOffset = length;
        var headers = ImmutableDictionary<string, ImmutableList<string>>.Empty.WithComparers(
            StringComparer.OrdinalIgnoreCase
        );
        var groups = NntpGroups.Empty;

        // The headers and the empty line separating them from the body are decoded as text; the body
        // bytes are left untouched in the pooled buffer (see ADR-0002).
        if ((_requestType & ArticleRequestType.Head) == ArticleRequestType.Head)
        {
            var parsed = ParseHeaders(buffer.AsSpan(0, length), out bodyOffset);
            headers = parsed.ToImmutableDictionary(
                x => x.Key,
                x => x.Value.ToImmutableList(),
                keyComparer: StringComparer.OrdinalIgnoreCase
            );

            if (parsed.TryGetValue(NntpHeaders.Newsgroups, out var values))
            {
                groups = new NntpGroupsBuilder().Add(values).Build();
            }
        }
        else
        {
            // A BODY response is body bytes only.
            bodyOffset = 0;
        }

        return new NntpArticleResponse(
            code,
            message,
            number,
            messageId,
            groups,
            headers,
            buffer,
            bodyOffset,
            length
        );
    }

    private (long Number, NntpMessageId MessageId) ParseResponseLine(string message)
    {
        var responseSplit = message.Split(' ');
        if (responseSplit.Length < 2)
        {
            _log.InvalidResponseMessage(message);
        }

        _ = long.TryParse(responseSplit.Length > 0 ? responseSplit[0] : null, out var number);
        NntpMessageId messageId = responseSplit.Length > 1 ? responseSplit[1] : NntpMessageId.Empty;
        return (number, messageId);
    }

    private MultiValueDictionary<string, string> ParseHeaders(
        ReadOnlySpan<byte> span,
        out int bodyOffset
    )
    {
        var headers = new List<Header>();
        Header? prevHeader = null;
        var position = 0;
        bodyOffset = span.Length;

        while (position < span.Length)
        {
            var newline = span[position..].IndexOf((byte)'\n');
            var lineEnd = newline < 0 ? span.Length : position + newline;
            var next = newline < 0 ? span.Length : lineEnd + 1;

            var contentEnd = lineEnd;
            if (contentEnd > position && span[contentEnd - 1] == (byte)'\r')
            {
                contentEnd--;
            }

            if (contentEnd == position)
            {
                // The empty line separates the headers from the body.
                bodyOffset = next;
                break;
            }

            var line = UsenetEncoding.Default.GetString(span[position..contentEnd]);
            if (char.IsWhiteSpace(line[0]) && prevHeader != null)
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

            position = next;
        }

        return headers.Count == 0 ? NntpHeaderCollection.Empty : new NntpHeaderCollection(headers);
    }

    private sealed class Header
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
