using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Usenet.Tests.TestHelpers;

/// <summary>
/// The compressed-overview codec a <see cref="XzCompressingNntpServer"/> emits for its
/// <c>XZVER</c>/<c>XZHDR</c> data block. Highwinds-family servers label the block <c>GZIP</c> but in
/// practice ship <see cref="ZLib"/>; the other members let the header-sniffing decoder be exercised
/// against the formats it must also accept. See ADR-0006.
/// </summary>
internal enum XzCodec
{
    ZLib,
    GZip,
    Deflate,
}

/// <summary>
/// A loopback NNTP server that answers <c>XZVER</c>/<c>XZHDR</c> with a clear-text status line followed
/// by a single compressed data block whose <c>.</c> terminator lives <em>inside</em> the compressed
/// member, exactly as eweka does. The connection stays open across commands, so the per-command
/// decompression scope is exercised against a persistent connection that never sees a socket close.
/// The codec is configurable so the same fixture can drive the zlib, gzip and raw-DEFLATE arms of the
/// header-sniffing decoder.
/// </summary>
internal sealed class XzCompressingNntpServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly XzCodec _codec;

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public XzCompressingNntpServer(XzCodec codec = XzCodec.ZLib)
    {
        _codec = codec;
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
            var socket = client.GetStream();
            await SendAsync(socket, "200 XZ server ready\r\n", cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await ReadLineAsync(socket, cancellationToken);
                if (line == null)
                    break;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts.Length > 0 ? parts[0].ToUpperInvariant() : string.Empty;

                switch (command)
                {
                    case "AUTHINFO":
                        await SendAsync(
                            socket,
                            "281 Authentication accepted\r\n",
                            cancellationToken
                        );
                        break;

                    case "XZVER":
                        await SendXzAsync(
                            socket,
                            "224 Overview information follows [COMPRESS=GZIP]\r\n",
                            BuildOverviewBlock(parts.Length > 1 ? parts[1] : "1-3"),
                            cancellationToken
                        );
                        break;

                    case "XZHDR":
                        await SendXzAsync(
                            socket,
                            "221 Header follows [COMPRESS=GZIP]\r\n",
                            BuildHeaderBlock(parts.Length > 2 ? parts[2] : "1-3"),
                            cancellationToken
                        );
                        break;

                    case "XOVER":
                    case "OVER":
                        await SendAsync(
                            socket,
                            "224 Overview information follows\r\n"
                                + BuildOverviewBlock(parts.Length > 1 ? parts[1] : "1-3"),
                            cancellationToken
                        );
                        break;

                    case "DATE":
                        await SendAsync(socket, "111 20260101000000\r\n", cancellationToken);
                        break;

                    case "QUIT":
                        await SendAsync(socket, "205 Goodbye\r\n", cancellationToken);
                        return;

                    default:
                        await SendAsync(
                            socket,
                            "500 Command not recognized\r\n",
                            cancellationToken
                        );
                        break;
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

    /// <summary>The decompressed bytes of the overview data block, terminator included.</summary>
    internal static string BuildOverviewBlock(string range)
    {
        var (low, high) = ParseRange(range);
        var body = new StringBuilder();
        for (var i = low; i <= high; i++)
        {
            body.Append(
                CultureInfo.InvariantCulture,
                $"{i}\tSubject {i}\tposter@example.com\tdate\t<{i}@example.com>\t\t1024\t8\r\n"
            );
        }

        body.Append(".\r\n");
        return body.ToString();
    }

    private static string BuildHeaderBlock(string range)
    {
        var (low, high) = ParseRange(range);
        var body = new StringBuilder();
        for (var i = low; i <= high; i++)
        {
            body.Append(CultureInfo.InvariantCulture, $"{i} Subject {i}\r\n");
        }

        body.Append(".\r\n");
        return body.ToString();
    }

    /// <summary>Compresses <paramref name="block"/> into a single member using the configured codec.</summary>
    internal byte[] Compress(string block)
    {
        var plain = Encoding.ASCII.GetBytes(block);
        using var output = new MemoryStream();
        using (Stream compressor = NewCompressor(output))
        {
            compressor.Write(plain);
        }

        return output.ToArray();
    }

    private Stream NewCompressor(Stream output) =>
        _codec switch
        {
            XzCodec.ZLib => new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true),
            XzCodec.GZip => new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true),
            _ => new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true),
        };

    private async Task SendXzAsync(
        Stream socket,
        string statusLine,
        string decompressedBlock,
        CancellationToken cancellationToken
    )
    {
        await SendAsync(socket, statusLine, cancellationToken);
        await socket.WriteAsync(Compress(decompressedBlock), cancellationToken);
        await socket.FlushAsync(cancellationToken);
    }

    private static async Task SendAsync(
        Stream stream,
        string text,
        CancellationToken cancellationToken
    )
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<string?> ReadLineAsync(
        Stream stream,
        CancellationToken cancellationToken
    )
    {
        var builder = new StringBuilder();
        var one = new byte[1];
        while (true)
        {
            var read = await stream.ReadAsync(one.AsMemory(0, 1), cancellationToken);
            if (read == 0)
                return builder.Length == 0 ? null : builder.ToString();

            var c = (char)one[0];
            if (c == '\n')
            {
                if (builder.Length > 0 && builder[^1] == '\r')
                    builder.Length--;
                return builder.ToString();
            }

            builder.Append(c);
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
