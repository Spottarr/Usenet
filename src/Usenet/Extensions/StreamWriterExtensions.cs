namespace Usenet.Extensions;

internal static class StreamWriterExtensions
{
    public static async Task WriteLineAsync(this StreamWriter writer, string value, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        cancellationToken.ThrowIfCancellationRequested();
        await writer.WriteLineAsync(value).ConfigureAwait(false);
#else
        await writer.WriteLineAsync(value.AsMemory(), cancellationToken).ConfigureAwait(false);
#endif
    }
}
