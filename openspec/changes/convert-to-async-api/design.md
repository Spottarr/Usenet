# Design: Async API Conversion

## Context
The Usenet library currently uses a mixed sync/async approach where only `ConnectAsync` is async. All NNTP commands execute synchronously, blocking threads during network I/O. This design predates modern async-first .NET patterns and limits scalability when downloading many articles concurrently.

The library targets:
- .NET Standard 2.0 (no `IAsyncEnumerable`, no `ValueTask`)
- .NET Standard 2.1 (`IAsyncEnumerable` available)
- .NET 10.0 (full async support)

## Goals
- Make all network I/O operations async
- Minimize API surface area
- Maintain memory efficiency (no buffering entire responses)
- Support cancellation throughout
- Ensure connection integrity (no leftover data in network stream between requests)

## Non-Goals
- Backward compatibility with synchronous API (this is a breaking change)
- Supporting .NET Standard 2.0 for `IAsyncEnumerable` (would require Microsoft.Bcl.AsyncInterfaces dependency)
- Parallel execution within single connection (NNTP protocol is sequential)
- Providing sync wrappers or facades

## Decisions

### Decision 1: Drop .NET Standard 2.0 for async enumerable types
**What**: Types using `IAsyncEnumerable<T>` will only be available on .NET Standard 2.1+
**Why**: Adding `Microsoft.Bcl.AsyncInterfaces` adds complexity and a runtime dependency. The library already has .NET Standard 2.1 target.
**Alternative**: Add the polyfill package - rejected due to added complexity and minimal benefit.

### Decision 2: Use `ValueTask<T>` for high-frequency methods
**What**: Methods called frequently (like `ReadLineAsync`) use `ValueTask<T>`, others use `Task<T>`
**Why**: `ValueTask<T>` reduces allocations when operations complete synchronously (e.g., buffered reads)
**Alternative**: Use `Task<T>` everywhere - simpler but more allocations on hot paths

### Decision 3: Eager consumption of multi-line responses (no lazy streaming)
**What**: All multi-line responses (article bodies, group lists, etc.) are fully read from the network before the async method completes. No `IAsyncEnumerable<string>` for response data.
**Why**: 
- NNTP is a sequential protocol - one command at a time per connection
- If a consumer doesn't fully enumerate a lazy response, leftover data remains in the network stream
- Subsequent commands would read stale data, corrupting the protocol state
- The connection would need complex state tracking to detect/recover from partial reads
**Trade-off**: Large responses are buffered in memory. This is acceptable because:
- Article bodies are typically processed immediately (yEnc decode)
- Memory-constrained scenarios can use the pool with multiple connections
- Correctness > memory optimization for protocol integrity

### Decision 4: Single async interface (no sync wrappers)
**What**: Remove all synchronous methods, provide only async versions. No sync facade.
**Why**: 
- Reduces API surface
- Sync-over-async is an antipattern (blocks threads)
- Consumers can use `.GetAwaiter().GetResult()` if truly needed (at their own risk)

### Decision 5: Keep `NntpArticle` as unified type (no header/body split)
**What**: `NntpArticle` remains a single type with both headers and body.
**Why**: 
- NNTP protocol has separate `HEAD`, `BODY`, and `ARTICLE` commands
- When `HEAD` is called, only headers come over the wire (no body to read)
- When `BODY` is called, only body comes over the wire (no headers)
- When `ARTICLE` is called, both come and both are read eagerly
- Splitting the type adds complexity without benefit since the data availability is determined by the command used, not consumer choice
**Alternative**: Create `NntpArticleHeaders` type - rejected as it fragments the API without solving a real problem

### Decision 6: ConfigureAwait(false) everywhere
**What**: All `await` calls in the library use `.ConfigureAwait(false)`
**Why**: 
- Library code doesn't need synchronization context
- Avoids deadlocks when called from UI/ASP.NET contexts
- Marginal performance improvement

### Decision 7: Consolidate NNTP client interfaces
**What**: Consider merging RFC-specific interfaces into single `INntpClient`
**Why**: Multiple interfaces (`INntpClientRfc2980`, etc.) add complexity without clear benefit
**Decision**: Keep separate for now - RFC grouping provides documentation value and enables partial implementations

## Risks / Trade-offs

### Risk: .NET Standard 2.0 consumers lose functionality
**Mitigation**: Document that async enumerable features require .NET Standard 2.1+. Core functionality (connect, auth, simple commands) can still work on 2.0 with `Task<T>` returns.

### Risk: Breaking change impacts existing users
**Mitigation**: 
- Major version bump (semver)
- Detailed migration guide
- Clear changelog

### Risk: Large responses consume memory
**Mitigation**: 
- This is acceptable for correctness
- Document memory characteristics
- Connection pooling allows parallel downloads across connections
- yEnc streaming still works (decode as body lines arrive, just within the async method)

### Risk: Complexity in YencStream with eager body loading
**Mitigation**: `YencStream` receives the complete body lines and decodes them. The decode itself can still stream bytes to the output, just the input is pre-loaded. Alternatively, yEnc decode can happen inline during body reading within the async method boundary.

## Resolved Questions

1. **Should we provide a sync facade for simple scenarios?**
   - **No** - encourages bad practices, increases API surface

2. **Should `NntpArticle` be split into `NntpArticleHeaders` and a separate body stream?**
   - **No** - NNTP protocol already has `HEAD`/`BODY`/`ARTICLE` commands that determine what data arrives. The type doesn't need to model lazy loading since the network read behavior is command-dependent, not consumer-dependent.

3. **ConfigureAwait(false) usage?**
   - **Yes** - use throughout library code
