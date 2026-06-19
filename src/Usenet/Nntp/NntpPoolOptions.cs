using JetBrains.Annotations;

namespace Usenet.Nntp;

/// <summary>
/// Configuration for an <see cref="NntpClientPool"/>: the connection options applied to every pooled
/// client, the credentials used to authenticate them, and the maximum size of the pool.
/// </summary>
/// <remarks>
/// The pool re-applies the connection options and credentials on every transparent reconnect, so they
/// are configuration rather than per-lease arguments.
/// </remarks>
[PublicAPI]
public sealed class NntpPoolOptions
{
    /// <summary>The connection configuration applied to every client in the pool.</summary>
    public required NntpConnectionOptions Connection { get; init; }

    /// <summary>The username used to authenticate each pooled connection.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>The password used to authenticate each pooled connection.</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>The maximum number of clients the pool will hold.</summary>
    public required int MaxPoolSize { get; init; }
}
