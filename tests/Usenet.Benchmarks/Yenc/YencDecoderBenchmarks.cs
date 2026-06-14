using BenchmarkDotNet.Attributes;
using Usenet.Util;
using Usenet.Yenc;

namespace Usenet.Benchmarks.Yenc;

/// <summary>
/// Benchmarks the yEnc decode hot path. Both methods decode a single in-memory part so
/// the numbers reflect raw codec throughput and allocations, not stream or socket overhead.
/// </summary>
[MemoryDiagnoser]
public class YencDecoderBenchmarks
{
    private const int LineLength = 128;

    [Params(64 * 1024)]
    public int PartSize { get; set; }

    private List<string> _encodedLines = [];
    private byte[] _encodedBytes = [];

    [GlobalSetup]
    public async Task Setup()
    {
        var data = new byte[PartSize];
        // Deterministic, reasonably "binary looking" payload so the decoder hits its
        // escape-handling branches instead of trivially copying ASCII text.
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)((i * 31 + 7) & 0xFF);
        }

        var header = new YencHeader("benchmark.bin", PartSize, LineLength, 0, 1, PartSize, 0);

        using var stream = new MemoryStream(data);
        _encodedLines = [.. await YencEncoder.EncodeAsync(header, stream)];

        // The byte-input path consumes the encoded body as raw CRLF-terminated bytes,
        // exactly as it arrives off the wire.
        _encodedBytes = UsenetEncoding.Default.GetBytes(string.Join("\r\n", _encodedLines));
    }

    [Benchmark(Baseline = true)]
    public int DecodeFromLines()
    {
        var article = YencArticleDecoder.Decode(_encodedLines);
        return article.Data.Count;
    }

    [Benchmark]
    public int DecodeFromBytes()
    {
        using var part = YencDecoder.Decode(_encodedBytes);
        return part.Data.Length;
    }
}
