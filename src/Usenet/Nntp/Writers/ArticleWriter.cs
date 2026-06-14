using Usenet.Nntp.Contracts;
using Usenet.Nntp.Models;

namespace Usenet.Nntp.Writers;

internal static class ArticleWriter
{
    private const int MaxHeaderLength = 998;

    public static async Task WriteAsync(
        INntpConnection connection,
        NntpArticle article,
        CancellationToken cancellationToken
    )
    {
        await WriteHeadersAsync(connection, article, cancellationToken).ConfigureAwait(false);
        await connection.WriteLineAsync(string.Empty, cancellationToken).ConfigureAwait(false);
        await WriteBodyAsync(connection, article, cancellationToken).ConfigureAwait(false);
        await connection.WriteLineAsync(".", cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteHeadersAsync(
        INntpConnection connection,
        NntpArticle article,
        CancellationToken cancellationToken
    )
    {
        await WriteHeaderAsync(
                connection,
                NntpHeaders.MessageId,
                article.MessageId,
                cancellationToken
            )
            .ConfigureAwait(false);
        await WriteHeaderAsync(
                connection,
                NntpHeaders.Newsgroups,
                article.Groups.ToString(),
                cancellationToken
            )
            .ConfigureAwait(false);
        foreach (var (key, value) in article.Headers)
        {
            if (key is NntpHeaders.MessageId or NntpHeaders.Newsgroups)
            {
                // skip message-id and newsgroups, they are already written
                continue;
            }

            await WriteHeaderAsync(connection, key, value, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WriteHeaderAsync(
        INntpConnection connection,
        string key,
        string val,
        CancellationToken cancellationToken
    )
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
        await connection
            .WriteLineAsync(line[..MaxHeaderLength], cancellationToken)
            .ConfigureAwait(false);
        line = line[MaxHeaderLength..];
        while (line.Length > MaxHeaderLength)
        {
            await connection
                .WriteLineAsync(
                    string.Concat("\t".AsSpan(), line.AsSpan(0, MaxHeaderLength - 1)),
                    cancellationToken
                )
                .ConfigureAwait(false);
            line = line[(MaxHeaderLength - 1)..];
        }

        await connection.WriteLineAsync("\t" + line, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteBodyAsync(
        INntpConnection connection,
        NntpArticle article,
        CancellationToken cancellationToken
    )
    {
        foreach (var line in article.Body)
        {
            if (line.Length > 0 && line[0] == '.')
            {
                await connection
                    .WriteLineAsync("." + line, cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            await connection.WriteLineAsync(line, cancellationToken).ConfigureAwait(false);
        }
    }
}
