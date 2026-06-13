namespace Usenet.Extensions;

internal static class StreamWriterExtensions
{
    public static async Task WriteLineAsync(
        this StreamWriter writer,
        string value,
        CancellationToken cancellationToken
    )
    {
        await writer.WriteLineAsync(value.AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}
