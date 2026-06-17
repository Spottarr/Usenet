using System.Net;
using Usenet.Nntp;
using Usenet.Nntp.Models;
using Usenet.Tests.TestHelpers;

namespace Usenet.Tests.Nntp;

internal sealed class NntpClientStreamingTests
{
    private static async Task<NntpClient> ConnectAsync(
        NntpConnection connection,
        StreamingNntpServer server,
        CancellationToken cancellationToken
    )
    {
        var client = new NntpClient(connection);
        await client.ConnectAsync(
            IPAddress.Loopback.ToString(),
            server.Port,
            false,
            cancellationToken
        );
        return client;
    }

    [Test]
    public async Task ShouldStreamXoverRangePerLine(CancellationToken cancellationToken)
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        await using var response = await client.XoverAsync(
            NntpArticleRange.Range(1, 5),
            cancellationToken
        );

        await Assert.That(response.Success).IsTrue();

        var rows = new List<NntpArticleOverview>();
        await foreach (var row in response.WithCancellation(cancellationToken))
        {
            rows.Add(row);
        }

        await Assert.That(rows.Count).IsEqualTo(5);
        await Assert.That(rows[0].Number).IsEqualTo(1L);
        await Assert.That(rows[0].Subject).IsEqualTo("Subject 1");
        await Assert.That(rows[0].MessageId.Value).IsEqualTo("1@example.com");
    }

    [Test]
    public async Task ShouldStreamOverRangePerLine(CancellationToken cancellationToken)
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        await using var response = await client.OverAsync(
            NntpArticleRange.Range(1, 5),
            cancellationToken
        );

        await Assert.That(response.Success).IsTrue();

        var rows = new List<NntpArticleOverview>();
        await foreach (var row in response.WithCancellation(cancellationToken))
        {
            rows.Add(row);
        }

        await Assert.That(rows.Count).IsEqualTo(5);
        await Assert.That(rows[0].Number).IsEqualTo(1L);
        await Assert.That(rows[0].Subject).IsEqualTo("Subject 1");
        await Assert.That(rows[0].MessageId.Value).IsEqualTo("1@example.com");
        await Assert.That(rows[4].Bytes).IsEqualTo(1024L);
        await Assert.That(rows[4].Lines).IsEqualTo(8);
    }

    [Test]
    public async Task ShouldReturnSingleOverviewByMessageId(CancellationToken cancellationToken)
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        var overview = await client.OverByMessageIdAsync(
            new NntpMessageId("42@example.com"),
            cancellationToken
        );

        await Assert.That(overview).IsNotNull();
        await Assert.That(overview!.Number).IsEqualTo(0L);
        await Assert.That(overview.MessageId.Value).IsEqualTo("42@example.com");
        await Assert.That(overview.Bytes).IsEqualTo(2048L);

        // The single-record form drains its own data block, so the connection serves the next command.
        var date = await client.DateAsync(cancellationToken);
        await Assert.That(date.Code).IsEqualTo(111);
    }

    [Test]
    public async Task ShouldReturnNullOverviewWhenArticleAbsent(CancellationToken cancellationToken)
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        var overview = await client.OverByMessageIdAsync(
            new NntpMessageId(StreamingNntpServer.MissingMessageId),
            cancellationToken
        );

        await Assert.That(overview).IsNull();
    }

    [Test]
    public async Task ShouldReturnSingleHeaderFieldByMessageId(CancellationToken cancellationToken)
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        var header = await client.HdrByMessageIdAsync(
            "Subject",
            new NntpMessageId("42@example.com"),
            cancellationToken
        );

        await Assert.That(header).IsNotNull();
        await Assert.That(header!.ArticleNumber).IsEqualTo(0L);
        await Assert.That(header.Value).IsEqualTo("Subject value for <42@example.com>");
    }

    [Test]
    public async Task ShouldReturnSingleHeaderFieldByMessageIdForXhdr(
        CancellationToken cancellationToken
    )
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        var header = await client.XhdrByMessageIdAsync(
            "Subject",
            new NntpMessageId("42@example.com"),
            cancellationToken
        );

        await Assert.That(header).IsNotNull();
        await Assert.That(header!.Value).IsEqualTo("Subject value for <42@example.com>");
    }

    [Test]
    public async Task ShouldReturnNullHeaderFieldWhenArticleAbsent(
        CancellationToken cancellationToken
    )
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        var header = await client.HdrByMessageIdAsync(
            "Subject",
            new NntpMessageId(StreamingNntpServer.MissingMessageId),
            cancellationToken
        );

        await Assert.That(header).IsNull();
    }

    [Test]
    public async Task ShouldStreamListGroupArticleNumbers(CancellationToken cancellationToken)
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        await using var response = await client.ListGroupAsync(
            "misc.test",
            cancellationToken: cancellationToken
        );

        var numbers = new List<long>();
        await foreach (var number in response.WithCancellation(cancellationToken))
        {
            numbers.Add(number);
        }

        await Assert.That(numbers).IsEquivalentTo(new long[] { 1, 2, 3 });
    }

    [Test]
    public async Task ShouldStreamListActiveGroups(CancellationToken cancellationToken)
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        await using var response = await client.ListActiveAsync(
            cancellationToken: cancellationToken
        );

        var groups = new List<NntpGroup>();
        await foreach (var group in response.WithCancellation(cancellationToken))
        {
            groups.Add(group);
        }

        await Assert.That(groups.Count).IsEqualTo(2);
        await Assert.That(groups[0].Name).IsEqualTo("misc.test");
        await Assert.That(groups[0].HighWaterMark).IsEqualTo(3);
        await Assert.That(groups[0].LowWaterMark).IsEqualTo(1);
    }

    [Test]
    public async Task ShouldReuseConnectionAfterEarlyDisposal(CancellationToken cancellationToken)
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        // Consume a single row then dispose the response without enumerating the rest.
        await using (
            var response = await client.XoverAsync(
                NntpArticleRange.Range(1, 100),
                cancellationToken
            )
        )
        {
            await foreach (var _ in response.WithCancellation(cancellationToken))
            {
                break;
            }
        }

        // The connection must be clean: a subsequent command parses correctly.
        var date = await client.DateAsync(cancellationToken);
        await Assert.That(date.Code).IsEqualTo(111);
    }

    [Test]
    public async Task ShouldThrowWhenIssuingCommandBeforeStreamDrained(
        CancellationToken cancellationToken
    )
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        // Start a stream but do not enumerate or dispose it, leaving the data block on the wire.
        _ = await client.XoverAsync(NntpArticleRange.Range(1, 100), cancellationToken);

        await Assert.That(async () => await client.DateAsync(cancellationToken)).ThrowsException();
    }

    [Test]
    [NotInParallel]
    public async Task ShouldKeepMemoryFlatOverLargeXoverRange(CancellationToken cancellationToken)
    {
        await using var server = new StreamingNntpServer();
        using var connection = new NntpConnection();
        var client = await ConnectAsync(connection, server, cancellationToken);

        const int count = 200_000;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalMemory(forceFullCollection: true);

        long streamed = 0;
        await using (
            var response = await client.XoverAsync(
                NntpArticleRange.Range(1, count),
                cancellationToken
            )
        )
        {
            await foreach (var _ in response.WithCancellation(cancellationToken))
            {
                streamed++;
            }
        }

        var after = GC.GetTotalMemory(forceFullCollection: true);

        await Assert.That(streamed).IsEqualTo(count);

        // Each overview line is tens of bytes; materializing the whole range would retain many
        // megabytes. Streaming keeps live memory flat, so growth stays well under that.
        await Assert.That(after - before).IsLessThan(2_000_000);
    }
}
