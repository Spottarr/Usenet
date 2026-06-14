# Usenet.Benchmarks

A [BenchmarkDotNet](https://benchmarkdotnet.org/) harness that captures an authoritative
performance and memory baseline for the library's hot paths **before** the 6.0.0
streaming/buffering rebuild begins. Later slices can re-run these benchmarks to show honest
before/after deltas.

All benchmarks use `[MemoryDiagnoser]`, so allocations are reported alongside timings.

## Covered hot paths

| Benchmark                            | Hot path                                                                  |
| ------------------------------------ | ------------------------------------------------------------------------- |
| `YencBenchmarks.Decode`              | yEnc decode of a single in-memory part (`YencArticleDecoder.Decode`)      |
| `YencBenchmarks.Encode`              | yEnc encode of a single in-memory part (`YencEncoder.EncodeAsync`)        |
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

| Method | PartSize | Mean      | Allocated |
| ------ | -------- | --------: | --------: |
| Encode | 65536    | 577.30 us | 160.49 KB |
| Decode | 65536    |  64.76 us | 143.29 KB |

### NNTP header parse (single article header block)

| Method       | Mean     | Allocated |
| ------------ | -------: | --------: |
| ParseHeaders | 2.629 us |   9.36 KB |

### NNTP read over loopback

| Method      | OverviewCount | Mean     | Allocated |
| ----------- | ------------- | -------: | --------: |
| ArticleRead | 1000          | 43.07 ms |  50.69 KB |
| XoverRange  | 1000          | 24.26 ms | 845.97 KB |
