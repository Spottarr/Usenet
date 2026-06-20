using JetBrains.Annotations;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Contracts;

/// <summary>
/// The non-standard <c>XZVER</c>/<c>XZHDR</c> commands: compressed siblings of <c>XOVER</c>/<c>XHDR</c>
/// offered by Highwinds-family servers (eweka and many resellers) that do not advertise RFC 8054
/// <c>COMPRESS</c>. The data block is a single compressed member with the dot terminator inside it;
/// the decoder is chosen by sniffing the member rather than trusting the advertised label. See
/// ADR-0006. Select these from the typed <see cref="NntpCapabilities"/> "because real servers
/// implement one or the other" — there is no transparent upgrade of <c>XOVER</c>/<c>XHDR</c>.
/// </summary>
[PublicAPI]
public interface INntpClientXz
{
    /// <summary>
    /// The non-standard <c>XZVER</c> command returns the same overview information as
    /// <c>XOVER</c> for the article(s) specified, but as a single compressed data block.
    /// </summary>
    /// <param name="range">The range of articles to retrieve the overview information for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object containing the overview database information.</returns>
    Task<NntpStreamResponse<NntpArticleOverview>> XzverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The non-standard <c>XZVER</c> command returns the same overview information as
    /// <c>XOVER</c> for the current article, but as a single compressed data block.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object containing the overview database information.</returns>
    Task<NntpStreamResponse<NntpArticleOverview>> CurrentXzverAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The non-standard <c>XZHDR</c> command retrieves a specific header from the specified
    /// articles, like <c>XHDR</c>, but returns it as a single compressed data block.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="range">The range of articles to retrieve the header for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object containing the headers.</returns>
    Task<NntpStreamResponse<NntpHeaderField>> XzhdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The non-standard <c>XZHDR</c> command retrieves a specific header from the current
    /// article, like <c>XHDR</c>, but returns it as a single compressed data block.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object containing the headers.</returns>
    Task<NntpStreamResponse<NntpHeaderField>> CurrentXzhdrAsync(
        string field,
        CancellationToken cancellationToken = default
    );
}
