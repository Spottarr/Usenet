using System.Diagnostics.CodeAnalysis;
using Usenet.Nntp;
using Usenet.Nntp.Contracts;
using Usenet.Nntp.Models;
using Usenet.Tests.TestHelpers;

namespace Usenet.Tests.Nntp.Pooling;

internal sealed class NntpClientPoolTests
{
    [Test]
    [Arguments(-1)]
    [Arguments(0)]
    public async Task InvalidMaxPoolSize(int maxPoolSize)
    {
        await Assert
            .That(() =>
                new NntpClientPool(
                    new NntpPoolOptions
                    {
                        MaxPoolSize = maxPoolSize,
                        Username = string.Empty,
                        Password = string.Empty,
                        Connection = new NntpConnectionOptions
                        {
                            Host = string.Empty,
                            Port = 0,
                            UseSsl = true,
                        },
                    }
                )
            )
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public async Task AllClientsBorrowed(CancellationToken cancellationToken)
    {
        using var pool = new NntpClientPool(
            new NntpPoolOptions
            {
                MaxPoolSize = 1,
                Username = "example.user",
                Password = "example.pass",
                Connection = new NntpConnectionOptions
                {
                    Host = "example.server",
                    Port = 563,
                    UseSsl = true,
                },
            }
        )
        {
            WaitTimeout = TimeSpan.Zero,
            ClientFactory = GetClientMock,
        };

        // Get the first lease, this should succeed
        await pool.GetLease(cancellationToken);

        // Get the second lease, should throw because the client does not become available again in time
        await Assert
            .That(async () => await pool.GetLease(cancellationToken))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ClientAvailable(CancellationToken cancellationToken)
    {
        using var pool = new NntpClientPool(
            new NntpPoolOptions
            {
                MaxPoolSize = 1,
                Username = "example.user",
                Password = "example.pass",
                Connection = new NntpConnectionOptions
                {
                    Host = "example.server",
                    Port = 563,
                    UseSsl = true,
                },
            }
        )
        {
            WaitTimeout = TimeSpan.Zero,
            ClientFactory = GetClientMock,
        };

        // Get the first lease, this should succeed
        var lease1 = await pool.GetLease(cancellationToken);
        lease1.Dispose();

        // Get the second lease, this should succeed because the first client was returned to the pool
        var lease2 = await pool.GetLease(cancellationToken);
        lease2.Dispose();
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public async Task DisposeClientAfterError(CancellationToken cancellationToken)
    {
        using var server = new TestNntpServer();
        using var pool = new NntpClientPool(
            new NntpPoolOptions
            {
                MaxPoolSize = 1,
                Username = "example.user",
                Password = "example.pass",
                Connection = new NntpConnectionOptions
                {
                    Host = "127.0.0.1",
                    Port = server.Port,
                    UseSsl = false,
                },
            }
        )
        {
            WaitTimeout = TimeSpan.Zero,
        };

        // Get the first lease
        var lease1 = await pool.GetLease(cancellationToken);
        var client1 = lease1.Client;
        try
        {
            // Group command triggers disconnect on server side - throws IOException when connection is reset
            // or NntpException when no response is received
            await Assert
                .That(async () => await lease1.Client.GroupAsync("some.group", cancellationToken))
                .ThrowsException();
        }
        finally
        {
            lease1.Dispose();
        }

        // The client should be disposed after the disconnect
        await Assert
            .That(async () => await client1.ArticleAsync("123", cancellationToken))
            .ThrowsExactly<ObjectDisposedException>();

        // Get the second lease
        // This should return a new client
        var lease2 = await pool.GetLease(cancellationToken);
        var client2 = lease2.Client;
        await lease2.Client.ArticleAsync("123", cancellationToken);
        lease2.Dispose();

        await Assert.That(client2).IsNotEqualTo(client1);
    }

    [Test]
    public async Task ParallelBorrowAndReturn(CancellationToken cancellationToken)
    {
        const int maxPoolSize = 4;

        using var pool = new NntpClientPool(
            new NntpPoolOptions
            {
                MaxPoolSize = maxPoolSize,
                Username = "example.user",
                Password = "example.pass",
                Connection = new NntpConnectionOptions
                {
                    Host = "example.server",
                    Port = 563,
                    UseSsl = true,
                },
            }
        )
        {
            // Keep the idle monitor out of the way so it does not race the borrow/return storm.
            MonitorInterval = TimeSpan.FromMinutes(1),
            ClientFactory = GetClientMock,
        };

        // Hammer the pool from many tasks at once; borrow, do a little work, then return.
        var tasks = Enumerable
            .Range(0, 200)
            .Select(async _ =>
            {
                var lease = await pool.GetLease(cancellationToken);
                await Task.Yield();
                lease.Dispose();
            });

        await Task.WhenAll(tasks);

        // The pool should never have handed out more clients than its capacity, and it should
        // still be able to lease a client once the storm has settled.
        var lease = await pool.GetLease(cancellationToken);
        lease.Dispose();
    }

    [Test]
    public async Task ReuseConnectionAfterFullyDrainedStream(CancellationToken cancellationToken)
    {
        await using var server = new StreamingNntpServer();
        using var pool = new NntpClientPool(
            new NntpPoolOptions
            {
                MaxPoolSize = 1,
                Username = "example.user",
                Password = "example.pass",
                Connection = new NntpConnectionOptions
                {
                    Host = "127.0.0.1",
                    Port = server.Port,
                    UseSsl = false,
                },
            }
        )
        {
            WaitTimeout = TimeSpan.Zero,
            MonitorInterval = TimeSpan.FromMinutes(1),
        };

        var lease1 = await pool.GetLease(cancellationToken);
        var client1 = lease1.Client;
        await using (
            var response = await client1.XoverAsync(NntpArticleRange.Range(1, 5), cancellationToken)
        )
        {
            await foreach (var _ in response.WithCancellation(cancellationToken)) { }
        }

        lease1.Dispose();

        // The stream was fully drained, so the same connection is handed back out and still works.
        var lease2 = await pool.GetLease(cancellationToken);
        await Assert.That(ReferenceEquals(lease2.Client, client1)).IsTrue();

        await using (
            var response = await lease2.Client.XoverAsync(
                NntpArticleRange.Range(1, 3),
                cancellationToken
            )
        )
        {
            var count = 0;
            await foreach (var _ in response.WithCancellation(cancellationToken))
            {
                count++;
            }

            await Assert.That(count).IsEqualTo(3);
        }

        lease2.Dispose();
    }

    [Test]
    public async Task DiscardConnectionWithUndrainedStream(CancellationToken cancellationToken)
    {
        await using var server = new StreamingNntpServer();
        using var pool = new NntpClientPool(
            new NntpPoolOptions
            {
                MaxPoolSize = 1,
                Username = "example.user",
                Password = "example.pass",
                Connection = new NntpConnectionOptions
                {
                    Host = "127.0.0.1",
                    Port = server.Port,
                    UseSsl = false,
                },
            }
        )
        {
            WaitTimeout = TimeSpan.Zero,
            MonitorInterval = TimeSpan.FromMinutes(1),
        };

        var lease1 = await pool.GetLease(cancellationToken);
        var client1 = lease1.Client;

        // Start a stream but never enumerate or dispose it: the data block is left on the wire.
        _ = await client1.XoverAsync(NntpArticleRange.Range(1, 100), cancellationToken);
        lease1.Dispose();

        // The connection had unread bytes, so it is discarded and the next lease is a fresh client.
        var lease2 = await pool.GetLease(cancellationToken);
        await Assert.That(ReferenceEquals(lease2.Client, client1)).IsFalse();

        await using (
            var response = await lease2.Client.XoverAsync(
                NntpArticleRange.Range(1, 3),
                cancellationToken
            )
        )
        {
            var count = 0;
            await foreach (var _ in response.WithCancellation(cancellationToken))
            {
                count++;
            }

            await Assert.That(count).IsEqualTo(3);
        }

        lease2.Dispose();
    }

    [SuppressMessage(
        "Performance",
        "CA1859",
        Justification = "Must return the interface type to match the pool's ClientFactory delegate."
    )]
    private static INntpPoolEntry GetClientMock()
    {
        var client = INntpPoolEntry.Mock();
        client.Connected.Returns(true);
        client.Authenticated.Returns(true);
        return client;
    }
}
