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
    /// <returns>A response object.</returns>
    Task<NntpResponse> XfeatureCompressGzipAsync(bool withTerminator);

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="withTerminator">Whether to include terminator.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object.</returns>
    Task<NntpResponse> XfeatureCompressGzipAsync(
        bool withTerminator,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="messageId">The message-id of the article.</param>
    /// <returns>A multi-line response object.</returns>
    Task<NntpMultiLineResponse> XzhdrAsync(string field, NntpMessageId messageId);

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="messageId">The message-id of the article.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object.</returns>
    Task<NntpMultiLineResponse> XzhdrAsync(
        string field,
        NntpMessageId messageId,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="range">The article range.</param>
    /// <returns>A multi-line response object.</returns>
    Task<NntpMultiLineResponse> XzhdrAsync(string field, NntpArticleRange range);

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="range">The article range.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object.</returns>
    Task<NntpMultiLineResponse> XzhdrAsync(
        string field,
        NntpArticleRange range,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <returns>A multi-line response object.</returns>
    Task<NntpMultiLineResponse> XzhdrAsync(string field);

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object.</returns>
    Task<NntpMultiLineResponse> XzhdrAsync(string field, CancellationToken cancellationToken);

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="range">The article range.</param>
    /// <returns>A multi-line response object.</returns>
    Task<NntpMultiLineResponse> XzverAsync(NntpArticleRange range);

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="range">The article range.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object.</returns>
    Task<NntpMultiLineResponse> XzverAsync(
        NntpArticleRange range,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <returns>A multi-line response object.</returns>
    Task<NntpMultiLineResponse> XzverAsync();

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object.</returns>
    Task<NntpMultiLineResponse> XzverAsync(CancellationToken cancellationToken);
}
