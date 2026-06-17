using BenchmarkDotNet.Attributes;
using Usenet.Benchmarks.Helpers;
using Usenet.Nntp;
using Usenet.Nntp.Models;

namespace Usenet.Benchmarks.Nntp.Client;

/// <summary>
/// End-to-end NNTP read benchmarks over a loopback socket served by
/// <see cref="BenchmarkNntpServer"/>. These cover a single-article read and a
/// streamed <c>XOVER</c> range, so the numbers include socket reads, the
/// pipe-based line framing and dot-unstuffing, and response parsing.
/// </summary>
[MemoryDiagnoser]
public class NntpBenchmarks
{
    [Params(1000)]
    public int OverviewCount { get; set; }

    private BenchmarkNntpServer _server = null!;
    private NntpConnection _connection = null!;
    private NntpClient _client = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _server = new BenchmarkNntpServer();
        _connection = new NntpConnection();
        _client = new NntpClient(_connection);
        await _client.ConnectAsync("127.0.0.1", _server.Port, false);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _client.QuitAsync();
        _connection.Dispose();
        _server.Dispose();
    }

    [Benchmark]
    public async Task<int> ArticleRead()
    {
        using var response = await _client.ArticleByNumberAsync(123);
        return response.Body.Length;
    }

    [Benchmark]
    public async Task<int> XoverRange()
    {
        var count = 0;
        await using var response = await _client.XoverAsync(
            NntpArticleRange.Range(1, OverviewCount)
        );
        await foreach (var _ in response)
        {
            count++;
        }

        return count;
    }
}
