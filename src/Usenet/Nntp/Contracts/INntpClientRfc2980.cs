using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Contracts;

public interface INntpClientRfc2980
{
    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc2980#section-2.6">XHDR</a>
    /// command is used to retrieve a specific header from a specific article.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="messageId">The message-id of the article to retrieve the header for.</param>
    /// <returns>A multi-line response object containing the header.</returns>
    NntpMultiLineResponse Xhdr(string field, NntpMessageId messageId);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc2980#section-2.6">XHDR</a>
    /// command is used to retrieve a specific header from specific articles.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <param name="range">The range of articles to retrieve the header for.</param>
    /// <returns>A multi-line response object containing the headers.</returns>
    NntpMultiLineResponse Xhdr(string field, NntpArticleRange range);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc2980#section-2.6">XHDR</a>
    /// command is used to retrieve a specific header from the current article.
    /// </summary>
    /// <param name="field">The header field to retrieve.</param>
    /// <returns>A multi-line response object containing the headers.</returns>
    NntpMultiLineResponse Xhdr(string field);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc2980#section-2.8">XOVER</a>
    /// command returns information from the overview database for the article(s) specified.
    /// </summary>
    /// <param name="range">The range of articles to retrieve the overview information for.</param>
    /// <returns>A multi-line response object containing the overview database information.</returns>
    NntpMultiLineResponse Xover(NntpArticleRange range);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc2980#section-2.8">XOVER</a>
    /// command returns information from the overview database for the current article.
    /// </summary>
    /// <returns>A multi-line response object containing the overview database information.</returns>
    NntpMultiLineResponse Xover();
}
