using BenchmarkDotNet.Attributes;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;

namespace Usenet.Benchmarks;

/// <summary>
/// Benchmarks parsing of a single NNTP article header block (as returned by
/// <c>HEAD</c>). This isolates the header-folding and dictionary-building cost
/// from any network I/O.
/// </summary>
[MemoryDiagnoser]
public class HeaderParseBenchmarks
{
    private const string Message = "123 <message-id@benchmark> head";

    private readonly ArticleResponseParser _parser = new(ArticleRequestType.Head);
    private List<string> _headerBlock = [];

    [GlobalSetup]
    public void Setup() =>
        _headerBlock = [
            "Path: news.example.com!not-for-mail",
            "From: \"Benchmark Poster\" <poster@example.com>",
            "Newsgroups: alt.binaries.benchmark,alt.binaries.test",
            "Subject: [01/42] \"benchmark.bin\" yEnc (1/128)",
            "Date: Sat, 14 Jun 2026 12:00:00 +0000",
            "Message-ID: <message-id@benchmark>",
            "References: <parent-1@benchmark> <parent-2@benchmark>",
            "X-Newsreader: Usenet.Benchmarks",
            "Organization: Example",
            "Lines: 1024",
            // Folded continuation line to exercise the whitespace-continuation path.
            "X-Long-Header: part-one",
            "\tpart-two-folded",
        ];

    [Benchmark]
    public NntpArticleResponse ParseHeaders() => _parser.Parse(221, Message, _headerBlock);
}
