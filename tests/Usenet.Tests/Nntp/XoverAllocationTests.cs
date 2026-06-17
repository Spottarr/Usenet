using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Usenet.Nntp;
using Usenet.Nntp.Parsers;
using Usenet.Tests.TestHelpers;

namespace Usenet.Tests.Nntp;

/// <summary>
/// Allocation-regression guard for the streamed <c>XOVER</c> read path (ADR-0003). It measures
/// the <em>marginal</em> managed allocation of a single overview row by reading two ranges of
/// different sizes over a loopback connection and dividing the allocation delta by the row
/// delta, so the fixed per-command overhead (command write, status line, socket and pipe setup)
/// cancels out. The loopback server replies from a pre-built byte buffer, so it does not
/// allocate per row and only the client framing/parsing is measured.
/// </summary>
internal sealed class XoverAllocationTests
{
    private const int SmallRange = 200;
    private const int LargeRange = 2_200;

    // A framed overview row is ~130 bytes of text, so the dominant per-row managed allocation is
    // the decoded string plus its slot in the materialized list. The ceiling leaves headroom for
    // runtime variation but trips if the per-row cost grows (e.g. extra copies or boxing).
    private const long MaxBytesPerRow = 896;

    private static NntpConnectionOptions LoopbackOptions(int port) =>
        new() { Host = IPAddress.Loopback.ToString(), Port = port };

    [Test]
    internal async Task StreamedOverviewRowShouldStayUnderAllocationCeiling(
        CancellationToken cancellationToken
    )
    {
        await using var server = new OverviewNntpServer();
        using var connection = new NntpConnection(LoopbackOptions(server.Port));

        await connection.ConnectAsync(new ResponseParser(200), cancellationToken);

        // Warm up both ranges so cached server buffers and JIT costs stay out of the window.
        await ReadOverviewAsync(connection, SmallRange, cancellationToken);
        await ReadOverviewAsync(connection, LargeRange, cancellationToken);

        // Take the lowest marginal cost over a few repetitions to filter transient background
        // noise from the runtime and the loopback server thread.
        var perRow = long.MaxValue;
        for (var repeat = 0; repeat < 5; repeat++)
        {
            var smallBytes = await AllocationMeasurement.TotalAsync(() =>
                ReadOverviewAsync(connection, SmallRange, cancellationToken)
            );
            var largeBytes = await AllocationMeasurement.TotalAsync(() =>
                ReadOverviewAsync(connection, LargeRange, cancellationToken)
            );

            var marginal = (largeBytes - smallBytes) / (LargeRange - SmallRange);
            perRow = Math.Min(perRow, marginal);
        }

        await Assert.That(perRow).IsLessThanOrEqualTo(MaxBytesPerRow);
    }

    private static async Task ReadOverviewAsync(
        NntpConnection connection,
        int count,
        CancellationToken cancellationToken
    )
    {
        var response = await connection.MultiLineCommandAsync(
            string.Create(CultureInfo.InvariantCulture, $"XOVER 1-{count}"),
            new TextResponseParser(224),
            cancellationToken
        );

        if (response.Lines.Count != count)
        {
            throw new InvalidOperationException(
                $"Expected {count} overview rows, got {response.Lines.Count}."
            );
        }
    }

    /// <summary>
    /// A minimal loopback NNTP server that replies to <c>XOVER from-to</c> with a pre-built,
    /// cached byte buffer so it allocates nothing per row while the client read is measured.
    /// </summary>
    private sealed class OverviewNntpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<int, byte[]> _cache = new();

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public OverviewNntpServer()
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
                    Encoding.ASCII.GetBytes("200 Overview server ready\r\n"),
                    cancellationToken
                );

                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null)
                        break;

                    if (line.StartsWith("XOVER", StringComparison.OrdinalIgnoreCase))
                    {
                        var count = ParseRangeCount(line);
                        var payload = _cache.GetOrAdd(count, BuildOverview);
                        await stream.WriteAsync(payload, cancellationToken);
                    }
                    else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                    {
                        await stream.WriteAsync(
                            Encoding.ASCII.GetBytes("205 Goodbye\r\n"),
                            cancellationToken
                        );
                        return;
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

        private static byte[] BuildOverview(int count)
        {
            var builder = new StringBuilder("224 Overview information follows\r\n");
            for (var number = 1; number <= count; number++)
            {
                builder.Append(
                    CultureInfo.InvariantCulture,
                    $"{number}\t[01/42] \"benchmark.bin\" yEnc (1/128)\tposter@example.com\tSat, 14 Jun 2026 12:00:00 +0000\t<{number}@benchmark>\t<parent@benchmark>\t8192\t128\r\n"
                );
            }
            builder.Append(".\r\n");
            return Encoding.ASCII.GetBytes(builder.ToString());
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
