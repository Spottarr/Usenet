using JetBrains.Annotations;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Contracts;

[PublicAPI]
public interface INntpClientCompression
{
    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="withTerminator">Whether to include terminator.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object.</returns>
    Task<NntpResponse> XfeatureCompressGzipAsync(
        bool withTerminator,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="range">The article range.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A streamed response yielding one <see cref="NntpHeaderField"/> per article.</returns>
    Task<NntpStreamResponse<NntpHeaderField>> XzhdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="messageId">The message-id of the article.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The requested header field for the article, or <see langword="null"/> when the
    /// article is absent or carries no such field.</returns>
    Task<NntpHeaderField?> XzhdrByMessageIdAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A streamed response yielding one <see cref="NntpHeaderField"/> per article.</returns>
    Task<NntpStreamResponse<NntpHeaderField>> CurrentXzhdrAsync(
        string field,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="range">The article range.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A streamed response yielding one <see cref="NntpArticleOverview"/> per article.</returns>
    Task<NntpStreamResponse<NntpArticleOverview>> XzverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A streamed response yielding one <see cref="NntpArticleOverview"/> per article.</returns>
    Task<NntpStreamResponse<NntpArticleOverview>> CurrentXzverAsync(
        CancellationToken cancellationToken = default
    );
}
