using JetBrains.Annotations;

namespace Usenet.Nntp;

/// <summary>
/// Transport configuration for an <see cref="NntpConnection"/>: where to connect, whether to encrypt,
/// how long to wait for the connection, and which compression mode to negotiate.
/// </summary>
/// <remarks>
/// Consolidating connection identity into an options object lets
/// <see cref="Usenet.Nntp.Contracts.INntpConnection.ConnectAsync{TResponse}"/> read the host, port and
/// SSL setting from configuration instead of taking them as method arguments. See
/// <a href="https://github.com/Spottarr/Usenet/blob/main/docs/adr/0005-compressed-overview-transport-and-connection-options.md">ADR-0005</a>.
/// </remarks>
[PublicAPI]
public sealed class NntpConnectionOptions
{
    /// <summary>The hostname of the usenet server.</summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>The port to connect on. Defaults to the standard NNTP port 119.</summary>
    public int Port { get; init; } = 119;

    /// <summary>A value indicating whether to use SSL encryption for the connection.</summary>
    public bool UseSsl { get; init; }

    /// <summary>
    /// The maximum time to wait for the TCP connection to be established. A non-positive value
    /// disables the timeout. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The compression mode to negotiate for multi-line data blocks. When set to a gzip variant, the
    /// connection negotiates <c>XFEATURE COMPRESS GZIP</c> after authentication and transparently
    /// inflates the compressed data block of subsequent multi-line responses. Defaults to
    /// <see cref="NntpCompression.None"/>.
    /// </summary>
    public NntpCompression Compression { get; init; } = NntpCompression.None;
}
