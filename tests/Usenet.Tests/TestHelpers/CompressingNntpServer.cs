using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Usenet.Tests.TestHelpers;

/// <summary>
/// How the server should mangle the compressed payload, to exercise the transport's error handling.
/// </summary>
internal enum CompressionFault
{
    /// <summary>Send a well-formed compressed block.</summary>
    None,

    /// <summary>Omit the terminating dot line and close, so the block ends before its terminator.</summary>
    DropTerminator,

    /// <summary>Send a payload that cannot be inflated.</summary>
    CorruptPayload,
}

/// <summary>
/// A loopback NNTP server that speaks <c>XFEATURE COMPRESS GZIP</c>: it negotiates the feature after
/// authentication and then gzip-compresses the data block of multi-line responses, so the transport's
/// inflate stage can be exercised end to end against a fabricated compressed block.
/// </summary>
internal sealed class CompressingNntpServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly bool _withTerminator;
    private readonly CompressionFault _fault;
    private int _negotiations;

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>The number of times <c>XFEATURE COMPRESS GZIP</c> was negotiated across all connections.</summary>
    public int Negotiations => Volatile.Read(ref _negotiations);

    public CompressingNntpServer(
        bool withTerminator = true,
        CompressionFault fault = CompressionFault.None
    )
    {
        _withTerminator = withTerminator;
        _fault = fault;
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

            await WriteAsciiAsync(stream, "200 Compressing server ready\r\n", cancellationToken);

            var compressed = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                    break;

                var command = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]
                    .ToUpperInvariant();

                if (command == "AUTHINFO")
                {
                    await WriteAsciiAsync(
                        stream,
                        "281 Authentication accepted\r\n",
                        cancellationToken
                    );
                }
                else if (command == "XFEATURE")
                {
                    Interlocked.Increment(ref _negotiations);
                    compressed = true;
                    await WriteAsciiAsync(stream, "290 compression enabled\r\n", cancellationToken);
                }
                else if (command is "XOVER" or "OVER")
                {
                    var range = line.Split(' ', StringSplitOptions.RemoveEmptyEntries) is [_, var r]
                        ? r
                        : "1-3";
                    await WriteOverviewAsync(stream, range, compressed, cancellationToken);
                    if (!_withTerminator || _fault == CompressionFault.DropTerminator)
                        return; // the data phase ended with the stream; the connection is spent.
                }
                else if (command == "DATE")
                {
                    await WriteAsciiAsync(stream, "111 20260101000000\r\n", cancellationToken);
                }
                else if (command == "QUIT")
                {
                    await WriteAsciiAsync(stream, "205 Goodbye\r\n", cancellationToken);
                    return;
                }
                else
                {
                    await WriteAsciiAsync(
                        stream,
                        "500 Command not recognized\r\n",
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

    private async Task WriteOverviewAsync(
        NetworkStream stream,
        string range,
        bool compressed,
        CancellationToken cancellationToken
    )
    {
        await WriteAsciiAsync(stream, "224 Overview information follows\r\n", cancellationToken);

        var (low, high) = ParseRange(range);
        var body = new StringBuilder();
        for (var i = low; i <= high; i++)
        {
            body.Append(
                CultureInfo.InvariantCulture,
                $"{i}\tSubject {i}\tposter@example.com\tdate\t<{i}@example.com>\t\t1024\t8\r\n"
            );
        }

        if (!compressed)
        {
            body.Append(".\r\n");
            await WriteAsciiAsync(stream, body.ToString(), cancellationToken);
            return;
        }

        // The non-terminator variant carries the terminating dot inside the compressed payload; the
        // terminator variant gets a literal dot line on the wire after the payload instead.
        if (!_withTerminator)
            body.Append(".\r\n");

        var payload = Gzip(Encoding.ASCII.GetBytes(body.ToString()));
        if (_fault == CompressionFault.CorruptPayload)
            Corrupt(payload);

        await stream.WriteAsync(payload, cancellationToken);

        if (_withTerminator && _fault != CompressionFault.DropTerminator)
            await WriteAsciiAsync(stream, "\r\n.\r\n", cancellationToken);

        await stream.FlushAsync(cancellationToken);
    }

    private static byte[] Gzip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(data);
        }

        return output.ToArray();
    }

    private static void Corrupt(byte[] gzip)
    {
        // Leave the gzip header intact so the inflate stage takes the gzip path, then flip the body
        // bytes so decompression fails on invalid codes.
        for (var i = 13; i < gzip.Length - 8; i++)
        {
            gzip[i] ^= 0xff;
        }
    }

    private static async Task WriteAsciiAsync(
        NetworkStream stream,
        string text,
        CancellationToken cancellationToken
    )
    {
        await stream.WriteAsync(Encoding.ASCII.GetBytes(text), cancellationToken);
        await stream.FlushAsync(cancellationToken);
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
            _ = long.TryParse(
                split[1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out high
            );

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
