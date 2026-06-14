# Byte-oriented article bodies with pooled, caller-owned buffers

## Status

accepted (6.0.0)

## Decision

In the 6.0.0 breaking release the article-read path becomes **byte-oriented**. The
transport reads each article's data block as bytes via `System.IO.Pipelines`, undoing
dot-stuffing and detecting the terminating `.` line without transcoding to `string`. A
body's canonical representation is a single **contiguous buffer rented from a pool**,
materialized whole per article (one part = one article, bounded ~≤1 MB), and exposed as
`ReadOnlyMemory<byte>`. The owning response is `IDisposable`/`IAsyncDisposable`; the byte
and text views are valid only until it is disposed. Plain-text consumers get a
lines-as-text accessor on demand; `YencDecoder` gains a byte-input overload that decodes
the body buffer into pooled **Data**. The NNTP client stays decoupled from yEnc — it hands
over bytes and never decodes. The post/write path keeps a text-oriented body.

## Context

The pre-6.0 pipeline was `bytes → string → bytes`: `NntpStreamReader : StreamReader`
allocated a `string` per line for every line on the wire, and the yEnc decoder immediately
re-encoded each line back to bytes (`encoding.GetBytes(line)`) before decoding. Each body
line cost three allocations and two transcodings, all discarded immediately. The two real
body consumers — binary yEnc downloads (want decoded Data) and text/XML protocol reads
(want bytes an `XmlReader` can consume directly, or text) — were both forced through
`string` lines. Rebuilding the boundary around bytes serves both without transcoding.

This reaffirms [ADR-0001](0001-article-buffering-and-streaming-model.md): articles are
still buffered **whole, one at a time**; the change is the *representation* (pooled bytes,
not `string` lines), not the buffering model. Low memory is still achieved at the
segment-loop level — one part in memory at a time, never a whole multi-segment file.

## Considered options

- **Expose `ReadOnlySequence<byte>` directly** (zero-copy from the pipe). Rejected as the
  default body shape: yEnc decoding, `XmlReader`, and span scanning are simpler and faster
  over a contiguous span, and at ≤1 MB per part the copy into one pooled buffer is
  negligible against network cost. A future zero-copy sink overload (`IBufferWriter<byte>`)
  can decode straight off the sequence for the streaming-download case.
- **GC-owned bytes (`byte[]`/`ImmutableArray<byte>`), no pooling.** Rejected: defeats the
  allocation win on the hot path; per-article ~1 MB arrays churn the LOH.
- **Hand-rolled span line-reader instead of Pipelines.** Rejected: re-implements the
  partial-frame/backpressure bookkeeping Pipelines already solves, which is where
  line-protocol bugs live.

## Consequences

- **Caller-owned lifetime is a behavioral break.** A body is backed by a pooled buffer, so
  the response must be disposed; reading a view after disposal is use-after-free and
  forgetting to dispose leaks a pooled buffer (or falls back to GC pressure). This is new —
  the old `IImmutableList<string>` was GC-owned with no lifetime contract.
- A contiguous-buffer-returning sink path (`IBufferWriter<byte>`/`Stream`) is a later
  **additive** addition, not a breaking one, so it is deferred.
- `PipeReader` drives the underlying stream's `ReadAsync`, so byte-counting can no longer
  rely on a sync-`Read`-only `CountingStream` wrapper; counting moves to the pipe layer or
  gains proper async overrides.

## Measured outcomes

Recorded after the rebuild landed (#122). BenchmarkDotNet `ShortRun`, .NET 10, on a single
64 KiB part; allocations are deterministic across runs, the times are indicative (dev
hardware, noisy short job). The allocation win — not the wall-clock time — is the point.

| Path | Method | Allocated | vs baseline |
|------|--------|----------:|------------:|
| yEnc decode (text, baseline) | `YencArticleDecoder.Decode(lines)` | 143.29 KB | 1.00× |
| yEnc decode (bytes → pooled `Data`) | `YencDecoder.Decode(bytes)` | **2.19 KB** | 0.02× |
| yEnc encode (text, baseline) | `EncodeAsync` → `List<string>` | 416.2 KB | 1.00× |
| yEnc encode (bytes → `IBufferWriter`) | `EncodeAsync` → sink | **920 B** | 0.002× |

The decoded `Data` and the encode sink draw from `ArrayPool<byte>`, so the residual managed
allocation is just per-call objects, not the payload. The byte decode path is *slower* in
wall-clock (~171 µs vs ~63 µs) because it also verifies the part `pcrc32` in the same pass;
that is an accepted trade for the ~98% allocation drop and is dwarfed by network cost.

A `GC.GetAllocatedBytesForCurrentThread()` ceiling on a full part decode is wired into the
TUnit suite (`YencDecoderAllocationTests`) so a regression back to per-line strings fails CI.
