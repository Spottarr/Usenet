using System.Globalization;
using System.Xml;
using Usenet.Util;

namespace Usenet.Nzb;

/// <summary>
/// Represents an NZB document writer.
/// </summary>
public class NzbWriter
{
    private readonly TextWriter _textWriter;

    /// <summary>
    /// Creates a new instance of the <see cref="NzbWriter"/> class that will use
    /// the specified <see cref="TextWriter"/> for writing.
    /// </summary>
    /// <param name="textWriter">The <see cref="TextWriter"/> to use for writing.</param>
    public NzbWriter(TextWriter textWriter)
    {
        _textWriter = textWriter;
    }

    /// <summary>
    /// Writes the specified <see cref="NzbDocument"/> asynchronously to the stream.
    /// </summary>
    /// <param name="nzbDocument">The NZB document to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> that can be awaited.</returns>
    public async Task WriteAsync(NzbDocument nzbDocument, CancellationToken cancellationToken = default)
    {
        Guard.ThrowIfNull(nzbDocument, nameof(nzbDocument));

        cancellationToken.ThrowIfCancellationRequested();

        using var writer = GetXmlWriter();
        await writer.WriteDocTypeAsync(
                NzbKeywords.Nzb,
                NzbKeywords.PubId,
                NzbKeywords.SysId,
                null)
            .ConfigureAwait(false);

        await writer.WriteStartElementAsync(
                null,
                NzbKeywords.Nzb,
                NzbKeywords.Namespace)
            .ConfigureAwait(false);

        await WriteHeadAsync(writer, nzbDocument).ConfigureAwait(false);
        await WriteFilesAsync(writer, nzbDocument).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await writer.WriteEndDocumentAsync().ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    private XmlWriter GetXmlWriter() =>
        XmlWriter.Create(_textWriter, new XmlWriterSettings { Encoding = _textWriter.Encoding, Async = true, Indent = true });

    private static async Task WriteHeadAsync(XmlWriter writer, NzbDocument nzbDocument)
    {
        await writer.WriteStartElementAsync(null, NzbKeywords.Head, null).ConfigureAwait(false);
        foreach (var header in nzbDocument.MetaData)
        {
            foreach (var value in header.Value)
            {
                await writer.WriteStartElementAsync(null, NzbKeywords.Meta, null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, NzbKeywords.Type, null, header.Key).ConfigureAwait(false);
                await writer.WriteStringAsync(value).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private static async Task WriteFilesAsync(XmlWriter writer, NzbDocument nzbDocument)
    {
        foreach (var file in nzbDocument.Files)
        {
            await writer.WriteStartElementAsync(null, NzbKeywords.File, null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, NzbKeywords.Poster, null, file.Poster).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, NzbKeywords.Date, null, file.Date.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
                .ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, NzbKeywords.Subject, null, file.Subject).ConfigureAwait(false);
            await WriteGroupsAsync(writer, file).ConfigureAwait(false);
            await WriteSegmentsAsync(writer, file).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private static async Task WriteGroupsAsync(XmlWriter writer, NzbFile file)
    {
        await writer.WriteStartElementAsync(null, NzbKeywords.Groups, null).ConfigureAwait(false);
        foreach (var group in file.Groups)
        {
            await writer.WriteElementStringAsync(null, NzbKeywords.Group, null, group).ConfigureAwait(false);
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private static async Task WriteSegmentsAsync(XmlWriter writer, NzbFile file)
    {
        await writer.WriteStartElementAsync(null, NzbKeywords.Segments, null).ConfigureAwait(false);
        foreach (var segment in file.Segments)
        {
            await writer.WriteStartElementAsync(null, NzbKeywords.Segment, null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, NzbKeywords.Bytes, null, segment.Size.ToString(CultureInfo.InvariantCulture))
                .ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, NzbKeywords.Number, null, segment.Number.ToString(CultureInfo.InvariantCulture))
                .ConfigureAwait(false);
            await writer.WriteStringAsync(segment.MessageId.Value).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }
}