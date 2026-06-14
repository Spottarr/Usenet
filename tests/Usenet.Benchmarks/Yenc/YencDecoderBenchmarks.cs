using System.Buffers;
using BenchmarkDotNet.Attributes;
using Usenet.Yenc;

namespace Usenet.Benchmarks.Yenc;

/// <summary>
/// Benchmarks the yEnc decode hot path. The decoder consumes a single in-memory part as the
/// raw CRLF-terminated bytes it arrives as off the wire, so the numbers reflect raw codec
/// throughput and allocations, not stream or socket overhead.
/// </summary>
[MemoryDiagnoser]
public class YencDecoderBenchmarks
{
    private const int LineLength = 128;

    [Params(64 * 1024)]
    public int PartSize { get; set; }

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
        var writer = new ArrayBufferWriter<byte>(PartSize * 2);
        await YencEncoder.EncodeAsync(header, stream, writer);
        _encodedBytes = writer.WrittenSpan.ToArray();
    }

    [Benchmark]
    public int DecodeFromBytes()
    {
        using var part = YencDecoder.Decode(_encodedBytes);
        return part.Data.Length;
    }
}
