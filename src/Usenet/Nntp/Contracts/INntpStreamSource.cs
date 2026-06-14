namespace Usenet.Nntp.Contracts;

/// <summary>
/// Represents the line source backing a streamed multi-line response. The source owns the
/// connection while a data block is in flight: lines are pulled one at a time and the block
/// must be drained before the connection can serve another command.
/// </summary>
internal interface INntpStreamSource
{
    /// <summary>
    /// Reads the next line of the in-flight multi-line data block, with dot-stuffing undone.
    /// Returns <see langword="null"/> once the terminating dot (or the end of input) is reached,
    /// after which the connection is reusable.
    /// </summary>
    ValueTask<string?> ReadStreamLineAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Consumes any remaining lines of the in-flight multi-line data block up to and including the
    /// terminating dot, leaving the connection clean for the next command. A no-op when no data
    /// block is in flight.
    /// </summary>
    ValueTask DrainStreamAsync(CancellationToken cancellationToken);
}
