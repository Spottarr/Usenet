using System.Text.RegularExpressions;
using System.Xml.Linq;
using Usenet.Exceptions;
using Usenet.Extensions;
using Usenet.Nntp.Models;
using Usenet.Util;
using Usenet.Util.Compatibility;

namespace Usenet.Nzb;

/// <summary>
/// Represents a <a href="https://sabnzbd.org/wiki/extra/nzb-spec">NZB</a> document parser.
/// It takes an xml string as input and parses it into an instance of the <see cref="NzbDocument"/> class.
/// Based on Kristian Hellang's Nzb project https://github.com/khellang/Nzb.
/// </summary>
public static class NzbParser
{
    private static readonly Regex _fileNameRegex = new("\"([^\"]*)\"", RegexOptions.Compiled);

    /// <summary>
    /// Asynchronously parses the xml input from a string into an instance of the <see cref="NzbDocument"/> class.
    /// </summary>
    /// <param name="text">An xml string representing the NZB document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing a parsed <see cref="NzbDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
    /// <exception cref="InvalidNzbDataException">InvalidNzbDataException</exception>
    public static async Task<NzbDocument> ParseAsync(string text, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(text, nameof(text));

        var doc = await XDocumentShims.LoadAsync(new StringReader(text), cancellationToken).ConfigureAwait(false);
        return ParseDocument(doc);
    }

    /// <summary>
    /// Asynchronously parses the xml input from a stream into an instance of the <see cref="NzbDocument"/> class.
    /// </summary>
    /// <param name="stream">A stream containing the xml NZB document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing a parsed <see cref="NzbDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
    /// <exception cref="InvalidNzbDataException">InvalidNzbDataException</exception>
    public static async Task<NzbDocument> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(stream, nameof(stream));

        var doc = await XDocumentShims.LoadAsync(stream, cancellationToken).ConfigureAwait(false);
        return ParseDocument(doc);
    }

    /// <summary>
    /// Asynchronously parses the xml input from a text reader into an instance of the <see cref="NzbDocument"/> class.
    /// </summary>
    /// <param name="reader">A text reader containing the xml NZB document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing a parsed <see cref="NzbDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">ArgumentNullException</exception>
    /// <exception cref="InvalidNzbDataException">InvalidNzbDataException</exception>
    public static async Task<NzbDocument> ParseAsync(TextReader reader, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(reader, nameof(reader));

        var doc = await XDocumentShims.LoadAsync(reader, cancellationToken).ConfigureAwait(false);
        return ParseDocument(doc);
    }

    private static NzbDocument ParseDocument(XDocument doc)
    {
        XNamespace ns = NzbKeywords.Namespace;
        var nzbElement = doc.Element(ns + NzbKeywords.Nzb);

        if (nzbElement == null)
        {
            ns = XNamespace.None;
        }

        nzbElement = doc.Element(ns + NzbKeywords.Nzb);

        if (nzbElement == null)
        {
            throw new InvalidNzbDataException(Resources.Nzb.MissingNzbElement);
        }

        var context = new NzbParserContext { Namespace = ns };

        var metaData = GetMetaData(context, nzbElement);
        var files = GetFiles(context, nzbElement);

        return new NzbDocument(metaData, files);
    }

    private static MultiValueDictionary<string, string>? GetMetaData(NzbParserContext context, XContainer nzbElement)
    {
        var headElement = nzbElement.Element(context.Namespace + NzbKeywords.Head);
        if (headElement == null)
        {
            return null;
        }

        var headers =
            from metaElement in headElement.Elements(context.Namespace + NzbKeywords.Meta)
            let typeAttribute = metaElement.Attribute(NzbKeywords.Type)
            where typeAttribute != null
            select new Tuple<string, string>(typeAttribute.Value, metaElement.Value);

        var dict = new MultiValueDictionary<string, string>();
        foreach (var header in headers)
        {
            dict.Add(header.Item1, header.Item2);
        }

        return dict;
    }

    private static IEnumerable<NzbFile> GetFiles(NzbParserContext context, XContainer nzbElement) => nzbElement
        .Elements(context.Namespace + NzbKeywords.File)
        .Select(f => GetFile(context, f));

    private static NzbFile GetFile(NzbParserContext context, XElement fileElement)
    {
        var poster = (string?)fileElement.Attribute(NzbKeywords.Poster) ?? string.Empty;
        if (!long.TryParse((string?)fileElement.Attribute(NzbKeywords.Date) ?? "0", out var unixTimestamp))
        {
            throw new InvalidNzbDataException(Resources.Nzb.InvalidDateAttriubute);
        }

        var date = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        var subject = (string?)fileElement.Attribute(NzbKeywords.Subject) ?? string.Empty;
        var fileName = GetFileName(subject);
        var groups = GetGroups(context, fileElement.Element(context.Namespace + NzbKeywords.Groups));
        IEnumerable<NzbSegment> segments = GetSegments(context, fileElement.Element(context.Namespace + NzbKeywords.Segments));

        return new NzbFile(poster, subject, fileName, date, groups, segments);
    }

    private static string GetFileName(string subject)
    {
        var match = _fileNameRegex.Match(subject);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        var len = subject.LastIndexOf(" (", StringComparison.OrdinalIgnoreCase);
        return RemoveTrailingYenc(len < 0 ? subject : subject[..len]);
    }

    private static string RemoveTrailingYenc(string subject)
    {
        subject = subject.Trim();
        var yencPos = subject.LastIndexOf(" yenc", StringComparison.OrdinalIgnoreCase);
        return yencPos < 0 ? subject : subject[..yencPos].Trim();
    }

    private static NntpGroups GetGroups(NzbParserContext context, XContainer? groupsElement)
    {
        var groups = groupsElement?
            .Elements(context.Namespace + NzbKeywords.Group)
            .Select(g => g.Value);
        return new NntpGroups(groups);
    }

    private static List<NzbSegment> GetSegments(NzbParserContext context, XContainer? segmentsElement)
    {
        var elements = segmentsElement?
            .Elements(context.Namespace + NzbKeywords.Segment)
            .OrderBy(element => ((string?)element.Attribute(NzbKeywords.Number)).ToIntSafe());

        var segments = new List<NzbSegment>();
        if (elements == null) return segments;
        
        long offset = 0;
        foreach (var element in elements)
        {
            var segment = GetSegment(element, offset);
            segments.Add(segment);
            offset += segment.Size;
        }

        return segments;
    }

    private static NzbSegment GetSegment(XElement element, long offset)
    {
        if (!int.TryParse((string?)element.Attribute(NzbKeywords.Number), out var number))
        {
            throw new InvalidNzbDataException(Resources.Nzb.InvalidOrMissingNumberAttribute);
        }

        if (!long.TryParse((string?)element.Attribute(NzbKeywords.Bytes), out var size))
        {
            throw new InvalidNzbDataException(Resources.Nzb.InvalidOrMissingBytesAttribute);
        }

        return new NzbSegment(number, offset, size, element.Value);
    }
}
