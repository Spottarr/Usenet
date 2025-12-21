using NSubstitute;
using Usenet.Nntp;
using Usenet.Nntp.Contracts;
using Usenet.Tests.TestHelpers;
using Xunit;

namespace Usenet.Tests.Nntp.Pooling;

public class NntpClientPoolTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void InvalidMaxPoolSize(int maxPoolSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NntpClientPool(maxPoolSize, string.Empty, 0, true, string.Empty, string.Empty));
    }

    [Fact]
    public async Task AllClientsBorrowed()
    {
        using var pool = new NntpClientPool(1, "example.server", 563, true, "example.user", "example.pass")
        {
            WaitTimeout = TimeSpan.Zero,
            ClientFactory = GetClientMock
        };

        // Get the first lease, this should succeed
        await pool.GetLease(TestContext.Current.CancellationToken);

        // Get the second lease, should throw because the client does not become available again in time
        await Assert.ThrowsAsync<InvalidOperationException>(() => pool.GetLease(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ClientAvailable()
    {
        using var pool = new NntpClientPool(1, "example.server", 563, true, "example.user", "example.pass")
        {
            WaitTimeout = TimeSpan.Zero,
            ClientFactory = GetClientMock
        };

        // Get the first lease, this should succeed
        var lease1 = await pool.GetLease(TestContext.Current.CancellationToken);
        lease1.Dispose();

        // Get the second lease, this should succeed because the first client was returned to the pool
        var lease2 = await pool.GetLease(TestContext.Current.CancellationToken);
        lease2.Dispose();
    }

    [Fact]
    public async Task DisposeClientAfterError()
    {
        using var server = new TestNntpServer();
        using var pool = new NntpClientPool(1, "127.0.0.1", server.Port, false, "example.user", "example.pass") { WaitTimeout = TimeSpan.Zero };

        // Get the first lease
        var lease1 = await pool.GetLease(TestContext.Current.CancellationToken);
        var client1 = lease1.Client;
        try
        {
            // Group command triggers disconnect on server side - throws IOException when connection is reset
            // or NntpException when no response is received
            await Assert.ThrowsAnyAsync<Exception>(() => lease1.Client.GroupAsync("some.group", TestContext.Current.CancellationToken));
        }
        finally
        {
            lease1.Dispose();
        }

        // The client should be disposed after the disconnect
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client1.ArticleAsync("123", TestContext.Current.CancellationToken));

        // Get the second lease
        // This should return a new client
        var lease2 = await pool.GetLease(TestContext.Current.CancellationToken);
        var client2 = lease2.Client;
        await lease2.Client.ArticleAsync("123", TestContext.Current.CancellationToken);
        lease2.Dispose();

        Assert.NotEqual(client1, client2);
    }

    private static IInternalPooledNntpClient GetClientMock()
    {
        var client = Substitute.For<IInternalPooledNntpClient>();
        client.Connected.Returns(true);
        client.Authenticated.Returns(true);
        return client;
    }
}
