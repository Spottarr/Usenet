using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Contracts;

public interface INntpClientCompression
{
    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="withTerminator"></param>
    /// <returns></returns>
    NntpResponse XfeatureCompressGzip(bool withTerminator);

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="messageId"></param>
    /// <returns></returns>
    NntpMultiLineResponse Xzhdr(string field, NntpMessageId messageId);

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    NntpMultiLineResponse Xzhdr(string field, NntpArticleRange range);

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    NntpMultiLineResponse Xzhdr(string field);

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    NntpMultiLineResponse Xzver(NntpArticleRange range);

    /// <summary>
    /// Needs a <a href="https://gist.github.com/keimpema/ec962384d5fe3eb7a5f5030353ba9e2b">decompressing connection</a>.
    /// </summary>
    /// <returns></returns>
    NntpMultiLineResponse Xzver();
}
