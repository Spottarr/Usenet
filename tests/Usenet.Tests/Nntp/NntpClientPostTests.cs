using System.Net;
using Usenet.Nntp;
using Usenet.Nntp.Builders;
using Usenet.Nntp.Parsers;
using Usenet.Tests.TestHelpers;
using Usenet.Yenc;

namespace Usenet.Tests.Nntp;

internal sealed class NntpClientPostTests
{
    private static NntpConnectionOptions LoopbackOptions(int port) =>
        new() { Host = IPAddress.Loopback.ToString(), Port = port };

    [Test]
    public async Task PostShouldRoundTripArticle(CancellationToken cancellationToken)
    {
        using var server = new TestNntpServer();
        using var connection = new NntpConnection(LoopbackOptions(server.Port));
        var client = new NntpClient(connection);

        await client.ConnectAsync(cancellationToken);

        var article = new NntpArticleBuilder()
            .SetMessageId("1@example.com")
            .SetFrom("\"Demo User\" <nobody@example.net>")
            .SetSubject("test subject")
            .AddGroups("alt.test")
            .AddLine("first body line")
            .AddLine(".dot-stuffed line")
            .Build();

        var posted = await client.PostAsync(article, cancellationToken);
        await Assert.That(posted).IsTrue();

        var dataBlock = await server.PostedArticle.WaitAsync(cancellationToken);
        await Assert.That(dataBlock).Contains("Newsgroups: alt.test");
        await Assert.That(dataBlock).Contains("Subject: test subject");
        await Assert.That(dataBlock).Contains("first body line");
        // The body line starting with a dot must survive dot-stuffing and unstuffing intact.
        await Assert.That(dataBlock).Contains(".dot-stuffed line");
    }

    [Test]
    public async Task IhaveShouldRoundTripArticle(CancellationToken cancellationToken)
    {
        using var server = new TestNntpServer();
        using var connection = new NntpConnection(LoopbackOptions(server.Port));
        var client = new NntpClient(connection);

        await client.ConnectAsync(cancellationToken);

        var article = new NntpArticleBuilder()
            .SetMessageId("2@example.com")
            .SetFrom("\"Demo User\" <nobody@example.net>")
            .SetSubject("ihave subject")
            .AddGroups("alt.test")
            .AddLine("ihave body line")
            .Build();

        var transferred = await client.IhaveAsync(article, cancellationToken);
        await Assert.That(transferred).IsTrue();

        var dataBlock = await server.PostedArticle.WaitAsync(cancellationToken);
        await Assert.That(dataBlock).Contains("ihave body line");
    }

    [Test]
    public async Task PostShouldStreamYencBodyThroughOutputSink(CancellationToken cancellationToken)
    {
        using var server = new TestNntpServer();
        using var connection = new NntpConnection(LoopbackOptions(server.Port));

        await connection.ConnectAsync(new ResponseParser(200), cancellationToken);

        var initial = await connection.CommandAsync(
            "POST",
            new ResponseParser(340),
            cancellationToken
        );
        await Assert.That(initial.Success).IsTrue();

        var data = new byte[2048];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)((i * 31 + 7) & 0xFF);
        }

        var header = new YencHeader("file.bin", data.Length, 128, 0, 1, data.Length, 0);

        // Buffer the headers, then stream the yEnc-encoded body straight into the connection's
        // byte sink (the same IBufferWriter the encoder targets) and flush once for the command.
        connection.BufferLine("Message-ID: <yenc@example.com>");
        connection.BufferLine("Newsgroups: alt.test");
        connection.BufferLine("Subject: [1/1] file.bin yEnc");
        connection.BufferLine(string.Empty);
        using (var source = new MemoryStream(data))
        {
            await YencEncoder.EncodeAsync(header, source, connection.Output, cancellationToken);
        }
        connection.BufferLine(".");
        await connection.FlushAsync(cancellationToken);

        var response = await connection.GetResponseAsync(
            new ResponseParser(240),
            cancellationToken
        );
        await Assert.That(response.Success).IsTrue();

        var dataBlock = await server.PostedArticle.WaitAsync(cancellationToken);
        await Assert.That(dataBlock).Contains("=ybegin line=128 size=2048 name=file.bin");
        await Assert
            .That(dataBlock.Any(line => line.StartsWith("=yend", StringComparison.Ordinal)))
            .IsTrue();
    }
}
