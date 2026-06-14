using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Usenet.Tests.TestHelpers;

internal sealed class TestNntpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly TaskCompletionSource<IReadOnlyList<string>> _postedArticle = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>
    /// Completes with the data block (dot-unstuffed, without the terminating dot) of the first
    /// article posted via POST or IHAVE.
    /// </summary>
    public Task<IReadOnlyList<string>> PostedArticle => _postedArticle.Task;

    public TestNntpServer()
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
            await using var writer = new StreamWriter(stream, Encoding.ASCII);
            using var reader = new StreamReader(
                stream,
                Encoding.ASCII,
                false,
                1024,
                leaveOpen: true
            );

            writer.NewLine = "\r\n";
            writer.AutoFlush = true;

            // Send NNTP greeting
            await writer.WriteLineAsync("200 Dummy server ready");
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                    break;

                var (command, _, _) = ParseCommand(line);
                if (command == null)
                    continue;

                switch (command)
                {
                    case "AUTHINFO":
                        await writer.WriteLineAsync("281 Success");
                        break;
                    case "POST":
                        await writer.WriteLineAsync("340 Send article");
                        await ReceiveArticleAsync(reader, cancellationToken);
                        await writer.WriteLineAsync("240 Article received");
                        break;
                    case "IHAVE":
                        await writer.WriteLineAsync("335 Send article");
                        await ReceiveArticleAsync(reader, cancellationToken);
                        await writer.WriteLineAsync("235 Article transferred");
                        break;
                    case "GROUP":
                        // Group command is used to force immediate connection reset
                        // so next write on client side breaks.
                        client.Client.LingerState = new LingerOption(true, 0);
                        return;
                    case "QUIT":
                        await writer.WriteLineAsync("205 Goodbye");
                        return;
                    default:
                        await writer.WriteLineAsync("500 Command not recognized");
                        break;
                }
            }
        }
        finally
        {
            client.Close();
            client.Dispose();
        }
    }

    private async Task ReceiveArticleAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var lines = new List<string>();
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null || line == ".")
            {
                break;
            }

            // undo dot-stuffing
            lines.Add(line.StartsWith("..", StringComparison.Ordinal) ? line[1..] : line);
        }

        _postedArticle.TrySetResult(lines);
    }

    [SuppressMessage("ReSharper", "UnusedTupleComponentInReturnValue")]
    private static (string? Command, string? SubCommand, string? Arguments) ParseCommand(
        string line
    )
    {
        var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => (null, null, null),
            1 => (parts[0].ToUpperInvariant(), null, null),
            2 => (parts[0].ToUpperInvariant(), null, parts[1]),
            3 => (parts[0].ToUpperInvariant(), parts[1].ToUpperInvariant(), parts[2]),
            _ => (null, null, null),
        };
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
        _listener.Dispose();
    }
}
