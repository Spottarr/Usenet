using System.Globalization;
using Microsoft.Extensions.Logging;
using Usenet.Exceptions;
using Usenet.Extensions;
using Usenet.Nntp.Models;
using Usenet.Util;

namespace Usenet.Nntp.Builders;

/// <summary>
/// Represents a mutable <see cref="NntpArticle"/>.
/// </summary>
public class NntpArticleBuilder
{
    private readonly ILogger _log = Logger.Create<NntpArticleBuilder>();

    private const string DateFormat = "dd MMM yyyy HH:mm:ss";

    private static readonly string[] ReservedHeaderKeys =
    [
        NntpHeaders.Date, NntpHeaders.From, NntpHeaders.Subject, NntpHeaders.MessageId, NntpHeaders.Newsgroups
    ];

    private MultiValueDictionary<string, string> _headers = [];
    private NntpGroupsBuilder _groupsBuilder = new();
    private NntpMessageId _messageId = NntpMessageId.Empty;
    private string _from = string.Empty;
    private string _subject = string.Empty;
    private DateTimeOffset? _dateTime;
    private List<string> _body = [];

    /// <summary>
    /// Creates a new instance of the <see cref="NntpArticleBuilder"/> class.
    /// </summary>
    public NntpArticleBuilder()
    {
    }

    /// <summary>
    /// Initialize the <see cref="NntpArticleBuilder"/> from the given <see cref="NntpArticle"/>.
    /// All properties are overwritten.
    /// </summary>
    /// <param name="article">The <see cref="NntpArticle"/> to initialize the <see cref="NntpArticleBuilder"/> with.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder InitializeFrom(NntpArticle article)
    {
        Guard.ThrowIfNull(article);

        _messageId = new(article.MessageId.Value);
        _groupsBuilder = new NntpGroupsBuilder().Add(article.Groups);
        _headers = [];
        _from = string.Empty;
        _subject = string.Empty;
        _dateTime = null;
        _body = [];

        foreach (var header in article.Headers)
        {
            foreach (var value in header.Value)
            {
                switch (header.Key)
                {
                    case NntpHeaders.MessageId:
                        if (!_messageId.HasValue)
                        {
                            _messageId = value;
                        }
                        else
                        {
                            _log.HeaderOccursMoreThanOnce(NntpHeaders.MessageId);
                        }

                        break;

                    case NntpHeaders.From:
                        if (string.IsNullOrEmpty(_from))
                        {
                            _from = value;
                        }
                        else
                        {
                            _log.HeaderOccursMoreThanOnce(NntpHeaders.From);
                        }

                        break;

                    case NntpHeaders.Subject:
                        if (string.IsNullOrEmpty(_subject))
                        {
                            _subject = value;
                        }
                        else
                        {
                            _log.HeaderOccursMoreThanOnce(NntpHeaders.Subject);
                        }

                        break;

                    case NntpHeaders.Date:
                        if (_dateTime == null)
                        {
                            if (DateTimeOffset.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture,
                                    DateTimeStyles.None, out var headerDateTime))
                            {
                                _dateTime = headerDateTime;
                            }
                            else
                            {
                                _log.InvalidHeader(NntpHeaders.Date, value);
                            }
                        }
                        else
                        {
                            _log.HeaderOccursMoreThanOnce(NntpHeaders.Date);
                        }

                        break;

                    case NntpHeaders.Newsgroups:
                        // convert group header to list of groups, do not add as header
                        _groupsBuilder.Add(value);
                        break;

                    default:
                        _headers.Add(header.Key, value);
                        break;
                }
            }
        }

        // make copy of body
        _body = article.Body.ToList();

