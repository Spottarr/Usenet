using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using Usenet.Nntp;
using Usenet.Nntp.Contracts;
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
                new NntpClientPool(maxPoolSize, string.Empty, 0, true, string.Empty, string.Empty)
            )
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public async Task AllClientsBorrowed(CancellationToken cancellationToken)
    {
        using var pool = new NntpClientPool(
            1,
            "example.server",
            563,
            true,
            "example.user",
            "example.pass"
        )
        {
            WaitTimeout = TimeSpan.Zero,
            ClientFactory = GetClientMock,
        };

        // Get the first lease, this should succeed
        await pool.GetLease(cancellationToken).ConfigureAwait(true);

        // Get the second lease, should throw because the client does not become available again in time
        await Assert
            .That(async () => await pool.GetLease(cancellationToken).ConfigureAwait(true))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ClientAvailable(CancellationToken cancellationToken)
    {
        using var pool = new NntpClientPool(
            1,
            "example.server",
            563,
            true,
            "example.user",
            "example.pass"
        )
        {
            WaitTimeout = TimeSpan.Zero,
            ClientFactory = GetClientMock,
        };

        // Get the first lease, this should succeed
        var lease1 = await pool.GetLease(cancellationToken).ConfigureAwait(true);
        lease1.Dispose();

        // Get the second lease, this should succeed because the first client was returned to the pool
        var lease2 = await pool.GetLease(cancellationToken).ConfigureAwait(true);
        lease2.Dispose();
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public async Task DisposeClientAfterError(CancellationToken cancellationToken)
    {
        using var server = new TestNntpServer();
        using var pool = new NntpClientPool(
            1,
            "127.0.0.1",
            server.Port,
            false,
            "example.user",
            "example.pass"
        )
        {
            WaitTimeout = TimeSpan.Zero,
        };

        // Get the first lease
        var lease1 = await pool.GetLease(cancellationToken).ConfigureAwait(true);
        var client1 = lease1.Client;
        try
        {
            // Group command triggers disconnect on server side - throws IOException when connection is reset
            // or NntpException when no response is received
            await Assert
                .That(async () =>
                    await lease1
                        .Client.GroupAsync("some.group", cancellationToken)
                        .ConfigureAwait(true)
                )
                .ThrowsException();
        }
        finally
        {
            lease1.Dispose();
        }

        // The client should be disposed after the disconnect
        await Assert
            .That(async () =>
                await client1.ArticleAsync("123", cancellationToken).ConfigureAwait(true)
            )
            .ThrowsExactly<ObjectDisposedException>();

        // Get the second lease
        // This should return a new client
        var lease2 = await pool.GetLease(cancellationToken).ConfigureAwait(true);
        var client2 = lease2.Client;
        await lease2.Client.ArticleAsync("123", cancellationToken).ConfigureAwait(true);
        lease2.Dispose();

        await Assert.That(client2).IsNotEqualTo(client1);
    }

    private static IInternalPooledNntpClient GetClientMock()
    {
        var client = Substitute.For<IInternalPooledNntpClient>();
        client.Connected.Returns(true);
        client.Authenticated.Returns(true);
        return client;
    }
}
