using JetBrains.Annotations;

namespace Usenet.Nntp;

/// <summary>
/// The compression mode negotiated for a connection's multi-line data blocks.
/// </summary>
/// <remarks>
/// Compression is configuration rather than a command: it is negotiated as part of the connection's
/// session-setup recipe so it survives transparent pool reconnects. See
/// <a href="https://github.com/Spottarr/Usenet/blob/main/docs/adr/0005-compressed-overview-transport-and-connection-options.md">ADR-0005</a>.
/// </remarks>
[PublicAPI]
public enum NntpCompression
{
    /// <summary>No compression; multi-line data blocks are transferred as plain text.</summary>
    None = 0,

    /// <summary>
    /// <c>XFEATURE COMPRESS GZIP</c> where the compressed block is followed by a literal terminating
    /// dot line, so the framer can find the block boundary without trusting the gzip trailer.
    /// </summary>
    GzipWithTerminator,

    /// <summary>
    /// <c>XFEATURE COMPRESS GZIP</c> where the compressed block is not followed by a terminating dot
    /// line; the block ends with the stream.
    /// </summary>
    Gzip,
}