        return this;
    }

    /// <summary>
    /// Sets the article's required <see cref="NntpHeaders.MessageId"/> header.
    /// </summary>
    /// <param name="value">The <see cref="NntpHeaders.MessageId"/> header value.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder SetMessageId(NntpMessageId value)
    {
        _messageId = value.ThrowIfNullOrWhiteSpace(nameof(value));
        return this;
    }

    /// <summary>
    /// Sets the article's required <see cref="NntpHeaders.From"/> header.
    /// </summary>
    /// <param name="value">The <see cref="NntpHeaders.From"/> header value.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder SetFrom(string value)
    {
        _from = value.ThrowIfNullOrWhiteSpace(nameof(value));
        return this;
    }

    /// <summary>
    /// Sets the article's required <see cref="NntpHeaders.Subject"/> header.
    /// </summary>
    /// <param name="value">The <see cref="NntpHeaders.Subject"/> header value.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder SetSubject(string value)
    {
        _subject = value.ThrowIfNullOrWhiteSpace(nameof(value));
        return this;
    }

    /// <summary>
    /// Sets the required <see cref="NntpHeaders.Date"/> header.
    /// </summary>
    /// <param name="value">The <see cref="NntpHeaders.Date"/> header value.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder SetDate(DateTimeOffset value)
    {
        _dateTime = value;
        return this;
    }

    /// <summary>
    /// Sets the article's body to the provided enumerable collection of string lines.
    /// </summary>
    /// <param name="lines">An enumerable collection of string lines.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder SetBody(IEnumerable<string> lines)
    {
        _body = lines.ThrowIfNull(nameof(lines)).ToList();
        return this;
    }

    /// <summary>
    /// Add newsgroups to the required <see cref="NntpHeaders.Newsgroups"/> header.
    /// </summary>
    /// <param name="values">The groups to add to the <see cref="NntpHeaders.Newsgroups"/> header.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder AddGroups(params NntpGroups[] values)
    {
        Guard.ThrowIfNull(values);
        foreach (var value in values)
        {
            _groupsBuilder.Add(value);
        }

        return this;
    }

    /// <summary>
    /// Removes newsgroups from the <see cref="NntpHeaders.Newsgroups"/> header.
    /// </summary>
    /// <param name="values">The groups to remove from the <see cref="NntpHeaders.Newsgroups"/> header.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder RemoveGroups(params NntpGroups[] values)
    {
        Guard.ThrowIfNull(values);
        foreach (var value in values)
        {
            _groupsBuilder.Remove(value);
        }

        return this;
    }

    /// <summary>
    /// Adds a header to the article.
    /// </summary>
    /// <param name="key">The key of the header to add.</param>
    /// <param name="value">The value of the header to add.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder AddHeader(string key, string value)
    {
        Guard.ThrowIfNullOrWhiteSpace(key, nameof(key));
        Guard.ThrowIfNull(value);
        if (ReservedHeaderKeys.Contains(key))
        {
            throw new NntpException(Resources.Nntp.ReservedHeaderKeyNotAllowed);
        }

        _headers.Add(key, value);
        return this;
    }

    /// <summary>
    /// Removes a header from the article with the given key and value. If no
    /// value is provided all headers with the given key are removed.
    /// </summary>
    /// <param name="key">The key of the header(s) to remove.</param>
    /// <param name="value">The value of the header to remove.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder RemoveHeader(string key, string value)
    {
        Guard.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (ReservedHeaderKeys.Contains(key))
        {
            throw new NntpException(Resources.Nntp.ReservedHeaderKeyNotAllowed);
        }

        _headers.Remove(key, value);
        return this;
    }

    /// <summary>
    /// Adds a line to the body of the article.
    /// </summary>
    /// <param name="line">The text line to add to the body.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder AddLine(string line)
    {
        Guard.ThrowIfNull(line);
        _body.Add(line);
        return this;
    }

    /// <summary>
    /// Adds multiple lines to the body of the article.
    /// </summary>
    /// <param name="lines">The text lines to add to the body.</param>
    /// <returns>The <see cref="NntpArticleBuilder"/> so that additional calls can be chained.</returns>
    public NntpArticleBuilder AddLines(IEnumerable<string> lines)
    {
        Guard.ThrowIfNull(lines);

        _body.AddRange(lines);

        return this;
    }

    /// <summary>
    /// Creates a <see cref="NntpArticle"/> with al the properties from the <see cref="NntpArticleBuilder"/>.
    /// </summary>
    /// <returns>The <see cref="NntpArticle"/>.</returns>
    public NntpArticle Build()
    {
        if (!_messageId.HasValue)
        {
            throw new NntpException(Resources.Nntp.MessageIdHeaderNotSet);
        }

        if (string.IsNullOrWhiteSpace(_from))
        {
            throw new NntpException(Resources.Nntp.FromHeaderNotSet);
        }

        if (string.IsNullOrWhiteSpace(_subject))
        {
            throw new NntpException(Resources.Nntp.SubjectHeaderNotSet);
        }

        if (_groupsBuilder.IsEmpty)
        {
            throw new NntpException(Resources.Nntp.NewsgroupsHeaderNotSet);
        }

        var groups = _groupsBuilder.Build();

        _headers.Add(NntpHeaders.From, _from);
        _headers.Add(NntpHeaders.Subject, _subject);

        if (_dateTime.HasValue)
        {
            var formattedDate = _dateTime.Value.ToUniversalTime().ToString(DateFormat, CultureInfo.InvariantCulture);
            _headers.Add(NntpHeaders.Date, $"{formattedDate} +0000");
        }

        return new NntpArticle(0, _messageId, groups, _headers, _body);
    }
}
