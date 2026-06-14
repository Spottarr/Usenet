using Usenet.Nntp.Parsers;
using Usenet.Tests.TestHelpers;

namespace Usenet.Tests.Nntp.Parsers;

/// <summary>
/// Allocation-regression guard for parsing a single article header block (as returned by
/// <c>HEAD</c>). This isolates the header-folding and dictionary-building cost from any
/// network I/O, so a regression in the parse path shows up as extra per-parse allocation.
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

    // Parsing this twelve-line block allocates ~9.6 KB: the header records and folded-value
    // strings, the backing dictionary and the article/groups objects. The ceiling sits ~50%
    // above the measured cost for runtime variation while still tripping if the parse regresses
    // (e.g. extra copies of the header block or repeated splits).
    private const long MaxBytesPerParse = 14_336;
    private const int Iterations = 200;

    [Test]
    internal async Task ParseShouldStayUnderAllocationCeiling()
    {
        var parser = new ArticleResponseParser(ArticleRequestType.Head);

        var perParse = AllocationMeasurement.PerIteration(
            () => parser.Parse(221, Message, HeaderBlock),
            Iterations
        );

        await Assert.That(perParse).IsLessThanOrEqualTo(MaxBytesPerParse);
    }
}
