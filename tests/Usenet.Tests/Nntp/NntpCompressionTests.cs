using System.Net;
using Usenet.Exceptions;
using Usenet.Nntp;
using Usenet.Nntp.Models;
using Usenet.Tests.TestHelpers;

namespace Usenet.Tests.Nntp;

internal sealed class NntpCompressionTests
{
    private static NntpConnectionOptions LoopbackOptions(int port, NntpCompression compression) =>
        new()
        {
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            Compression = compression,
        };

    private static async Task<NntpClient> ConnectAndAuthenticateAsync(
        NntpConnection connection,
        CancellationToken cancellationToken
    )
    {
        var client = new NntpClient(connection);
        await client.ConnectAsync(cancellationToken);
        await client.AuthenticateAsync("example.user", "example.pass", cancellationToken);
        return client;
    }

    [Test]
    public async Task ShouldStreamXoverOverCompressedConnection(CancellationToken cancellationToken)
    {
        await using var server = new CompressingNntpServer();
        using var connection = new NntpConnection(
            LoopbackOptions(server.Port, NntpCompression.Deflate)
        );
        var client = await ConnectAndAuthenticateAsync(connection, cancellationToken);

        await using var response = await client.XoverAsync(
            NntpArticleRange.Range(1, 5),
            cancellationToken
        );

        var rows = new List<NntpArticleOverview>();
        await foreach (var row in response.WithCancellation(cancellationToken))
        {
            rows.Add(row);
        }

        await Assert.That(server.Negotiations).IsEqualTo(1);
        await Assert.That(rows.Count).IsEqualTo(5);
        await Assert.That(rows[0].Number).IsEqualTo(1L);
        await Assert.That(rows[0].Subject).IsEqualTo("Subject 1");
        await Assert.That(rows[0].MessageId.Value).IsEqualTo("1@example.com");
        await Assert.That(rows[4].Bytes).IsEqualTo(1024L);
        await Assert.That(rows[4].Lines).IsEqualTo(8);
    }

    [Test]
    public async Task ShouldRetrieveArticleOverCompressedConnection(
        CancellationToken cancellationToken
    )
    {
        // Unlike the legacy XFEATURE mode, RFC 8054 compresses every response uniformly, so article
        // retrieval rides the same compressed connection as the overview scan.
        await using var server = new CompressingNntpServer();
        using var connection = new NntpConnection(
            LoopbackOptions(server.Port, NntpCompression.Deflate)
        );
        var client = await ConnectAndAuthenticateAsync(connection, cancellationToken);

        using var response = await client.ArticleAsync("1@example.com", cancellationToken);

        await Assert.That(response.Success).IsTrue();
        await Assert.That(response.Headers.Contains("Subject")).IsTrue();
        var body = response.ReadBodyLines().ToList();
        await Assert.That(body).Contains("Body line one");
        await Assert.That(body).Contains("Body line two");
    }

    [Test]
    public async Task ShouldServeManyCommandsOverOnePersistentCompressedConnection(
        CancellationToken cancellationToken
    )
    {
        // The headline regression: a self-delimiting compressed response on a persistent connection
        // must return without waiting on a socket close, so successive commands keep working.
        await using var server = new CompressingNntpServer();
        using var connection = new NntpConnection(
            LoopbackOptions(server.Port, NntpCompression.Deflate)
        );
        var client = await ConnectAndAuthenticateAsync(connection, cancellationToken);

        await Assert
            .That(await CountRowsAsync(client, NntpArticleRange.Range(1, 3), cancellationToken))
            .IsEqualTo(3);

        using (var article = await client.ArticleAsync("1@example.com", cancellationToken))
        {
            await Assert.That(article.Success).IsTrue();
        }

        await Assert
            .That(await CountRowsAsync(client, NntpArticleRange.Range(1, 7), cancellationToken))
            .IsEqualTo(7);

        await Assert.That(server.Negotiations).IsEqualTo(1);
    }

    private static async Task<int> CountRowsAsync(
        NntpClient client,
        NntpArticleRange range,
        CancellationToken cancellationToken
    )
    {
        await using var response = await client.XoverAsync(range, cancellationToken);
        var count = 0;
        await foreach (var _ in response.WithCancellation(cancellationToken))
        {
            count++;
        }

        return count;
    }

    [Test]
    public async Task ShouldNotNegotiateWhenCompressionDisabled(CancellationToken cancellationToken)
    {
        await using var server = new CompressingNntpServer();
        using var connection = new NntpConnection(
            LoopbackOptions(server.Port, NntpCompression.None)
        );
        var client = await ConnectAndAuthenticateAsync(connection, cancellationToken);

        await using var response = await client.XoverAsync(
            NntpArticleRange.Range(1, 3),
            cancellationToken
        );

        var rows = new List<NntpArticleOverview>();
        await foreach (var row in response.WithCancellation(cancellationToken))
        {
            rows.Add(row);
        }

        await Assert.That(server.Negotiations).IsEqualTo(0);
        await Assert.That(rows.Count).IsEqualTo(3);
    }

    [Test]
    public async Task ShouldThrowWhenServerRefusesCompression(CancellationToken cancellationToken)
    {
        await using var server = new CompressingNntpServer(refuseCompression: true);
        using var connection = new NntpConnection(
            LoopbackOptions(server.Port, NntpCompression.Deflate)
        );
        var client = new NntpClient(connection);
        await client.ConnectAsync(cancellationToken);

        // Negotiation is fail-fast: a refusal surfaces on the authentication step that runs the
        // session-setup recipe rather than silently leaving the connection in plain text.
        await Assert
            .That(async () =>
                await client.AuthenticateAsync("example.user", "example.pass", cancellationToken)
            )
            .ThrowsExactly<NntpException>();
    }

    [Test]
    public async Task PoolReAppliesCompressionOnReconnect(CancellationToken cancellationToken)
    {
        await using var server = new CompressingNntpServer();
        using var pool = new NntpClientPool(
            new NntpPoolOptions
            {
                MaxPoolSize = 1,
                Username = "example.user",
                Password = "example.pass",
                Connection = LoopbackOptions(server.Port, NntpCompression.Deflate),
            }
        )
        {
            WaitTimeout = TimeSpan.Zero,
            MonitorInterval = TimeSpan.FromMinutes(1),
        };

        // First lease negotiates compression, then leaves a stream undrained so the pool discards the
        // connection on return.
        var lease1 = await pool.GetLease(cancellationToken);
        var client1 = lease1.Client;
        _ = await client1.XoverAsync(NntpArticleRange.Range(1, 100), cancellationToken);
        lease1.Dispose();

        // The replacement connection must re-run the full recipe (including compression), so it never
        // ends up uncompressed while the transport expects a compressed stream.
        var lease2 = await pool.GetLease(cancellationToken);
        await Assert.That(ReferenceEquals(lease2.Client, client1)).IsFalse();

        await using (
            var response = await lease2.Client.XoverAsync(
                NntpArticleRange.Range(1, 4),
                cancellationToken
            )
        )
        {
            var count = 0;
            await foreach (var _ in response.WithCancellation(cancellationToken))
            {
                count++;
            }

            await Assert.That(count).IsEqualTo(4);
        }

        lease2.Dispose();

        await Assert.That(server.Negotiations).IsEqualTo(2);
    }
}
