# Change: Convert Library to Fully Asynchronous API

## Why
The current NNTP client API is predominantly synchronous, blocking threads during network I/O operations. This is problematic for high-throughput scenarios where multiple articles need to be fetched/posted concurrently. Modern .NET applications expect async-first APIs to maximize scalability and resource efficiency.

## What Changes

### NNTP Client API (**BREAKING**)
- **BREAKING**: All synchronous methods on `INntpClient` and related interfaces become async, returning `Task<T>`
- **BREAKING**: Method names gain `Async` suffix (e.g., `Article()` → `ArticleAsync()`)
- **BREAKING**: Interface method signatures updated across `INntpClientRfc2980`, `INntpClientRfc3977`, `INntpClientRfc4643`, `INntpClientRfc6048`, `INntpClientCompression`
- Add `CancellationToken` parameter to all async methods
- Use `ConfigureAwait(false)` on all internal awaits

### NNTP Connection (**BREAKING**)
- **BREAKING**: `INntpConnection.Command<T>` becomes `CommandAsync<T>` returning `Task<T>`
- **BREAKING**: `INntpConnection.MultiLineCommand<T>` becomes `MultiLineCommandAsync<T>` returning `Task<T>`
- **BREAKING**: `INntpConnection.GetResponse<T>` becomes `GetResponseAsync<T>` returning `Task<T>`
- **BREAKING**: `INntpConnection.WriteLine` becomes `WriteLineAsync` returning `Task`
- Internal stream reading becomes fully async using `ReadLineAsync`

### Response Handling (eager loading)
- All multi-line responses are fully read from network before async method returns
- No lazy `IAsyncEnumerable<string>` - this ensures connection integrity for subsequent requests
- `NntpArticle.Body` remains `IImmutableList<string>` (fully loaded)
- `NntpMultiLineResponse.Lines` remains `IEnumerable<string>` (fully loaded)

### yEnc (**BREAKING**)
- **BREAKING**: `YencStreamDecoder.Decode` becomes `DecodeAsync` returning `Task<YencStream>`
- **BREAKING**: `YencEncoder.Encode` becomes `EncodeAsync` returning `Task<IReadOnlyList<string>>`

### NZB Parser
- Add `ParseAsync(Stream)` and `ParseAsync(TextReader)` overloads for async XML parsing
- Existing `Parse(string)` remains synchronous (already has all data in memory)

### NZB Writer (**BREAKING**)
- **BREAKING**: Remove synchronous `Write` method (keep only `WriteAsync`)

## Impact
- **Affected interfaces**: `INntpClient`, `INntpClientConnection`, `INntpClientRfc2980`, `INntpClientRfc3977`, `INntpClientRfc4643`, `INntpClientRfc6048`, `INntpClientCompression`, `INntpConnection`
- **Affected classes**: `NntpClient`, `NntpConnection`, `PooledNntpClient`, `NntpClientPool`, `YencStreamDecoder`, `YencEncoder`, `YencStream`, `NzbParser`, `NzbWriter`
- **Breaking change**: Yes - all consumers must update to async/await patterns
- **No `IAsyncEnumerable`**: Response data types unchanged, only method signatures change to async

## Design Decisions

### Why no lazy `IAsyncEnumerable<string>` for responses?
NNTP is a sequential protocol - one command at a time per connection. If a consumer doesn't fully read a response, leftover data remains in the network stream, corrupting subsequent commands. Eager loading ensures:
- Connection is always in clean state after each command
- No complex state tracking for partial reads
- Predictable behavior

### Why keep `NntpArticle` as single type (not split headers/body)?
NNTP has separate `HEAD`, `BODY`, and `ARTICLE` commands:
- `HEAD` returns only headers (nothing to split)
- `BODY` returns only body (nothing to split)  
- `ARTICLE` returns both (both loaded eagerly)

The data availability is determined by the command used, not consumer preference.

## Migration Guide
Consumers will need to:
1. Change all method calls to use `await` (e.g., `client.Article(id)` → `await client.ArticleAsync(id)`)
2. Propagate `async` up the call stack
3. Add `CancellationToken` parameters where appropriate
4. Change `writer.Write(doc)` to `await writer.WriteAsync(doc)`
5. Change `YencStreamDecoder.Decode(lines)` to `await YencStreamDecoder.DecodeAsync(lines)`
