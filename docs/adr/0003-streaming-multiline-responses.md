# Streaming unbounded multi-line responses with a drain-on-dispose contract

## Status

accepted (6.0.0)

## Decision

Unbounded multi-line commands (`XOVER`, `XHDR`, `HDR`, `LISTGROUP`, `NEWNEWS`,
`LIST ACTIVE`, `LIST NEWSGROUPS`, and similar range/wildmat scans) return
`IAsyncEnumerable<T>`, parsed per line as it arrives off the `PipeReader`. Bounded
multi-line commands (`CAPABILITIES`, `HELP`, `GROUP`, `LIST OVERVIEW.FMT`, `DATE`) stay
materialized as typed responses. The data block of a streamed response must be drained
before the connection is reusable, so enumerating a streamed result carries a contract:
the caller enumerates it fully, or disposes the enumerator (which drains or closes the
connection), before issuing the next command on that lease.

## Context

- Pre-6.0 every multi-line response was buffered whole via `ToListAsync` before parsing.
- For the core indexing operation (`XOVER` over millions of articles) that meant holding
  the entire range in a `List<string>` in memory, which contradicted the library's
  low-memory claim and forced the consumer to wait for the full transfer before starting.
- NNTP is a single ordered connection whose multi-line data block is terminated by a `.`
  line, so the block must be consumed before the connection can serve another command.

## Considered options

- **Keep materializing.** Simpler, no lifetime contract, but flat-out wrong for the
  workload that most needs low memory, and high latency to first row.
- **Stream everything including bounded commands.** Rejected: bounded responses gain
  nothing from streaming and are more ergonomic as typed aggregates.

## Consequences

- Memory stays flat over arbitrarily large ranges; the consumer processes the first row
  while the rest is still arriving.
- New behavioral contract: a streamed enumerator ties up its connection/lease until fully
  enumerated or disposed. The pooled lease must drain or discard a partially-consumed
  enumerator on return to avoid leaving unread bytes on a reused connection.
- This is the same framing discipline [ADR-0001](0001-article-buffering-and-streaming-model.md)
  established for bodies, now applied to multi-line results.
