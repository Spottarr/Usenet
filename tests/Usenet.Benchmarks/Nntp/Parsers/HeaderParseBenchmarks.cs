using System.Buffers;
using BenchmarkDotNet.Attributes;
using Usenet.Nntp.Parsers;
using Usenet.Util;

namespace Usenet.Benchmarks.Nntp.Parsers;

/// <summary>
/// Benchmarks parsing of a single NNTP article header block (as returned by
/// <c>HEAD</c>). This isolates the header-folding and dictionary-building cost
/// from any network I/O. The header block is supplied as the contiguous,
/// CRLF-terminated byte buffer the connection materializes for the byte-oriented
/// read path.
/// </summary>
[MemoryDiagnoser]
public class HeaderParseBenchmarks
{
    private const string Message = "123 <message-id@benchmark> head";

    private readonly ArticleResponseParser _parser = new(ArticleRequestType.Head);
    private byte[] _buffer = [];
    private int _length;

    [GlobalSetup]
    public void Setup()
    {
        string[] headerLines =
        [
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

        _buffer = UsenetEncoding.Default.GetBytes(string.Join("\r\n", headerLines) + "\r\n");
        _length = _buffer.Length;
    }

    [Benchmark]
    public int ParseHeaders()
    {
        // The parser takes ownership of the buffer, so hand it a fresh pooled copy each iteration and
        // dispose the response to return that copy to the pool.
        var buffer = ArrayPool<byte>.Shared.Rent(_length);
        _buffer.AsSpan(0, _length).CopyTo(buffer);
        using var response = _parser.Parse(221, Message, buffer, _length);
        return response.Headers.Count;
    }
}
