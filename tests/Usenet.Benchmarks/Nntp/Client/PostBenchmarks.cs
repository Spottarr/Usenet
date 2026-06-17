using BenchmarkDotNet.Attributes;
using Usenet.Benchmarks.Helpers;
using Usenet.Nntp;
using Usenet.Nntp.Builders;
using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;

namespace Usenet.Benchmarks.Nntp.Client;

/// <summary>
/// End-to-end NNTP write/post benchmarks over a loopback socket served by
/// <see cref="BenchmarkNntpServer"/>. The baseline flushes the pipe once per line (the
/// pre-rebuild behaviour), while <see cref="PostBatched"/> buffers the whole article and
/// flushes a single time per command, so the numbers contrast the flush count and
/// allocations of a multi-line post.
/// </summary>
[MemoryDiagnoser]
public class PostBenchmarks
{
    [Params(500)]
    public int BodyLines { get; set; }

    private BenchmarkNntpServer _server = null!;
    private NntpConnection _connection = null!;
    private NntpClient _client = null!;
    private NntpArticle _article = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _server = new BenchmarkNntpServer();
        _connection = new NntpConnection(
            new NntpConnectionOptions { Host = "127.0.0.1", Port = _server.Port }
        );
        _client = new NntpClient(_connection);
        await _client.ConnectAsync();

        var builder = new NntpArticleBuilder()
            .SetMessageId("benchmark@example.com")
            .SetFrom("\"Benchmark Poster\" <poster@example.com>")
            .SetSubject("[01/42] \"benchmark.bin\" yEnc (1/128)")
            .AddGroups("alt.binaries.benchmark");

        for (var i = 0; i < BodyLines; i++)
        {
            builder.AddLine(BodyLine);
        }

        _article = builder.Build();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _client.QuitAsync();
        _connection.Dispose();
        _server.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task<bool> PostPerLineFlush()
    {
        var initial = await _connection.CommandAsync("POST", new ResponseParser(340));
        if (!initial.Success)
        {
            return false;
        }

        // Replicates the pre-rebuild write path: one flush per line.
        await _connection.WriteLineAsync($"Message-ID: <{_article.MessageId}>");
        await _connection.WriteLineAsync($"Newsgroups: {_article.Groups}");
        foreach (var header in _article.Headers)
        {
            foreach (var value in header.Value)
            {
                await _connection.WriteLineAsync($"{header.Key}: {value}");
            }
        }

        await _connection.WriteLineAsync(string.Empty);
        foreach (var line in _article.Body)
        {
            await _connection.WriteLineAsync(line);
        }

        await _connection.WriteLineAsync(".");
        var response = await _connection.GetResponseAsync(new ResponseParser(240));
        return response.Success;
    }

    [Benchmark]
    public Task<bool> PostBatched() => _client.PostAsync(_article);

    // A representative yEnc-encoded line (128 columns) used to pad the article body.
    private const string BodyLine =
        "()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~ !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJ";
}
