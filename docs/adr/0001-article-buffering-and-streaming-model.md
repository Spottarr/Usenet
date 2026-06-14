# Article buffering and the streaming / low-memory model

## Status

accepted (reaffirmed by [ADR-0002](0002-byte-oriented-article-bodies.md) in 6.0.0: the
whole-part buffering model is kept; only the body's representation changes from `string`
lines to pooled bytes)

## Decision

Article bodies are read and buffered **whole, one article at a time** (asynchronously, into pooled buffers). The library does **not** expose a lazy per-article body stream. Low memory usage is achieved at the **file / segment loop level** — never holding a whole multi-segment file in memory — not by streaming the bytes of a single article incrementally.

Consumers that need to inspect an article's headers without paying to transfer its body use `HeadAsync` (NNTP `HEAD`) first and only then issue a conditional `BodyAsync`/`ArticleAsync`.

## Context

This is a published NuGet library, so the shape of the article-reading API is hard to change later. A reader who sees `NntpArticle.Body` fully materialized as an `IImmutableList<string>` may assume it's an oversight and try to "fix" it into a lazy stream. It isn't — buffering each article whole is deliberate, for three reasons:

- **Articles are bounded-small.** Server posting limits and NZB segmentation keep a single article body well under ~1 MB; large files are thousands of articles. So buffering one whole article is already O(1) bounded memory, and the meaningful low-memory win is not holding all segments at once — which the file/segment loop already achieves.
- **yEnc integrity requires the whole part.** The `=yend` footer carries a CRC32 over the part, so a body must be read end-to-end to be verified regardless.
- **Connection framing stays clean.** NNTP is a single ordered connection whose multi-line data block is terminated by a `.` line. Buffering each response to completion leaves the connection immediately reusable by the pool, with no risk of a later command reading leftover body bytes.

## Considered options

- **HttpClient-style lazy body stream** over a single `ARTICLE` response, with drain-or-close on dispose and one-active-reader-per-connection enforcement (Pipelines-based framing). Rejected: it adds significant machinery and connection-lifetime coupling, and it does not actually help the known use cases. In particular, for the "skip the body when a header is present" case it is strictly worse than `HEAD`-first — once `ARTICLE` is issued the server has already begun sending the body, so skipping means draining wasted bytes off the socket or closing the connection, whereas `HEAD` tells the server not to send the body at all.

## Consequences

- `NntpArticle.Body` remains a materialized, re-enumerable collection — simple and backwards-compatible.
- The "headers-only, maybe body" optimization is expressed at the protocol level (`HEAD` then conditional `BODY`), costing one extra round-trip only when the body is actually needed.
- If a future use case genuinely needs incremental consumption of a single very large article, this decision must be revisited — adding a lazy stream then is a breaking API addition that brings the drain/single-reader machinery with it.
