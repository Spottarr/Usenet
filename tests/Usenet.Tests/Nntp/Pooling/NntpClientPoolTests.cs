using NSubstitute;
using Usenet.Nntp;
using Usenet.Nntp.Contracts;
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
        await pool.GetLease();

        // Get the second lease, should throw because the client does not become available again in time
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await pool.GetLease().ConfigureAwait(false));
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
        var lease1 = await pool.GetLease();
        lease1.Dispose();

        // Get the second lease, this should succeed because the first client was returned to the pool
        var lease2 = await pool.GetLease();
        lease2.Dispose();
    }

    private static IInternalPooledNntpClient GetClientMock()
    {
        var client = Substitute.For<IInternalPooledNntpClient>();
        client.Connected.Returns(true);
        client.Authenticated.Returns(true);
        return client;
    }
}
