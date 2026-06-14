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
        // Buffer the whole article into the connection's write buffer, then flush once. This
        // replaces the previous flush-per-line behaviour with a single batched flush per command.
        WriteHeaders(connection, article);
        connection.BufferLine(string.Empty);
        WriteBody(connection, article);
        connection.BufferLine(".");
        await connection.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void WriteHeaders(INntpConnection connection, NntpArticle article)
    {
        WriteHeader(connection, NntpHeaders.MessageId, article.MessageId);
        WriteHeader(connection, NntpHeaders.Newsgroups, article.Groups.ToString());
        foreach (var (key, value) in article.Headers)
        {
            if (key is NntpHeaders.MessageId or NntpHeaders.Newsgroups)
            {
                // skip message-id and newsgroups, they are already written
                continue;
            }

            WriteHeader(connection, key, value);
        }
    }

    private static void WriteHeader(INntpConnection connection, string key, string val)
    {
        if (key == NntpHeaders.MessageId)
        {
            val = new NntpMessageId(val);
        }

        var line = $"{key}: {val}";
        if (line.Length <= MaxHeaderLength)
        {
            connection.BufferLine(line);
            return;
        }

        // header line is too long, fold it
        connection.BufferLine(line[..MaxHeaderLength]);
        line = line[MaxHeaderLength..];
        while (line.Length > MaxHeaderLength)
        {
            connection.BufferLine(
                string.Concat("\t".AsSpan(), line.AsSpan(0, MaxHeaderLength - 1))
            );
            line = line[(MaxHeaderLength - 1)..];
        }

        connection.BufferLine("\t" + line);
    }

    private static void WriteBody(INntpConnection connection, NntpArticle article)
    {
        foreach (var line in article.Body)
        {
            if (line.Length > 0 && line[0] == '.')
            {
                connection.BufferLine("." + line);
                continue;
            }

            connection.BufferLine(line);
        }
    }
}
