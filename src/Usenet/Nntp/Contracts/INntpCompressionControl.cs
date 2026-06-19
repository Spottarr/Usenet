namespace Usenet.Nntp.Contracts;

/// <summary>
/// The internal seam the session-setup recipe uses to negotiate <c>COMPRESS DEFLATE</c> on a
/// connection. Compression is configuration rather than a public command (ADR-0005): it is negotiated
/// after connect and authentication so it survives transparent pool reconnects, and the transport then
/// carries the whole session as a continuous DEFLATE stream in both directions.
/// </summary>
internal interface INntpCompressionControl
{
    /// <summary>The compression mode configured on the connection.</summary>
    NntpCompression Compression { get; }

    /// <summary>Whether the compression mode has been negotiated and is active on the transport.</summary>
    bool CompressionEnabled { get; }

    /// <summary>
    /// Negotiates the configured compression mode with the server by issuing
    /// <a href="https://www.rfc-editor.org/rfc/rfc8054">RFC 8054</a> <c>COMPRESS DEFLATE</c>, and
    /// installs the transport's bidirectional DEFLATE layer on success. A no-op when
    /// <see cref="Compression"/> is <see cref="NntpCompression.None"/> or already enabled.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task EnableCompressionAsync(CancellationToken cancellationToken = default);
}
