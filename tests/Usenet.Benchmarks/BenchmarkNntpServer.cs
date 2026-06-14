using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Usenet.Benchmarks;

/// <summary>
/// A minimal in-process NNTP server bound to the loopback interface. It serves
/// just enough of the protocol (<c>ARTICLE</c>, <c>HEAD</c>, <c>XOVER</c>) to
/// drive the client read/parse hot paths over a real socket without needing an
/// upstream Usenet provider.
/// </summary>
internal sealed class BenchmarkNntpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public BenchmarkNntpServer()
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
            await using var writer = new StreamWriter(stream, Encoding.ASCII);
            using var reader = new StreamReader(
                stream,
                Encoding.ASCII,
                false,
                1024,
                leaveOpen: true
            );

            writer.NewLine = "\r\n";
            writer.AutoFlush = false;

            await writer.WriteLineAsync("200 Benchmark server ready");
            await writer.FlushAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                    break;

                var parts = line.Split(
                    ' ',
                    2,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                );
                var command = parts.Length > 0 ? parts[0].ToUpperInvariant() : string.Empty;
                var argument = parts.Length > 1 ? parts[1] : string.Empty;

                switch (command)
                {
                    case "AUTHINFO":
                        await writer.WriteLineAsync("281 Success");
                        break;
                    case "MODE":
                        await writer.WriteLineAsync("201 Posting prohibited");
                        break;
                    case "GROUP":
                        await writer.WriteLineAsync("211 1000 1 1000 alt.binaries.benchmark");
                        break;
                    case "ARTICLE":
                        await WriteArticleAsync(writer, argument, includeBody: true);
                        break;
                    case "HEAD":
                        await WriteArticleAsync(writer, argument, includeBody: false);
                        break;
                    case "XOVER":
                    case "OVER":
                        await WriteOverviewAsync(writer, argument);
                        break;
                    case "QUIT":
                        await writer.WriteLineAsync("205 Goodbye");
                        await writer.FlushAsync(cancellationToken);
                        return;
                    default:
                        await writer.WriteLineAsync("500 Command not recognized");
                        break;
                }

                await writer.FlushAsync(cancellationToken);
            }
        }
        catch (IOException)
        {
            // Client disconnected; nothing to do.
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

    private static async Task WriteArticleAsync(
        StreamWriter writer,
        string argument,
        bool includeBody
    )
    {
        var number = long.TryParse(argument, CultureInfo.InvariantCulture, out var n) ? n : 1;
        var code = includeBody ? 220 : 221;

        await writer.WriteLineAsync(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{code} {number} <{number}@benchmark> article"
            )
        );

        await writer.WriteLineAsync("Path: news.example.com!not-for-mail");
        await writer.WriteLineAsync("From: \"Benchmark Poster\" <poster@example.com>");
        await writer.WriteLineAsync("Newsgroups: alt.binaries.benchmark");
        await writer.WriteLineAsync("Subject: [01/42] \"benchmark.bin\" yEnc (1/128)");
        await writer.WriteLineAsync("Date: Sat, 14 Jun 2026 12:00:00 +0000");
        await writer.WriteLineAsync(
            string.Create(CultureInfo.InvariantCulture, $"Message-ID: <{number}@benchmark>")
        );
        await writer.WriteLineAsync("Lines: 128");

        if (includeBody)
        {
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("=ybegin line=128 size=8192 name=benchmark.bin");
            for (var i = 0; i < 64; i++)
            {
                await writer.WriteLineAsync(BodyLine);
            }
            await writer.WriteLineAsync("=yend size=8192 crc32=abcdef01");
        }

        await writer.WriteLineAsync(".");
    }

    private static async Task WriteOverviewAsync(StreamWriter writer, string argument)
    {
        var (from, to) = ParseRange(argument);

        await writer.WriteLineAsync("224 Overview information follows");
        for (var number = from; number <= to; number++)
        {
            await writer.WriteLineAsync(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{number}\t[01/42] \"benchmark.bin\" yEnc (1/128)\tposter@example.com\tSat, 14 Jun 2026 12:00:00 +0000\t<{number}@benchmark>\t<parent@benchmark>\t8192\t128"
                )
            );
        }
        await writer.WriteLineAsync(".");
    }

    private static (long From, long To) ParseRange(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            return (1, 1);
        }

        var dash = argument.IndexOf('-', StringComparison.Ordinal);
        if (dash < 0)
        {
            return long.TryParse(argument, CultureInfo.InvariantCulture, out var single)
                ? (single, single)
                : (1, 1);
        }

        _ = long.TryParse(argument.AsSpan(0, dash), CultureInfo.InvariantCulture, out var from);
        _ = long.TryParse(argument.AsSpan(dash + 1), CultureInfo.InvariantCulture, out var to);
        return (Math.Max(from, 1), Math.Max(to, from));
    }

    // A representative yEnc-encoded line (128 columns) used to pad the article body.
    private const string BodyLine =
        "()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~ !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJ";

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
        _listener.Dispose();
    }
}
