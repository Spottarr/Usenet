# Compressed overview transport and consolidated connection options

## Status

accepted (6.0.0)

## Decision

`XFEATURE COMPRESS GZIP` is implemented as a **transparent connection mode**: once negotiated,
the connection inflates the (gzip/zlib-compressed) data block of every subsequent multi-line
response, while the status line stays clear text. The decompression stage sits in the transport,
between the socket bytes and the line framer, scoped to the data block. With it on, the existing
streamed scans (`OverAsync`, `XoverAsync`, `HdrAsync`, …) ride compressed bytes with no change to
their method shapes.

Compression is **configuration, not a command.** It is expressed as an `NntpCompression` value
(capturing the `TERMINATOR`/no-terminator wire variants) on new **options objects**
— `NntpConnectionOptions` and `NntpPoolOptions` — which also absorb host, port, SSL, credentials
and pool size. The connection negotiates compression as part of its session-setup recipe
(connect → authenticate → enable compression, in that order, since some servers reject the feature
pre-auth), and the pool re-applies the whole recipe on every transparent reconnect. Because
host/port/SSL move into the options, `ConnectAsync(CancellationToken cancellationToken = default)`
reads them from the options rather than taking them as arguments.

`INntpClientCompression` is **removed from the public surface**: the standalone
`XfeatureCompressGzipAsync` command and the unimplemented `XzverAsync`/`XzhdrAsync` are gone.

## Context

The compression methods shipped as a fully public interface on `INntpClient`, and every one threw
`NotImplementedException` (the pooled client faithfully delegated to the throwing methods). Their
doc comments pointed at an external gist for a "decompressing connection" that was never built.
Shipping public API that throws at runtime is the opposite of the release's ergonomics goal.

Compressed overview is, however, a genuine performance feature for the headline workload — `XOVER`
over millions of articles — so it is worth building rather than merely deleting. Two unrelated
mechanisms exist: the mode-based `XFEATURE COMPRESS GZIP` (Giganews-style, compresses the data
block of all subsequent multi-line commands) and the self-contained yEnc+DEFLATE commands
`XZVER`/`XZHDR` (Astraweb-style). The mode-based one transparently accelerates *every* streamed
scan for the broadest set of providers, so it is the 6.0.0 target.

The pool forced the configuration shape. `NntpClientPool` reconnects dropped connections
transparently, so a one-off `XFEATURE` command issued on a lease would silently not survive a
reconnect — the replacement connection would be in plain mode while the framer still tried to
inflate, corrupting the block. Compression therefore has to be part of the session-setup recipe
the pool re-applies, which makes it connection configuration rather than an imperative call.

## Considered options

- **Scope.** `XFEATURE` only (chosen, widest coverage per unit of work) vs. also implementing
  `XZVER`/`XZHDR` vs. those alone. The self-contained commands are deferred; adding them later is
  additive and they will return the same typed `NntpStreamResponse<NntpArticleOverview>` /
  `<NntpHeaderField>` as their plaintext siblings.
- **Enablement.** A manual command, a session option, or both. Session-option-only was chosen: it
  is the only shape that is correct under transparent pool reconnects, and it gives one obvious way
  to do it.
- **Configuration carrier.** Threading compression through as an extra parameter on `ConnectAsync`
  and the pool constructor vs. introducing options objects. Options objects were chosen; they also
  retire the 6-argument positional pool constructor.

## Consequences

- A new inflate stage lives in the transport ahead of line framing, active only while the mode is
  on and only over the data block. The `TERMINATOR` variant appends a literal `.` line after the
  compressed payload so the framer can find the block boundary without trusting the gzip trailer;
  the non-terminator variant relies on the stream end. This is the wire ambiguity the
  `NntpCompression` value disambiguates.
- Connection configuration is consolidated: `new NntpConnection(NntpConnectionOptions)` /
  `new NntpClientPool(NntpPoolOptions)`, and `ConnectAsync` no longer takes host/port/SSL.
- `XZVER`/`XZHDR` and the standalone `XFEATURE` command are not in the public 6.0.0 surface;
  re-introducing the XZ commands later is non-breaking.
- Decompression failure (a truncated or corrupt block) surfaces as a transport error on the
  affected command rather than as silently dropped rows.
