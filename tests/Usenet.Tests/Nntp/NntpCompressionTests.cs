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
    public async Task ShouldStreamXoverOverTerminatedCompressedBlock(
        CancellationToken cancellationToken
    )
    {
        await using var server = new CompressingNntpServer(withTerminator: true);
        using var connection = new NntpConnection(
            LoopbackOptions(server.Port, NntpCompression.GzipWithTerminator)
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
    public async Task ShouldStreamXoverOverNonTerminatedCompressedBlock(
        CancellationToken cancellationToken
    )
    {
        await using var server = new CompressingNntpServer(withTerminator: false);
        using var connection = new NntpConnection(
            LoopbackOptions(server.Port, NntpCompression.Gzip)
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

        await Assert.That(server.Negotiations).IsEqualTo(1);
        await Assert.That(rows.Count).IsEqualTo(3);
        await Assert.That(rows[2].MessageId.Value).IsEqualTo("3@example.com");
    }

    [Test]
    public async Task ShouldNotNegotiateWhenCompressionDisabled(CancellationToken cancellationToken)
    {
        await using var server = new CompressingNntpServer(withTerminator: true);
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
    public async Task ShouldSurfaceTransportErrorOnCorruptCompressedBlock(
        CancellationToken cancellationToken
    )
    {
        await using var server = new CompressingNntpServer(
            withTerminator: true,
            fault: CompressionFault.CorruptPayload
        );
        using var connection = new NntpConnection(
            LoopbackOptions(server.Port, NntpCompression.GzipWithTerminator)
        );
        var client = await ConnectAndAuthenticateAsync(connection, cancellationToken);

        await Assert
            .That(async () =>
                await client.XoverAsync(NntpArticleRange.Range(1, 3), cancellationToken)
            )
            .ThrowsExactly<NntpException>();
    }

    [Test]
    public async Task ShouldSurfaceTransportErrorWhenTerminatorMissing(
        CancellationToken cancellationToken
    )
    {
        await using var server = new CompressingNntpServer(
            withTerminator: true,
            fault: CompressionFault.DropTerminator
        );
        using var connection = new NntpConnection(
            LoopbackOptions(server.Port, NntpCompression.GzipWithTerminator)
        );
        var client = await ConnectAndAuthenticateAsync(connection, cancellationToken);

        await Assert
            .That(async () =>
                await client.XoverAsync(NntpArticleRange.Range(1, 3), cancellationToken)
            )
            .ThrowsExactly<NntpException>();
    }

    [Test]
    public async Task PoolReAppliesCompressionOnReconnect(CancellationToken cancellationToken)
    {
        await using var server = new CompressingNntpServer(withTerminator: true);
        using var pool = new NntpClientPool(
            new NntpPoolOptions
            {
                MaxPoolSize = 1,
                Username = "example.user",
                Password = "example.pass",
                Connection = LoopbackOptions(server.Port, NntpCompression.GzipWithTerminator),
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
        // ends up in plain mode while the framer expects compressed bytes.
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
