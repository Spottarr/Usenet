using System.Net;
using Usenet.Nntp.Client;
using Usenet.Nntp.Models;
using Usenet.Tests.TestHelpers;

namespace Usenet.Tests.Nntp;

internal sealed class NntpXzCompressionTests
{
    private static NntpConnectionOptions LoopbackOptions(int port) =>
        new() { Host = IPAddress.Loopback.ToString(), Port = port };

    private static async Task<NntpClient> ConnectAsync(
        NntpConnection connection,
        CancellationToken cancellationToken
    )
    {
        var client = new NntpClient(connection);
        await client.ConnectAsync(cancellationToken);
        await client.AuthenticateAsync("example.user", "example.pass", cancellationToken);
        return client;
    }

    private static async Task<List<NntpArticleOverview>> CollectAsync(
        NntpClient client,
        NntpArticleRange range,
        CancellationToken cancellationToken
    )
    {
        await using var response = await client.XzverAsync(range, cancellationToken);
        var rows = new List<NntpArticleOverview>();
        await foreach (var row in response.WithCancellation(cancellationToken))
        {
            rows.Add(row);
        }

        return rows;
    }

    [Test]
    [Arguments(XzCodec.ZLib)]
    [Arguments(XzCodec.GZip)]
    [Arguments(XzCodec.Deflate)]
    public async Task ShouldDecodeXzverForEveryCodec(
        XzCodec codec,
        CancellationToken cancellationToken
    )
    {
        // The decoder sniffs the member's first byte rather than trusting the [COMPRESS=GZIP] label,
        // so zlib, gzip and raw DEFLATE all decode to the same rows. See ADR-0006.
        await using var server = new XzCompressingNntpServer(codec);
        using var connection = new NntpConnection(LoopbackOptions(server.Port));
        var client = await ConnectAsync(connection, cancellationToken);

        var rows = await CollectAsync(client, NntpArticleRange.Range(1, 5), cancellationToken);

        await Assert.That(rows.Count).IsEqualTo(5);
        await Assert.That(rows[0].Number).IsEqualTo(1L);
        await Assert.That(rows[0].Subject).IsEqualTo("Subject 1");
        await Assert.That(rows[0].MessageId.Value).IsEqualTo("1@example.com");
        await Assert.That(rows[4].Number).IsEqualTo(5L);
        await Assert.That(rows[4].Bytes).IsEqualTo(1024L);
        await Assert.That(rows[4].Lines).IsEqualTo(8);
    }

    [Test]
    public async Task XzverShouldMatchPlaintextXover(CancellationToken cancellationToken)
    {
        // The decompressed block is byte-identical to XOVER (dot terminator included), so the typed
        // rows from XZVER must equal those from XOVER over the same range.
        await using var server = new XzCompressingNntpServer();
        using var connection = new NntpConnection(LoopbackOptions(server.Port));
        var client = await ConnectAsync(connection, cancellationToken);

        var xz = await CollectAsync(client, NntpArticleRange.Range(1, 4), cancellationToken);

        await using var plain = await client.XoverAsync(
            NntpArticleRange.Range(1, 4),
            cancellationToken
        );
        var xover = new List<NntpArticleOverview>();
        await foreach (var row in plain.WithCancellation(cancellationToken))
        {
            xover.Add(row);
        }

        await Assert
            .That(xz.Select(r => r.MessageId.Value).ToList())
            .IsEquivalentTo(xover.Select(r => r.MessageId.Value).ToList());
        await Assert.That(xz.Count).IsEqualTo(xover.Count);
    }

    [Test]
    public async Task ShouldDecodeXzhdr(CancellationToken cancellationToken)
    {
        await using var server = new XzCompressingNntpServer();
        using var connection = new NntpConnection(LoopbackOptions(server.Port));
        var client = await ConnectAsync(connection, cancellationToken);

        await using var response = await client.XzhdrAsync(
            "Subject",
            NntpArticleRange.Range(1, 3),
            cancellationToken
        );

        var rows = new List<NntpHeaderField>();
        await foreach (var row in response.WithCancellation(cancellationToken))
        {
            rows.Add(row);
        }

        await Assert.That(rows.Count).IsEqualTo(3);
        await Assert.That(rows[0].ArticleNumber).IsEqualTo(1L);
    }

    [Test]
    public async Task ShouldServeManyCommandsOverOnePersistentConnection(
        CancellationToken cancellationToken
    )
    {
        // The headline regression: the per-command decompression scope must tear down at the in-band
        // dot and restore the plaintext reader, so a second command on the same connection works.
        await using var server = new XzCompressingNntpServer();
        using var connection = new NntpConnection(LoopbackOptions(server.Port));
        var client = await ConnectAsync(connection, cancellationToken);

        var first = await CollectAsync(client, NntpArticleRange.Range(1, 3), cancellationToken);
        await Assert.That(first.Count).IsEqualTo(3);

        // A plaintext command after an XZ scope proves the reader was restored.
        await using var date = await client.XzverAsync(
            NntpArticleRange.Range(1, 7),
            cancellationToken
        );
        var second = new List<NntpArticleOverview>();
        await foreach (var row in date.WithCancellation(cancellationToken))
        {
            second.Add(row);
        }

        await Assert.That(second.Count).IsEqualTo(7);
    }
}
