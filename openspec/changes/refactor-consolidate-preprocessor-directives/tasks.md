# Tasks: Consolidate Preprocessor Directives

## 1. Create StreamWriterExtensions
- [ ] 1.1 Create `Extensions/StreamWriterExtensions.cs` with `WriteLineAsync(this StreamWriter, string, CancellationToken)` extension method
- [ ] 1.2 Move `#if NETSTANDARD2_0` logic from `NntpConnection.WriteLineInternalAsync` into the extension
- [ ] 1.3 Update `NntpConnection` to use the new extension method
- [ ] 1.4 Remove `WriteLineInternalAsync` private method from `NntpConnection`

## 2. Create StreamExtensions for ReadAsync
- [ ] 2.1 Create `Extensions/StreamExtensions.cs` with `ReadAsync(this Stream, byte[], int, int, CancellationToken)` extension method
- [ ] 2.2 Move `#if NETSTANDARD2_0` logic from `YencEncoder.EncodeAsync` into the extension
- [ ] 2.3 Update `YencEncoder` to use the new extension method

## 3. Update StreamReaderExtensions for ReadLineAsync
- [ ] 3.1 Add `ReadLineWithCancellationAsync(this StreamReader, CancellationToken)` to `StreamReaderExtensions`
- [ ] 3.2 Update `NntpStreamReader` to delegate to the extension method
- [ ] 3.3 Keep minimal `#if` in `NntpStreamReader` for method signature only (override vs new)

## 4. Create LockShims
- [ ] 4.1 Create `Util/Compatibility/LockShims.cs` with type alias pattern
- [ ] 4.2 Update `NntpClientPool` to use `LockShims.LockType` instead of conditional `Lock`/`object`

## 5. NzbParser (Documentation Only)
- [ ] 5.1 Add XML documentation comment explaining `ParseAsync` is only available on .NET Standard 2.1+
- [ ] 5.2 Keep existing `#if !NETSTANDARD2_0` as it controls API availability (acceptable exception)

## 6. Validation
- [ ] 6.1 Run `dotnet build` for all target frameworks
- [ ] 6.2 Run `dotnet test` to ensure no regressions
- [ ] 6.3 Verify no `#if` directives remain in `Nntp/`, `Nzb/`, `Yenc/` (except NzbParser API availability)
- [ ] 6.4 Verify all new extension/shim files are in correct namespaces

## Dependencies
- Tasks 1-4 can be done in parallel
- Task 5 is independent
- Task 6 depends on all previous tasks
