using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Usenet.Nntp;
using Usenet.Nntp.Models;
using Usenet.Tests.TestHelpers;

namespace Usenet.Tests.Nntp;

/// <summary>
/// Allocation-regression guard for the streamed <c>XZVER</c> read path (ADR-0006), the compressed
/// sibling of <see cref="XoverAllocationTests"/>. It measures the <em>marginal</em> managed
/// allocation of a single inflated overview row by reading two ranges and dividing the allocation
/// delta by the row delta, so the fixed per-command overhead — including the constant decompression
/// window and input buffer — cancels out, pinning the steady per-row cost of the streamed
/// decompress/frame/parse path so an accidental O(rows) copy, boxing, or per-row buffer regression
/// trips CI. The loopback server replies from a pre-built, cached compressed buffer, so it allocates
/// nothing per row and only the client decompress/framing/parsing is measured.
/// </summary>
internal sealed class XzverAllocationTests
{
    private const int SmallRange = 200;
    private const int LargeRange = 2_200;

    // The marginal cost is the per-row managed churn of the streamed, typed read: the parsed
    // NntpArticleOverview (several strings, message-id and references) plus the async-enumerator and
    // decompress-pipe work. The decompressor's window and input buffer are fixed per-command costs
    // that cancel in the marginal measurement. Measured ~3 KB/row; the ceiling leaves runtime headroom
    // and trips on a gross regression.
    private const long MaxBytesPerRow = 4608;

    private static NntpConnectionOptions LoopbackOptions(int port) =>
        new() { Host = IPAddress.Loopback.ToString(), Port = port };

    // Allocation is measured with the process-wide GC counter (async work hops threads), so the test
    // must run exclusively: a concurrent test allocating on another thread would inflate the reading.
    [Test]
    [NotInParallel]
    internal async Task StreamedXzverRowShouldStayUnderAllocationCeiling(
        CancellationToken cancellationToken
    )
    {
        await using var server = new CachedXzServer();
        using var connection = new NntpConnection(LoopbackOptions(server.Port));
        var client = new NntpClient(connection);
        await client.ConnectAsync(cancellationToken);

        await ReadXzverAsync(client, SmallRange, cancellationToken);
        await ReadXzverAsync(client, LargeRange, cancellationToken);

        var perRow = long.MaxValue;
        for (var repeat = 0; repeat < 5; repeat++)
        {
            var smallBytes = await AllocationMeasurement.TotalAsync(() =>
                ReadXzverAsync(client, SmallRange, cancellationToken)
            );
            var largeBytes = await AllocationMeasurement.TotalAsync(() =>
                ReadXzverAsync(client, LargeRange, cancellationToken)
            );

            var marginal = (largeBytes - smallBytes) / (LargeRange - SmallRange);
            perRow = Math.Min(perRow, marginal);
        }

        await Assert.That(perRow).IsLessThanOrEqualTo(MaxBytesPerRow);
    }

    private static async Task ReadXzverAsync(
        NntpClient client,
        int count,
        CancellationToken cancellationToken
    )
    {
        await using var response = await client.XzverAsync(
            NntpArticleRange.Range(1, count),
            cancellationToken
        );

        var rows = 0;
        await foreach (var _ in response.WithCancellation(cancellationToken))
        {
            rows++;
        }

        if (rows != count)
        {
            throw new InvalidOperationException($"Expected {count} overview rows, got {rows}.");
        }
    }

    /// <summary>
    /// A loopback server that answers <c>XZVER from-to</c> with a clear-text status line and a
    /// pre-built, cached zlib member (dot terminator inside) so it allocates nothing per row while
    /// the client read is measured.
    /// </summary>
    private sealed class CachedXzServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<int, byte[]> _cache = new();

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public CachedXzServer()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _ = AcceptLoop();
        }

        private async Task AcceptLoop()
        {
            var cancellationToken = _cts.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                var stream = client.GetStream();
                using var reader = new StreamReader(
                    stream,
                    Encoding.ASCII,
                    false,
                    1024,
                    leaveOpen: true
                );

                await stream.WriteAsync(
                    Encoding.ASCII.GetBytes("200 XZ server ready\r\n"),
                    cancellationToken
                );

                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null)
                        break;

                    if (line.StartsWith("XZVER", StringComparison.OrdinalIgnoreCase))
                    {
                        var count = ParseRangeCount(line);
                        var payload = _cache.GetOrAdd(count, BuildCompressedResponse);
                        await stream.WriteAsync(payload, cancellationToken);
                    }
                    else
                    {
                        await stream.WriteAsync(
                            Encoding.ASCII.GetBytes("500 Command not recognized\r\n"),
                            cancellationToken
                        );
                    }
                }
            }
            catch (IOException)
            {
                // Client disconnected.
            }
            catch (OperationCanceledException)
            {
                // Server shutting down.
            }
            finally
            {
                client.Close();
                client.Dispose();
            }
        }

        private static int ParseRangeCount(string command)
        {
            var dash = command.IndexOf('-', StringComparison.Ordinal);
            return dash < 0 ? 1 : int.Parse(command.AsSpan(dash + 1), CultureInfo.InvariantCulture);
        }

        private static byte[] BuildCompressedResponse(int count)
        {
            var block = new StringBuilder();
            for (var number = 1; number <= count; number++)
            {
                block.Append(
                    CultureInfo.InvariantCulture,
                    $"{number}\t[01/42] \"benchmark.bin\" yEnc (1/128)\tposter@example.com\tSat, 14 Jun 2026 12:00:00 +0000\t<{number}@benchmark>\t<parent@benchmark>\t8192\t128\r\n"
                );
            }
            block.Append(".\r\n");

            using var output = new MemoryStream();
            output.Write(
                Encoding.ASCII.GetBytes("224 Overview information follows [COMPRESS=GZIP]\r\n")
            );
            using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(Encoding.ASCII.GetBytes(block.ToString()));
            }

            return output.ToArray();
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            _listener.Stop();
            _cts.Dispose();
            _listener.Dispose();
        }
    }
}
