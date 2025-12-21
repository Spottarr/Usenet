# Tasks: Convert to Async API

## 1. Foundation - Async Connection Layer
- [ ] 1.1 Update `INntpConnection` interface with async method signatures (`CommandAsync`, `MultiLineCommandAsync`, `GetResponseAsync`, `WriteLineAsync`)
- [ ] 1.2 Implement async methods in `NntpConnection` using `ConfigureAwait(false)`
- [ ] 1.3 Update `NntpStreamReader` to use `ReadLineAsync`
- [ ] 1.4 Ensure multi-line responses are fully consumed before returning
- [ ] 1.5 Add `CancellationToken` support to all async methods
- [ ] 1.6 Add tests for async connection methods

## 2. Core NNTP Client Interfaces
- [ ] 2.1 Update `INntpClientConnection` - `Quit()` becomes `QuitAsync(CancellationToken)`
- [ ] 2.2 Update `INntpClientRfc4643` - `Authenticate()` becomes `AuthenticateAsync(CancellationToken)`
- [ ] 2.3 Update `INntpClientRfc3977` - all methods become async with `CancellationToken`
- [ ] 2.4 Update `INntpClientRfc2980` - all methods become async with `CancellationToken`
- [ ] 2.5 Update `INntpClientRfc6048` - all methods become async with `CancellationToken`
- [ ] 2.6 Update `INntpClientCompression` - all methods become async with `CancellationToken`
- [ ] 2.7 Update `INntpClient` composite interface

## 3. NntpClient Implementation
- [ ] 3.1 Convert all `NntpClient` methods to async with `ConfigureAwait(false)`
- [ ] 3.2 Update `ArticleAsync`, `HeadAsync`, `BodyAsync` implementations
- [ ] 3.3 Update `PostAsync` and `IhaveAsync` implementations
- [ ] 3.4 Update all list/group methods to async
- [ ] 3.5 Add tests for async client operations

## 4. Response Parsers
- [ ] 4.1 Update `IResponseParser<T>` for async parsing if needed
- [ ] 4.2 Update `IMultiLineResponseParser<T>` for async parsing
- [ ] 4.3 Update `ArticleResponseParser` to work with async enumeration internally
- [ ] 4.4 Update other response parsers as needed
- [ ] 4.5 Add tests for async parsing

## 5. Pooled Client
- [ ] 5.1 Update `IPooledNntpClient` interface for async methods
- [ ] 5.2 Update `PooledNntpClient` implementation with `ConfigureAwait(false)`
- [ ] 5.3 Update `NntpClientPool` async operations
- [ ] 5.4 Add tests for pooled async operations

## 6. Article Writer
- [ ] 6.1 Update `ArticleWriter.Write` to `WriteAsync` returning `Task`
- [ ] 6.2 Use `ConfigureAwait(false)` in implementation
- [ ] 6.3 Add tests for async article writing

## 7. yEnc Async Conversion
- [ ] 7.1 Update `YencStreamDecoder.Decode` to `DecodeAsync` returning `Task<YencStream>`
- [ ] 7.2 Update `YencStream` internals if needed
- [ ] 7.3 Update `YencEncoder.Encode` to `EncodeAsync` returning `Task<IReadOnlyList<string>>`
- [ ] 7.4 Use `ConfigureAwait(false)` throughout
- [ ] 7.5 Add tests for async yEnc operations

## 8. NZB Async Additions
- [ ] 8.1 Add `NzbParser.ParseAsync(Stream, CancellationToken)` overload
- [ ] 8.2 Add `NzbParser.ParseAsync(TextReader, CancellationToken)` overload
- [ ] 8.3 Remove `NzbWriter.Write` sync method (keep only `WriteAsync`)
- [ ] 8.4 Add tests for async NZB operations

## 9. Cleanup and Documentation
- [ ] 9.1 Remove all synchronous method implementations from interfaces and classes
- [ ] 9.2 Verify `ConfigureAwait(false)` used on all awaits
- [ ] 9.3 Update XML documentation for async methods
- [ ] 9.4 Update README with async usage examples
- [ ] 9.5 Add migration guide for breaking changes
- [ ] 9.6 Update package version (major bump)

## 10. Validation
- [ ] 10.1 Run all tests and fix failures
- [ ] 10.2 Build for all target frameworks (.NET Standard 2.0, 2.1, .NET 10.0)
- [ ] 10.3 Review API surface for minimal exposure
- [ ] 10.4 Verify no leftover sync methods remain

## Dependencies
- Section 1 must complete before sections 2-6
- Section 2 must complete before section 3
- Sections 3, 4, 5, 6 can run in parallel after section 2
- Section 7 is independent after section 1
- Section 8 is independent
- Section 9 depends on sections 1-8
- Section 10 depends on section 9
