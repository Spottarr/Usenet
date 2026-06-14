using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Usenet.Tests.TestHelpers;

/// <summary>
/// A loopback NNTP server that answers the streamed unbounded commands (XOVER, LISTGROUP,
/// LIST ACTIVE) with a data block sized from the requested range, so streaming and drain behaviour
/// can be exercised end to end without buffering the range on the server side either.
/// </summary>
internal sealed class StreamingNntpServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public StreamingNntpServer()
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

    private static async Task HandleClientAsync(
        TcpClient client,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var stream = client.GetStream();
            await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n" };
            using var reader = new StreamReader(
                stream,
                Encoding.ASCII,
                false,
                1024,
                leaveOpen: true
            );
            writer.AutoFlush = false;

            await writer.WriteLineAsync("200 Streaming server ready");
            await writer.FlushAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                    break;

                await HandleCommandAsync(writer, line, cancellationToken);
                await writer.FlushAsync(cancellationToken);
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

    private static async Task HandleCommandAsync(
        StreamWriter writer,
        string line,
        CancellationToken cancellationToken
    )
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToUpperInvariant();

        switch (command)
        {
            case "AUTHINFO":
                await writer.WriteLineAsync("281 Authentication accepted");
                break;
            case "XOVER":
            {
                await writer.WriteLineAsync("224 Overview information follows");
                var (low, high) = ParseRange(parts.Length > 1 ? parts[1] : "1-3");
                for (var i = low; i <= high; i++)
                {
                    await writer.WriteLineAsync(
                        $"{i}\tSubject {i}\tposter@example.com\tdate\t<{i}@example.com>\t\t1024\t8"
                    );
                }

                await writer.WriteLineAsync(".");
                break;
            }
            case "LISTGROUP":
            {
                await writer.WriteLineAsync("211 3 1 3 misc.test list follows");
                for (var i = 1; i <= 3; i++)
                {
                    await writer.WriteLineAsync(i.ToString(CultureInfo.InvariantCulture));
                }

                await writer.WriteLineAsync(".");
                break;
            }
            case "LIST"
                when parts.Length > 1
                    && parts[1].Equals("ACTIVE", StringComparison.OrdinalIgnoreCase):
                await writer.WriteLineAsync("215 list of newsgroups follows");
                await writer.WriteLineAsync("misc.test 0000000003 0000000001 y");
                await writer.WriteLineAsync("alt.test 0000000009 0000000004 n");
                await writer.WriteLineAsync(".");
                break;
            case "DATE":
                await writer.WriteLineAsync("111 20260614000000");
                break;
            case "QUIT":
                await writer.WriteLineAsync("205 Goodbye");
                break;
            default:
                await writer.WriteLineAsync("500 Command not recognized");
                break;
        }
    }

    private static (long Low, long High) ParseRange(string range)
    {
        var split = range.Split('-', 2);
        _ = long.TryParse(
            split[0],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var low
        );
        var high = low;
        if (split.Length > 1)
        {
            _ = long.TryParse(
                split[1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out high
            );
        }

        return (low, high);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _listener.Stop();
        _cts.Dispose();
        _listener.Dispose();
    }
}
