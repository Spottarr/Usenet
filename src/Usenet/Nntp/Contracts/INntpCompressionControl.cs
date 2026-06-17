namespace Usenet.Nntp.Contracts;

/// <summary>
/// The internal seam the session-setup recipe uses to negotiate <c>XFEATURE COMPRESS GZIP</c> on a
/// connection. Compression is configuration rather than a public command (ADR-0005): it is negotiated
/// after connect and authentication so it survives transparent pool reconnects, and the transport then
/// inflates the data block of every subsequent multi-line response.
/// </summary>
internal interface INntpCompressionControl
{
    /// <summary>The compression mode configured on the connection.</summary>
    NntpCompression Compression { get; }

    /// <summary>Whether the compression mode has been negotiated and is active on the transport.</summary>
    bool CompressionEnabled { get; }

    /// <summary>
    /// Negotiates the configured compression mode with the server by issuing
    /// <c>XFEATURE COMPRESS GZIP [TERMINATOR]</c>, and arms the transport's inflate stage on success.
    /// A no-op when <see cref="Compression"/> is <see cref="NntpCompression.None"/> or already enabled.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task EnableCompressionAsync(CancellationToken cancellationToken = default);
}
