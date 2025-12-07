namespace Usenet.Extensions;

internal static class StreamReaderExtensions
{
    public static Task<string> ReadToEndAsync(this StreamReader reader, CancellationToken cancellationToken)
    {
#if NET7_0_OR_GREATER
        return reader.ReadToEndAsync(cancellationToken);
#else
        cancellationToken.ThrowIfCancellationRequested();
        return reader.ReadToEndAsync();
#endif
    }

    public static Task<string> ReadLineAsync(this StreamReader reader, CancellationToken cancellationToken)
    {
#if NET7_0_OR_GREATER
        return reader.ReadLineAsync(cancellationToken);
#else
        cancellationToken.ThrowIfCancellationRequested();
        return reader.ReadLineAsync();
#endif
    }
}

