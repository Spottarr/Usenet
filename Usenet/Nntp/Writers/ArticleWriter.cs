using Usenet.Nntp.Models;
using Usenet.Util;

namespace Usenet.Nntp.Writers;

internal class ArticleWriter
{
    private const int MaxHeaderLength = 998;

    public static void Write(INntpConnection connection, NntpArticle article)
    {
        WriteHeaders(connection, article);
        connection.WriteLine(string.Empty);
        WriteBody(connection, article);
        connection.WriteLine(".");
    }

    private static void WriteHeaders(INntpConnection connection, NntpArticle article)
    {
        WriteHeader(connection, NntpHeaders.MessageId, article.MessageId);
        WriteHeader(connection, NntpHeaders.Newsgroups, article.Groups.ToString());
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
                WriteHeader(connection, header.Key, value);
            }
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
            connection.WriteLine(line);
            return;
        }

        // header line is too long, fold it
        connection.WriteLine(line.Substring(0, MaxHeaderLength));
        line = line.Substring(MaxHeaderLength);
        while (line.Length > MaxHeaderLength)
        {
            connection.WriteLine(StringShims.Concat("\t".AsSpan(), line.AsSpan(0, MaxHeaderLength - 1)));
            line = line.Substring(MaxHeaderLength - 1);
        }

        connection.WriteLine("\t" + line);
    }

    private static void WriteBody(INntpConnection connection, NntpArticle article)
    {
        foreach (var line in article.Body)
        {
            if (line.Length > 0 && line[0] == '.')
            {
                connection.WriteLine("." + line);
                continue;
            }

            connection.WriteLine(line);
        }
    }
}