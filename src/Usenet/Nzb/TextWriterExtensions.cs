namespace Usenet.Nzb;

/// <summary>
/// TextWriter extension methods.
/// </summary>
public static class TextWriterExtensions
{
    /// <summary>
    /// Writes the specified <see cref="NzbDocument"/> asynchronously to the stream.
    /// </summary>
    /// <param name="textWriter">The <see cref="TextWriter"/> to use.</param>
    /// <param name="nzbDocument">The <see cref="NzbDocument"/> to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> that can be awaited.</returns>
    public static Task WriteNzbDocumentAsync(
        this TextWriter textWriter,
        NzbDocument nzbDocument,
        CancellationToken cancellationToken = default
    )
    {
        return new NzbWriter(textWriter).WriteAsync(nzbDocument, cancellationToken);
    }
}
