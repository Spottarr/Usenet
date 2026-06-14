using System.Net;
using System.Net.Sockets;
using System.Text;
using Usenet.Nntp;
using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp;

internal sealed class NntpConnectionTests
{
    private static readonly string[] ExpectedDataBlock =
    [
        "Subject: test",
        "",
        ".dot-stuffed line",
        "last body line",
    ];

    [Test]
    public async Task ShouldFrameSingleLineResponse(CancellationToken cancellationToken)
    {
        await using var server = new ScriptedNntpServer();
        using var connection = new NntpConnection();

        var response = await connection.ConnectAsync(
            IPAddress.Loopback.ToString(),
            server.Port,
            false,
            new ResponseParser(200),
            cancellationToken
        );

        await Assert.That(response.Code).IsEqualTo(200);
        await Assert.That(response.Message).IsEqualTo("Scripted server ready");
    }

    [Test]
    public async Task ShouldFrameMultiLineBlockAndUndoDotStuffing(
        CancellationToken cancellationToken
    )
    {
        await using var server = new ScriptedNntpServer();
        using var connection = new NntpConnection();

        await connection.ConnectAsync(
            IPAddress.Loopback.ToString(),
            server.Port,
            false,
            new ResponseParser(200),
            cancellationToken
        );

        var lines = await connection.MultiLineCommandAsync(
            "ARTICLE 1",
            new CollectingParser(),
            cancellationToken
        );

        await Assert.That(lines).IsEquivalentTo(ExpectedDataBlock);
    }

    [Test]
    public async Task ShouldCountBytesReadAndWritten(CancellationToken cancellationToken)
    {
        await using var server = new ScriptedNntpServer();
        using var connection = new NntpConnection();

        await connection.ConnectAsync(
            IPAddress.Loopback.ToString(),
            server.Port,
            false,
            new ResponseParser(200),
            cancellationToken
        );

        await connection.MultiLineCommandAsync(
            "ARTICLE 1",
            new CollectingParser(),
            cancellationToken
        );

        await Assert.That(connection.BytesRead).IsGreaterThan(0);
        await Assert.That(connection.BytesWritten).IsGreaterThan(0);

        connection.ResetCounters();

        await Assert.That(connection.BytesRead).IsEqualTo(0);
        await Assert.That(connection.BytesWritten).IsEqualTo(0);
    }

    [Test]
    public async Task ShouldStreamMultiLineBlockPerLine(CancellationToken cancellationToken)
    {
        await using var server = new ScriptedNntpServer();
        using var connection = new NntpConnection();

        await connection.ConnectAsync(
            IPAddress.Loopback.ToString(),
            server.Port,
            false,
            new ResponseParser(200),
            cancellationToken
        );

        await using var response = await connection.MultiLineStreamCommandAsync<string>(
            "XOVER 1-3",
            224,
            Identity,
            cancellationToken
        );

        await Assert.That(response.Success).IsTrue();
        await Assert.That(response.Code).IsEqualTo(224);

        var lines = new List<string>();
        await foreach (var item in response.WithCancellation(cancellationToken))
        {
            lines.Add(item);
        }

        await Assert.That(lines).IsEquivalentTo(["1\tsubject 1", "2\tsubject 2", "3\tsubject 3"]);
    }

    [Test]
    public async Task ShouldDrainPartiallyConsumedStreamForReuse(
        CancellationToken cancellationToken
    )
    {
        await using var server = new ScriptedNntpServer();
        using var connection = new NntpConnection();

        await connection.ConnectAsync(
            IPAddress.Loopback.ToString(),
            server.Port,
            false,
            new ResponseParser(200),
            cancellationToken
        );

        // Consume only the first line, then dispose without enumerating the rest.
        await using (
            var response = await connection.MultiLineStreamCommandAsync<string>(
                "XOVER 1-3",
                224,
                Identity,
                cancellationToken
            )
        )
        {
            await foreach (var item in response.WithCancellation(cancellationToken))
            {
                await Assert.That(item).IsEqualTo("1\tsubject 1");
                break;
            }
        }

        // The connection must be clean: a subsequent command round-trips correctly.
        await using var second = await connection.MultiLineStreamCommandAsync<string>(
            "XOVER 1-3",
            224,
            Identity,
            cancellationToken
        );

        var count = 0;
        await foreach (var _ in second.WithCancellation(cancellationToken))
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(3);
    }

    private static bool Identity(string line, out string value)
    {
        value = line;
        return true;
    }

    private sealed class CollectingParser : IMultiLineResponseParser<IReadOnlyList<string>>
    {
        public bool IsSuccessResponse(int code) => code is >= 200 and < 300;

        public IReadOnlyList<string> Parse(
            int code,
            string message,
            IEnumerable<string> dataBlock
        ) => dataBlock.ToList();
    }

    private sealed class ScriptedNntpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public ScriptedNntpServer()
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
                await using var writer = new StreamWriter(stream, Encoding.ASCII)
                {
                    NewLine = "\r\n",
                };
                using var reader = new StreamReader(
                    stream,
                    Encoding.ASCII,
                    false,
                    1024,
                    leaveOpen: true
                );
                writer.AutoFlush = true;

                await writer.WriteLineAsync("200 Scripted server ready");

                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null)
                        break;

                    if (line.StartsWith("ARTICLE", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("220 1 <1@scripted> article");
                        await writer.WriteLineAsync("Subject: test");
                        await writer.WriteLineAsync("");
                        await writer.WriteLineAsync("..dot-stuffed line");
                        await writer.WriteLineAsync("last body line");
                        await writer.WriteLineAsync(".");
                    }
                    else if (line.StartsWith("XOVER", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("224 overview follows");
                        for (var i = 1; i <= 3; i++)
                        {
                            await writer.WriteLineAsync($"{i}\tsubject {i}");
                        }
                        await writer.WriteLineAsync(".");
                    }
                    else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("205 Goodbye");
                        return;
                    }
                    else
                    {
                        await writer.WriteLineAsync("500 Command not recognized");
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

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            _listener.Stop();
            _cts.Dispose();
            _listener.Dispose();
        }
    }
}
