# Compressed transport and consolidated connection options

## Status

accepted (6.0.0)

## Decision

Compression is implemented as [RFC 8054](https://www.rfc-editor.org/rfc/rfc8054) `COMPRESS DEFLATE`:
a **whole-session compression layer**. Once negotiated, every byte in both directions — commands the
client sends and all responses the server returns, status lines included — is carried as a single
continuous raw-DEFLATE ([RFC 1951](https://www.rfc-editor.org/rfc/rfc1951)) stream per direction. The
decompression/compression stages sit in the transport between the socket and the line framer, so the
existing streamed scans (`OverAsync`, `XoverAsync`, `HdrAsync`, …) and article retrieval
(`ArticleAsync`, `BodyAsync`, …) all ride the compressed connection with no change to their shapes.

Compression is **configuration, not a command.** It is expressed as an `NntpCompression` value
(`None` or `Deflate`) on the options objects — `NntpConnectionOptions` and `NntpPoolOptions` — which
also absorb host, port, SSL, credentials and pool size. The connection negotiates compression as the
last step of its session-setup recipe (connect → authenticate → `COMPRESS DEFLATE`, in that order,
because RFC 8054 §2.2 forbids authenticating *after* compression is active), and the pool re-applies
the whole recipe on every transparent reconnect. Because host/port/SSL move into the options,
`ConnectAsync(CancellationToken cancellationToken = default)` reads them from the options rather than
taking them as arguments.

`INntpClientCompression` is **removed from the public surface**: the standalone
`XfeatureCompressGzipAsync` command and the unimplemented `XzverAsync`/`XzhdrAsync` are gone.

## Context

This ADR originally specified the mode-based `XFEATURE COMPRESS GZIP` (Giganews-style), which
compresses only the data block of overview commands and leaves the status line clear text. Building
it against a real server surfaced three fatal problems with that model:

- **It is not uniform.** Real servers compress overview responses (`XOVER`/`XHDR`) but return
  `ARTICLE`/`BODY`/`HEAD` as plain text. A transport that inflates the data block of *every*
  multi-line response therefore tries to gzip-decode plaintext and breaks.
- **The block boundary is unknowable on a persistent connection.** The non-terminator variant relied
  on the gzip stream being self-delimiting via socket close — which never happens on a pooled,
  persistent connection, so reads hung forever. The `TERMINATOR` variant searched binary payloads for
  a literal `.` line, which is ambiguous against compressed bytes.
- **It is legacy.** `XFEATURE COMPRESS GZIP` predates standardization; RFC 8054 `COMPRESS DEFLATE`
  (2016) is the IETF-track extension modern servers advertise via `CAPABILITIES`.

RFC 8054 dissolves all three: compression is a uniform session-wide layer, so there is no per-command
"is this one compressed?" question and no data-block boundary to find — the DEFLATE stream's own
framing (a flush at each message boundary) delimits responses. It compresses the headline workload
(`XOVER` over millions of articles) *and* article bodies.

The pool forced the configuration shape. `NntpClientPool` reconnects dropped connections
transparently, so a one-off compression command issued on a lease would silently not survive a
reconnect — the replacement connection would be uncompressed while the transport still tried to
inflate. Compression therefore has to be part of the session-setup recipe the pool re-applies, which
makes it connection configuration rather than an imperative call.

## Considered options

- **Mechanism.** RFC 8054 `COMPRESS DEFLATE` (chosen — standardized, uniform, the modern default) vs.
  the legacy `XFEATURE COMPRESS GZIP` (the original decision here; abandoned for the reasons above)
  vs. supporting both behind capability detection (more code and test surface than the workload
  justifies). The self-contained `XZVER`/`XZHDR` commands remain deferred; re-introducing them later
  is additive.
- **Negotiation.** Optimistic `COMPRESS DEFLATE` with fail-fast (chosen) vs. a `CAPABILITIES`
  pre-check. The pre-check costs an extra round trip on every connect and reconnect, and a server can
  still refuse (`403`) after advertising, so failure handling is needed regardless. The connection
  issues the command directly; `206`/`502`-already-active is success, anything else throws.
- **Refusal handling.** Fail-fast (chosen) vs. silent fallback to plaintext. The user explicitly
  configured compression; silently running uncompressed is the kind of silent divergence that caused
  the original bugs.
- **Enablement.** A session option, not a manual command — the only shape correct under transparent
  pool reconnects.
- **Configuration carrier.** Options objects (chosen) vs. threading compression through `ConnectAsync`
  and the pool constructor as extra parameters; the options objects also retire the 6-argument
  positional pool constructor.

## Consequences

- The transport installs a bidirectional DEFLATE layer the instant the `206` response's CRLF is read:
  a decompressing `DeflateStream` feeds the line framer and a compressing `DeflateStream` wraps the
  command writer, which is flushed after each command so the server can decode it incrementally. Once
  on, the layer is invisible to every command method — there is no per-command compression branching.
- RFC 8054 starts compression *immediately after the 206 CRLF*, and a server may coalesce the status
  line and the first compressed bytes into one TCP segment. The plaintext reader can over-read those
  bytes; the transport recovers any such bytes still buffered after the status line and replays them
  through the decompressor ahead of the socket, so a coalesced segment decodes correctly.
- Compression is negotiated only after a successful authentication (RFC 8054 §2.2 forbids
  authenticating once compression is active). A connection that never authenticates therefore never
  compresses; this matches the pool, which mandates authentication, and every commercial provider.
- `COMPRESS` cannot be issued twice or turned off per RFC 8054; the pool's reconnect establishes a
  fresh socket and re-runs the recipe, which is the supported way to "reset" compression.
- `BytesRead`/`BytesWritten` count decompressed (logical) bytes, since the framer and command writer
  sit above the DEFLATE layer.
- Connection configuration is consolidated: `new NntpConnection(NntpConnectionOptions)` /
  `new NntpClientPool(NntpPoolOptions)`, and `ConnectAsync` no longer takes host/port/SSL.
- A server that refuses `COMPRESS DEFLATE` fails the connection at setup rather than silently serving
  plaintext.
- `XZVER`/`XZHDR` and the standalone `XFEATURE` command are not in the public 6.0.0 surface;
  re-introducing the XZ commands later is non-breaking.
