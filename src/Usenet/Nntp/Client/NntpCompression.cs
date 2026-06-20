using JetBrains.Annotations;

namespace Usenet.Nntp.Client;

/// <summary>
/// The compression mode negotiated for a connection.
/// </summary>
/// <remarks>
/// Compression is configuration rather than a command: it is negotiated as part of the connection's
/// session-setup recipe so it survives transparent pool reconnects.
/// </remarks>
[PublicAPI]
public enum NntpCompression
{
    /// <summary>No compression; the connection is transferred as plain text.</summary>
    None = 0,

    /// <summary>
    /// <a href="https://www.rfc-editor.org/rfc/rfc8054">RFC 8054</a> <c>COMPRESS DEFLATE</c>: once
    /// negotiated, the whole session is carried as a continuous raw-DEFLATE stream in both directions,
    /// commands and responses alike.
    /// </summary>
    Deflate,
}
