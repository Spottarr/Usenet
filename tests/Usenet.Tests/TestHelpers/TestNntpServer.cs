using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Usenet.Tests.TestHelpers;

internal sealed class TestNntpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

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
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
        }
    }

    private static async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
#pragma warning disable CA2007
            var stream = client.GetStream();
            await using var writer = new StreamWriter(stream, Encoding.ASCII);
            using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
#pragma warning restore CA2007
            writer.NewLine = "\r\n";
            writer.AutoFlush = true;

            // Send NNTP greeting
            await writer.WriteLineAsync("200 Dummy server ready").ConfigureAwait(false);
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null) break;

                var (command, _, _) = ParseCommand(line);
                if (command == null) continue;

                switch (command)
                {
                    case "AUTHINFO":
                        await writer.WriteLineAsync("281 Success").ConfigureAwait(false);
                        break;
                    case "GROUP":
                        // Group command is used to force immediate connection reset
                        // so next write on client side breaks.
                        client.Client.LingerState = new LingerOption(true, 0);
                        return;
                    case "QUIT":
                        await writer.WriteLineAsync("205 Goodbye").ConfigureAwait(false);
                        return;
                    default:
                        await writer.WriteLineAsync("500 Command not recognized").ConfigureAwait(false);
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

    private static (string? Command, string? SubCommand, string? Arguments) ParseCommand(string line)
    {
        var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => (null, null, null),
            1 => (parts[0].ToUpperInvariant(), null, null),
            2 => (parts[0].ToUpperInvariant(), null, parts[1]),
            3 => (parts[0].ToUpperInvariant(), parts[1].ToUpperInvariant(), parts[2]),
            _ => (null, null, null)
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
