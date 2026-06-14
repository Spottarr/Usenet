# Usenet.Benchmarks

A [BenchmarkDotNet](https://benchmarkdotnet.org/) harness that captures an authoritative
performance and memory baseline for the library's hot paths **before** the 6.0.0
streaming/buffering rebuild begins. Later slices can re-run these benchmarks to show honest
before/after deltas.

All benchmarks use `[MemoryDiagnoser]`, so allocations are reported alongside timings.

## Covered hot paths

| Benchmark                            | Hot path                                                                  |
| ------------------------------------ | ------------------------------------------------------------------------- |
| `YencDecoderBenchmarks.DecodeFromLines` | yEnc decode of a single in-memory part (`YencArticleDecoder.Decode`)   |
| `YencDecoderBenchmarks.DecodeFromBytes` | yEnc byte-input decode of a single part into pooled Data (`YencDecoder.Decode`) |
| `YencEncoderBenchmarks.EncodeToLines`   | yEnc encode of a single in-memory part (`YencEncoder.EncodeAsync`)      |
| `YencEncoderBenchmarks.EncodeToWriter`  | yEnc encode of a part into an `IBufferWriter<byte>` sink (block reads)  |
| `HeaderParseBenchmarks.ParseHeaders` | Parsing one NNTP article header block (`HEAD`), with a folded header line |
| `NntpBenchmarks.ArticleRead`         | A single-article read over a loopback socket (`ARTICLE`)                   |
| `NntpBenchmarks.XoverRange`          | A streamed `XOVER` range over a loopback socket                           |

`NntpBenchmarks` drives a real socket served in-process by `BenchmarkNntpServer`, so its
numbers include socket reads, the dot-unstuffing `NntpStreamReader`, and response parsing.

## Running

The project multi-targets `net8.0;net10.0` in line with the library. The baseline below was
captured on `net10.0`:

```bash
# All benchmarks
dotnet run -c Release -f net10.0 --project tests/Usenet.Benchmarks -- --filter '*'

# A single class
dotnet run -c Release -f net10.0 --project tests/Usenet.Benchmarks -- --filter '*Yenc*'
```

> The harness is intentionally **excluded from `Usenet.slnx`** so the normal CI build/test
> run (which operates on the solution) never picks it up — BenchmarkDotNet is slow and noisy
> on shared runners. Run it locally when you need numbers.

## Baseline

Captured on `main` (`net10.0`) with the BenchmarkDotNet `ShortRun` job
(`--job short`: 3 warmup + 3 measured iterations) on a loopback ARM64 Linux host. These are
a directional baseline for tracking deltas — re-run on a quiet machine with the default job
for publication-grade numbers.

```
BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9, Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.9, Arm64 RyuJIT armv8.0-a
Job=ShortRun  IterationCount=3  LaunchCount=1  WarmupCount=3
```

### yEnc codec (64 KiB part)

| Method          | PartSize | Mean      | Allocated |
| --------------- | -------- | --------: | --------: |
| EncodeToLines   | 65536    | 577.30 us | 160.49 KB |
| EncodeToWriter  | 65536    | 179.6 us  |    920 B  |
| DecodeFromLines | 65536    |  64.76 us | 143.29 KB |
| DecodeFromBytes | 65536    | 178.87 us |   2.19 KB |

`EncodeToWriter` streams the part into a reused `IBufferWriter<byte>` via block reads and a
precomputed escape table, replacing the per-byte `ReadAsync` and the `List<string>` of all
lines. The `EncodeToLines` row is the back-compat text adapter over the same byte sink.

`DecodeFromBytes` is the byte-input path added in 6.0.0: it decodes straight from the body bytes
into a single pooled `Data` buffer, dropping per-part allocations from ~143 KB to ~2 KB
(the decoded buffer is rented from `ArrayPool`, so it does not show as a managed allocation).
It does more CPU work per call than the string-based `DecodeFromLines` because it also verifies
the per-part `pcrc32` in the same pass, which the older decoder skips entirely.

### NNTP header parse (single article header block)

| Method       | Mean     | Allocated |
| ------------ | -------: | --------: |
| ParseHeaders | 2.629 us |   9.36 KB |

### NNTP read over loopback

| Method      | OverviewCount | Mean     | Allocated |
| ----------- | ------------- | -------: | --------: |
| ArticleRead | 1000          | 43.07 ms |  50.69 KB |
| XoverRange  | 1000          | 24.26 ms | 845.97 KB |
