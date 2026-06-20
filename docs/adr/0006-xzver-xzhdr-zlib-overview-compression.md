# XZVER/XZHDR zlib overview compression for Highwinds-family servers

## Status

accepted

## Decision

Implement `XZVER` and `XZHDR` as first-class commands that stream the same typed rows as their
plaintext siblings (`XZVER` → `NntpArticleOverview`, `XZHDR` → `NntpHeaderField`), decoding the
compressed data block as a **per-command, streaming decompression layer whose codec is chosen by
sniffing the data block's first byte**. The `[COMPRESS=GZIP]` label on the status line is not trusted:
the magic byte selects the decoder — `0x78` (a valid [zlib](https://www.rfc-editor.org/rfc/rfc1950)
header, [RFC 1950](https://www.rfc-editor.org/rfc/rfc1950)) → `ZLibStream`; `0x1f` (`1f 8b`,
[gzip](https://www.rfc-editor.org/rfc/rfc1952), RFC 1952) → `GZipStream`; otherwise raw DEFLATE
([RFC 1951](https://www.rfc-editor.org/rfc/rfc1951)) → `DeflateStream`. All three are BCL streams; no
vendored inflater is needed. Once the status line is read the transport feeds the remaining bytes
through the chosen decompressor, frames the **decompressed** bytes with the existing line framer, and
runs the existing per-line overview/header parser. Memory stays flat: the decompression window and
input buffer are a constant per-command overhead (~40 KB), and the per-row marginal allocation is
identical to `XOVER`/`XHDR` because the final parse stage is unchanged. `ZLibStream` and `DeflateStream`
turn a truncated or terminator-less member into a clean EOF, so the framer terminates gracefully;
`GZipStream` is the one arm that can block on such a malformed member, bounded by the caller's
cancellation token (see below).

`XZVER`/`XZHDR` are exposed as **explicit siblings** of `Xover*`/`Xhdr*` — mirroring the existing
`Over`/`Xover` duality the API already keeps "because real servers implement one or the other." The
consumer (or a higher layer) selects them from the typed `NntpCapabilities`. Overview compression is
**command selection, not a transport mode**: it stays off the `NntpCompression` axis and never
becomes connection state the pool must re-apply.

This **reverses the deferral in [ADR-0005](0005-compressed-overview-transport-and-connection-options.md)**,
which removed `XZVER`/`XZHDR` from the 6.0.0 surface and recorded "re-introducing the XZ commands
later is non-breaking." RFC 8054 `COMPRESS DEFLATE` remains the preferred mechanism where a server
offers it; `XZVER`/`XZHDR` cover the servers that do not.

## Context

ADR-0005 standardized on RFC 8054 `COMPRESS DEFLATE` and rejected "supporting both behind capability
detection (more code and test surface than the workload justifies)." New information reopens that
trade-off: the entire **Highwinds network** (eweka and many resellers) does **not** advertise
`COMPRESS` at all. Its `CAPABILITIES` lists `XZVER`, `XZHDR`, and `XFEATURE-COMPRESS GZIP TERMINATOR`
but no `COMPRESS`. So today `NntpCompression.Deflate` against eweka fails fast and throws — these
servers get **no compression whatsoever** from the library. The workload that "doesn't justify" the
extra code is, for Highwinds users, the only compression available.

The behaviour was verified empirically against `news.eweka.nl` (a throwaway probe, since removed):

- **The payload is zlib, despite the label.** The status line reads
  `224 Overview Information Follows [COMPRESS=GZIP]`, but the data block begins `78 01` — a zlib
  stream, not gzip (`1f 8b`). Decoding it as gzip is wrong twice over.
- **The decompressed output is byte-identical to `XOVER`, including the terminating `.` line.** A
  1000-row range decompressed to 314006 bytes — exactly the 314001-byte plaintext `XOVER` block plus
  its 5-byte `\r\n.\r\n` terminator. The dot-terminator lives *inside* the compressed stream, so the
  existing framer and `ProcessLine` terminator detection apply unchanged to the inflated bytes; there
  is no wire-terminator to search for and no ambiguity.
- **The BCL `ZLibStream` decodes one member off a live socket and stops cleanly.** Reading straight
  from the socket, `ZLibStream` consumed exactly the 80957-byte member, returned the full decompressed
  block, and stopped at the Adler-32 footer without over-reading or blocking. eweka's payload is zlib,
  but the wire format is not guaranteed to be zlib on every server that advertises these commands, so
  the decoder is chosen by sniffing the magic byte rather than hard-coding zlib (see below).
- **`XZVER` and `XFEATURE COMPRESS GZIP TERMINATOR` + `XOVER` produce identical bytes** (same 80957-byte
  zlib member). The per-command form needs no connection mode, so it is the simpler of the two.
- **Ratio ≈ 3.88×** (~74% less bandwidth) on the overview scan. Modest but real, and it applies to the
  indexer's dominant traffic.

This also explains why ADR-0005's earlier `XFEATURE COMPRESS GZIP` attempt "hung forever." It treated
a `78 01` zlib stream as gzip via `GZipStream` and combined it with an `XFEATURE` framing that had no
in-band terminator and waited for a socket close that never comes on a persistent connection. The
"block boundary is unknowable" problem was in large part a **wrong-decoder problem**.

The `GZipStream` blocking behaviour is real but **conditional**, and was originally mis-stated here as
an absolute reason to avoid the BCL gzip decoder. It was re-tested directly on net9.0 by delivering a
single compressed member to a stream that then blocks forever (simulating a persistent socket where
the server awaits the next command):

- Read **to EOF**, `GZipStream` **hangs** — after the member's CRC32/ISIZE footer it speculatively
  reads the source for a *concatenated gzip member* (RFC 1952 permits several), and that read blocks.
  `ZLibStream` returns a clean EOF because the zlib format has no concatenated-member concept.
- Read only **until the in-band `.` terminator** — which is exactly what the line framer does, since
  the dot lives *inside* the decompressed payload — **neither decoder hangs**. The framer stops
  pulling bytes at the dot and never triggers the post-member probe.

So native `GZipStream` is usable for `XZVER`/`XZHDR` on the happy path. Its one residual asymmetry:
a **truncated or terminator-less member** makes the framer read past the member, where `GZipStream`
hangs (bounded by the caller's cancellation token) while `ZLibStream`/`DeflateStream` surface a clean
EOF and let the framer terminate. The drain budget still bounds a *well-formed but oversized*
remainder when a partially-read stream is abandoned; it cannot interrupt the blocking read of a
truncated `GZipStream` member, so that case relies on the caller's cancellation token. The indexer's
hot path always supplies one, and no known server emits true gzip here, so the residual risk is a
malformed-server edge rather than a normal path.

## Considered options

- **Decoder: header-sniff native dispatch (chosen) vs hard-coded `ZLibStream` vs a vendored
  inflater.** The `[COMPRESS=GZIP]` label is unreliable and the wire format is not guaranteed across
  servers (eweka sends zlib `78 01`; others may send true gzip or raw DEFLATE), so the first byte
  selects the codec: `0x78` → `ZLibStream`, `0x1f` → `GZipStream`, otherwise raw `DeflateStream`. All
  three are BCL streams, so no vendored inflater is needed. This mirrors how battle-tested clients
  (e.g. NZBGet) decode with zlib's automatic header detection instead of trusting the advertised
  name. Hard-coding `ZLibStream` would have been simpler but eweka-specific; the sniff costs one byte
  and makes the feature generic.
- **Framing: per-command `XZVER`/`XZHDR` (chosen) vs the `XFEATURE COMPRESS GZIP` mode.** Both produce
  identical compressed bytes. The per-command commands are stateless — no `290`-enabled connection
  mode that the pool would have to re-apply on every transparent reconnect (the exact problem that
  forced `COMPRESS` to be configuration in ADR-0005). `XFEATURE`'s only documented advantage,
  `TERMINATOR`, is moot because the dot-terminator is already inside the decompressed stream.
- **Decode shape: streaming inflate (chosen) vs buffer-then-inflate.** Buffering the whole block before
  inflating would reintroduce the `ToListAsync` memory problem [ADR-0003](0003-streaming-multiline-responses.md)
  removed for the headline scan. Streaming inflate keeps memory flat and reuses the streamed-response
  surface and drain contract verbatim.
- **Surface: explicit `Xzver*`/`Xzhdr*` siblings (chosen) vs a transparent capability-driven upgrade
  of `Xover*`/`Xhdr*`.** Explicit siblings match the existing `Over`/`Xover` pattern and the library's
  preference for explicitness (de-overloaded API, typed results). A transparent auto-upgrade can be
  layered on additively later; it is not baked into the transport here.
- **Axis: command selection (chosen) vs extending `NntpCompression`.** `NntpCompression` is a
  whole-session transport mode (`None`/`Deflate`). XZ compression is scoped to one command's data
  block, so modelling it as a transport value would be a category error and would drag in the
  pool-reapply machinery it does not need.

## Consequences

- The chosen decoder is selected per command by sniffing the data block's first byte
  (`0x78`→`ZLibStream`, `0x1f`→`GZipStream`, else raw `DeflateStream`); all three are BCL streams. A
  truncated member surfaces a clean EOF on the `ZLibStream`/`DeflateStream` arms; on the `GZipStream`
  arm it is bounded by the caller's cancellation token (a malformed-server edge, not a normal path).
- New public `XzverAsync`/`XzhdrAsync` (range and current-pointer forms, following the ADR-0004 naming
  convention), returning the same `NntpStreamResponse<NntpArticleOverview>` /
  `NntpStreamResponse<NntpHeaderField>` as their plaintext siblings. Malformed rows are skipped, as in
  ADR-0003.
- The transport gains a per-command decompression scope that mirrors `InstallDeflateLayer`: after the
  XZ status line, sniff the first data-block byte, wrap the underlying stream in the matching
  decompressor, frame lines off a `PipeReader` over it, and tear the layer down when the `.` terminator
  is reached so subsequent commands read plaintext again. Bytes the plaintext reader over-reads past the status line are recovered and replayed through
  the decompressor using the existing `PrefixStream`/leftover-recovery already built for ADR-0005.
- Memory stays flat over arbitrarily large ranges; the only added cost is a constant ~40 KB per active
  XZ command (zlib window + input buffer). An allocation test pins the per-row marginal cost under a
  ceiling, as `XOVER` already does, so an accidental buffer-the-whole-block regression fails CI.
- Compression covers **overview and header metadata only**, never `ARTICLE`/`BODY`. This is by
  construction — a per-command decode, not a transport layer — and so avoids ADR-0005's "not uniform"
  failure where a session-wide layer tried to inflate plaintext bodies. Article bodies are yEnc binary
  and near-incompressible anyway.
- `BytesRead` counts decompressed (logical) bytes for an XZ command, since the framer sits above the
  `ZLibStream`, consistent with how ADR-0005 counts under `COMPRESS DEFLATE`.
- The library now supports two compression mechanisms on different axes: `COMPRESS DEFLATE` (transport,
  preferred) and `XZVER`/`XZHDR` (per-command, for Highwinds-family servers). The added test surface is
  bounded by reusing the existing parsers and streamed-response machinery.
- `NntpCompression.Deflate` still throws against a server that does not advertise `COMPRESS`; choosing
  `XZVER`/`XZHDR` is the documented path for those servers. (Making that failure friendlier, or adding
  a capability-aware convenience selector, are separate additive changes.)
