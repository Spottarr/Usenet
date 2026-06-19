using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Usenet.Tests.TestHelpers;

/// <summary>
/// A loopback NNTP server that speaks <a href="https://www.rfc-editor.org/rfc/rfc8054">RFC 8054</a>
/// <c>COMPRESS DEFLATE</c>: it negotiates the feature after authentication and then carries the whole
/// session as a continuous raw-DEFLATE stream in both directions — commands the client sends and every
/// response it returns, overview and article alike. The connection stays open across commands, so the
/// transport is exercised exactly as it is against a real persistent server (a self-delimiting block
/// that never sees a socket close).
/// </summary>
internal sealed class CompressingNntpServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly bool _refuseCompression;
    private int _negotiations;

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>The number of times <c>COMPRESS DEFLATE</c> was negotiated across all connections.</summary>
    public int Negotiations => Volatile.Read(ref _negotiations);

    /// <param name="refuseCompression">
    /// When <see langword="true"/>, the server rejects <c>COMPRESS DEFLATE</c> with a <c>403</c> so the
    /// transport's fail-fast negotiation can be exercised.
    /// </param>
    public CompressingNntpServer(bool refuseCompression = false)
    {
        _refuseCompression = refuseCompression;
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
        DeflateStream? compressStream = null;
        DeflateStream? decompressStream = null;
        try
        {
            var socket = client.GetStream();

            // Reads and writes start in clear text; once COMPRESS DEFLATE is negotiated both swap to a
            // raw-DEFLATE stream over the socket, mirroring the client's transport. Commands are read a
            // byte at a time so the clear-text reader never over-reads past the COMPRESS line into the
            // first compressed command.
            Stream read = socket;
            Stream write = socket;

            await SendAsync(write, "200 Compressing server ready\r\n", cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await ReadLineAsync(read, cancellationToken);
                if (line == null)
                    break;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts.Length > 0 ? parts[0].ToUpperInvariant() : string.Empty;

                switch (command)
                {
                    case "AUTHINFO":
                        await SendAsync(
                            write,
                            "281 Authentication accepted\r\n",
                            cancellationToken
                        );
                        break;

                    case "COMPRESS":
                        if (_refuseCompression)
                        {
                            await SendAsync(
                                write,
                                "403 Compression not available\r\n",
                                cancellationToken
                            );
                            break;
                        }

                        Interlocked.Increment(ref _negotiations);
                        // The 206 is sent in clear text; compression takes effect for both directions
                        // immediately afterwards (RFC 8054 §2.2).
                        await SendAsync(write, "206 Compression active\r\n", cancellationToken);
                        compressStream = new DeflateStream(
                            socket,
                            CompressionMode.Compress,
                            leaveOpen: true
                        );
                        decompressStream = new DeflateStream(
                            socket,
                            CompressionMode.Decompress,
                            leaveOpen: true
                        );
                        write = compressStream;
                        read = decompressStream;
                        break;

                    case "XOVER":
                    case "OVER":
                        await SendAsync(
                            write,
                            BuildOverview(parts.Length > 1 ? parts[1] : "1-3"),
                            cancellationToken
                        );
                        break;

                    case "ARTICLE":
                        await SendAsync(write, BuildArticle(), cancellationToken);
                        break;

                    case "DATE":
                        await SendAsync(write, "111 20260101000000\r\n", cancellationToken);
                        break;

                    case "QUIT":
                        await SendAsync(write, "205 Goodbye\r\n", cancellationToken);
                        return;

                    default:
                        await SendAsync(write, "500 Command not recognized\r\n", cancellationToken);
                        break;
                }
            }
        }
        catch (IOException)
        {
            // Client disconnected.
        }
        catch (InvalidDataException)
        {
            // The client sent something that did not decode as DEFLATE; treat as a disconnect.
        }
        catch (OperationCanceledException)
        {
            // Server shutting down.
        }
        finally
        {
            if (compressStream != null)
                await compressStream.DisposeAsync();
            if (decompressStream != null)
                await decompressStream.DisposeAsync();
            client.Close();
            client.Dispose();
        }
    }

    private static string BuildOverview(string range)
    {
        var (low, high) = ParseRange(range);
        var body = new StringBuilder("224 Overview information follows\r\n");
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

    private static string BuildArticle() =>
        "220 1 <1@example.com>\r\n"
        + "Subject: Article subject\r\n"
        + "From: poster@example.com\r\n"
        + "\r\n"
        + "Body line one\r\n"
        + "Body line two\r\n"
        + ".\r\n";

    /// <summary>
    /// Writes an ASCII payload to the current write stream and flushes it. The flush matters once the
    /// write stream is a compressing <see cref="DeflateStream"/>: it emits a sync flush so the client
    /// can decompress the response without waiting for more input.
    /// </summary>
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

    /// <summary>
    /// Reads a single CRLF-terminated line, one byte at a time, off the current read stream. Reading
    /// byte by byte keeps the clear-text phase from buffering past the COMPRESS line, and over the
    /// decompressing stream it stops exactly at the command boundary the client flushed to.
    /// </summary>
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
