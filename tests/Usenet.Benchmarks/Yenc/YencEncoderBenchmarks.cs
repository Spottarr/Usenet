using System.Buffers;
using BenchmarkDotNet.Attributes;
using Usenet.Yenc;

namespace Usenet.Benchmarks.Yenc;

/// <summary>
/// Benchmarks the yEnc encode hot path. Both methods operate on a single in-memory
/// part so the numbers reflect raw codec throughput and allocations, not stream or
/// socket overhead.
/// </summary>
[MemoryDiagnoser]
public class YencEncoderBenchmarks
{
    private const int LineLength = 128;

    [Params(64 * 1024)]
    public int PartSize { get; set; }

    private byte[] _data = [];
    private YencHeader _header = null!;
    private ArrayBufferWriter<byte> _writer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[PartSize];
        // Deterministic, reasonably "binary looking" payload so the encoder hits
        // its escaping branches instead of trivially copying ASCII text.
        for (var i = 0; i < _data.Length; i++)
        {
            _data[i] = (byte)((i * 31 + 7) & 0xFF);
        }

        _header = new YencHeader("benchmark.bin", PartSize, LineLength, 0, 1, PartSize, 0);
        _writer = new ArrayBufferWriter<byte>(_data.Length * 2);
    }

    [Benchmark(Baseline = true)]
    public async Task<int> EncodeToLines()
    {
        using var stream = new MemoryStream(_data);
        var lines = await YencEncoder.EncodeAsync(_header, stream);
        return lines.Count;
    }

    [Benchmark]
    public async Task<int> EncodeToWriter()
    {
        using var stream = new MemoryStream(_data);
        _writer.ResetWrittenCount();
        await YencEncoder.EncodeAsync(_header, stream, _writer);
        return _writer.WrittenCount;
    }
}
