namespace Usenet.Extensions;

internal static class StreamReaderExtensions
{
    public static Task<string> ReadToEndAsync(
        this StreamReader reader,
        CancellationToken cancellationToken
    )
    {
#if NET7_0_OR_GREATER
        return reader.ReadToEndAsync(cancellationToken);
#else
        cancellationToken.ThrowIfCancellationRequested();
        return reader.ReadToEndAsync();
#endif
    }
}
