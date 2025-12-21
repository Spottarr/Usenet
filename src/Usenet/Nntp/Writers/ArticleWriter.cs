using Usenet.Nntp.Contracts;
using Usenet.Nntp.Models;
using Usenet.Util.Compatibility;

namespace Usenet.Nntp.Writers;

internal class ArticleWriter
{
    private const int MaxHeaderLength = 998;

    public static async Task WriteAsync(INntpConnection connection, NntpArticle article, CancellationToken cancellationToken = default)
    {
        await WriteHeadersAsync(connection, article, cancellationToken).ConfigureAwait(false);
        await connection.WriteLineAsync(string.Empty, cancellationToken).ConfigureAwait(false);
        await WriteBodyAsync(connection, article, cancellationToken).ConfigureAwait(false);
        await connection.WriteLineAsync(".", cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteHeadersAsync(INntpConnection connection, NntpArticle article, CancellationToken cancellationToken)
    {
        await WriteHeaderAsync(connection, NntpHeaders.MessageId, article.MessageId, cancellationToken).ConfigureAwait(false);
        await WriteHeaderAsync(connection, NntpHeaders.Newsgroups, article.Groups.ToString(), cancellationToken).ConfigureAwait(false);
        foreach (var header in article.Headers)
        {
            if (header.Key == NntpHeaders.MessageId ||
                header.Key == NntpHeaders.Newsgroups)
            {
                // skip message-id and newsgroups, they are already written
                continue;
            }

            foreach (var value in header.Value)
            {
                await WriteHeaderAsync(connection, header.Key, value, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task WriteHeaderAsync(INntpConnection connection, string key, string val, CancellationToken cancellationToken)
    {
        if (key == NntpHeaders.MessageId)
        {
            val = new NntpMessageId(val);
        }

        var line = $"{key}: {val}";
        if (line.Length <= MaxHeaderLength)
        {
            await connection.WriteLineAsync(line, cancellationToken).ConfigureAwait(false);
            return;
        }

        // header line is too long, fold it
        await connection.WriteLineAsync(line.Substring(0, MaxHeaderLength), cancellationToken).ConfigureAwait(false);
        line = line.Substring(MaxHeaderLength);
        while (line.Length > MaxHeaderLength)
        {
            await connection.WriteLineAsync(StringShims.Concat("\t".AsSpan(), line.AsSpan(0, MaxHeaderLength - 1)), cancellationToken).ConfigureAwait(false);
            line = line.Substring(MaxHeaderLength - 1);
        }

        await connection.WriteLineAsync("\t" + line, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteBodyAsync(INntpConnection connection, NntpArticle article, CancellationToken cancellationToken)
    {
        foreach (var line in article.Body)
        {
            if (line.Length > 0 && line[0] == '.')
            {
                await connection.WriteLineAsync("." + line, cancellationToken).ConfigureAwait(false);
                continue;
            }

            await connection.WriteLineAsync(line, cancellationToken).ConfigureAwait(false);
        }
    }
}
