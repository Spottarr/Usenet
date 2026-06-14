using System.Buffers;
using Usenet.Nntp.Parsers;
using Usenet.Tests.TestHelpers;
using Usenet.Util;

namespace Usenet.Tests.Nntp.Parsers;

/// <summary>
/// Allocation-regression guard for parsing a single article header block (as returned by
/// <c>HEAD</c>). This isolates the header-folding and collection-building cost from any
/// network I/O, so a regression in the parse path shows up as extra per-parse allocation.
/// The working buffer is rented from the pool (ADR-0002) and is not counted by the
/// measurement, so the bytes measured are the per-parse object/string churn.
/// </summary>
internal sealed class ArticleResponseParserAllocationTests
{
    private const string Message = "123 <message-id@benchmark> head";

    // A representative folded header block, matching the benchmark fixture.
    private static readonly string[] HeaderBlock =
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

    // Parsing this twelve-line block decodes each header line off the pooled buffer and folds the
    // continuation onto its predecessor, producing the per-line key/value strings, the flat
    // key/value list wrapped in an NntpHeaderCollection, the groups and the response object. The
    // pooled working buffer is not counted (ADR-0002). The ceiling sits ~50% above the measured
    // cost for runtime variation while still tripping if the parse regresses (e.g. extra copies of
    // the header block or repeated splits).
    private const long MaxBytesPerParse = 14_336;
    private const int Iterations = 200;

    [Test]
    internal async Task ParseShouldStayUnderAllocationCeiling()
    {
        var parser = new ArticleResponseParser(ArticleRequestType.Head);

        // Build the contiguous, CRLF-terminated data block once; each parse copies it into a freshly
        // rented buffer and the response returns that buffer to the pool when disposed.
        var block = UsenetEncoding.Default.GetBytes(string.Join("\r\n", HeaderBlock) + "\r\n");

        var perParse = AllocationMeasurement.PerIteration(
            () =>
            {
                var buffer = ArrayPool<byte>.Shared.Rent(block.Length);
                block.CopyTo(buffer, 0);
                using var response = parser.Parse(221, Message, buffer, block.Length);
            },
            Iterations
        );

        await Assert.That(perParse).IsLessThanOrEqualTo(MaxBytesPerParse);
    }
}
