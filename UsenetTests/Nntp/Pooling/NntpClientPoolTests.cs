using NSubstitute;
using Usenet.Nntp;
using Xunit;

namespace UsenetTests.Nntp.Pooling;

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

        // Get first client, should succeed
        await pool.BorrowClient();

        // Get second client, should throw because the client does not become available again in time
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pool.BorrowClient().ConfigureAwait(false));
    }

    [Fact]
    public async Task ClientAvailable()
    {
        using var pool = new NntpClientPool(1, "example.server", 563, true, "example.user", "example.pass")
        {
            WaitTimeout = TimeSpan.Zero,
            ClientFactory = GetClientMock
        };

        // Get first client, should succeed
        var client = await pool.BorrowClient();

        pool.ReturnClient(client);

        // Get second client, should succeed because the first client was returned to the pool
        await pool.BorrowClient();
    }

    private static PooledNntpClient GetClientMock()
    {
        var client = Substitute.For<PooledNntpClient>();
        client.Connected = true;
        client.Authenticated = true;
        client.When(x => x.Flush()).Do(_ => { });
        return client;
    }
}
