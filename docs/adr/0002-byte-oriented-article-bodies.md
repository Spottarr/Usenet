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
  the response must be disposed; forgetting to dispose leaks a pooled buffer (or falls back to
  GC pressure). This is new — the old `IImmutableList<string>` was GC-owned with no lifetime
  contract.
- A contiguous-buffer-returning sink path (`IBufferWriter<byte>`/`Stream`) is a later
  **additive** addition, not a breaking one, so it is deferred.
- `PipeReader` drives the underlying stream's `ReadAsync`, so byte-counting can no longer
  rely on a sync-`Read`-only `CountingStream` wrapper; counting moves to the pipe layer or
  gains proper async overrides.

## Refinement (6.0.0 finalization)

Two refinements were made while hardening the path for release, neither changing the core
byte-oriented decision:

- **Fail-fast views.** `Body` and the yEnc `Data` view are backed by a `MemoryManager<byte>`
  that the owning response/part invalidates on dispose, so a use-after-dispose read throws
  `ObjectDisposedException` instead of silently reading a recycled buffer — even when the
  `ReadOnlyMemory<byte>` was captured before disposal. The earlier framing ("reading a view
  after disposal is use-after-free") described undefined behavior; it is now a defined,
  immediate error. Cost is one small manager object per body, negligible against the body
  buffer and the network round-trip.
- **One disposal contract.** Every public pooled-buffer owner (`NntpArticleResponse`,
  `YencPart`) now shares the same shape — `IDisposable` + `IAsyncDisposable`, a finalizer that
  returns the buffer as a safety net, and a single shared
  `PooledBufferDiagnostics.LeakedBufferCount` incremented only when the finalizer (not
  `Dispose`) reclaims a leaked buffer. The rent/return/leak-count logic lives in one internal
  `PooledBuffer` helper; the finalizer stays on each owning type (it cannot be factored out).

- **CRC moved to `System.IO.Hashing.Crc32`.** The hand-rolled scalar `Crc32` was replaced by
  the SIMD/hardware-accelerated first-party `System.IO.Hashing.Crc32`, computing the same
  ISO-HDLC CRC-32 yEnc uses. The decoder now fills the pooled buffer first and hashes the
  contiguous span in one accelerated pass (the encoder `Append`s each block) rather than
  interleaving a scalar CRC per byte, which removes the wall-clock caveat noted below.

## Measured outcomes

The text baselines below were the pre-6.0 path; that path has since been **removed** (only the
byte-oriented codec ships), so the comparison is historical — kept to record the allocation win
that motivated the rebuild. Numbers from BenchmarkDotNet `ShortRun`, .NET 10, on a single 64 KiB
part (#122); allocations are deterministic, times were indicative (noisy dev hardware).

| Path | Method | Allocated | vs (removed) text baseline |
|------|--------|----------:|------------:|
| yEnc decode (text, removed baseline) | `YencArticleDecoder.Decode(lines)` | 143.29 KB | 1.00× |
| yEnc decode (bytes → pooled `Data`) | `YencDecoder.Decode(bytes)` | **2.19 KB** | 0.02× |
| yEnc encode (text, removed baseline) | text `EncodeAsync` → `List<string>` | 416.2 KB | 1.00× |
| yEnc encode (bytes → `IBufferWriter`) | `EncodeAsync` → sink | **920 B** | 0.002× |

The decoded `Data` and the encode sink draw from `ArrayPool<byte>`, so the residual managed
allocation is just per-call objects, not the payload; the CRC swap does not change these
allocation figures. The earlier byte-decode wall-clock penalty (~171 µs vs ~63 µs) came from the
scalar per-byte CRC verifying the part `pcrc32` in the decode loop; moving to the
SIMD/hardware-accelerated `System.IO.Hashing.Crc32` over the contiguous decoded span removes that
penalty. Re-run `YencDecoderBenchmarks`/`YencEncoderBenchmarks` on release hardware to refresh the
times; the allocation win is the durable result.

A `GC.GetAllocatedBytesForCurrentThread()` ceiling on a full part decode is wired into the
TUnit suite (`YencDecoderAllocationTests`) so a regression back to per-line strings fails CI.
