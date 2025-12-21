# Tasks: Consolidate Preprocessor Directives

## 1. Create StreamWriterExtensions
- [x] 1.1 Create `Extensions/StreamWriterExtensions.cs` with `WriteLineAsync(this StreamWriter, string, CancellationToken)` extension method
- [x] 1.2 Move `#if NETSTANDARD2_0` logic from `NntpConnection.WriteLineInternalAsync` into the extension
- [x] 1.3 Update `NntpConnection` to use the new extension method
- [x] 1.4 Remove `WriteLineInternalAsync` private method from `NntpConnection`

## 2. Create StreamExtensions for ReadAsync
- [x] 2.1 Add `ReadByteAsync(this Stream, byte[], CancellationToken)` to `Extensions/StreamExtensions.cs`
- [x] 2.2 Move `#if NETSTANDARD2_0` logic from `YencEncoder.EncodeAsync` into the extension
- [x] 2.3 Update `YencEncoder` to use the new extension method

## 3. NntpStreamReader (Acceptable Exception)
- [x] 3.1 Keep `#if NET8_0_OR_GREATER` in `NntpStreamReader` - this controls method signature (`override` vs `new`) and is intrinsically coupled with the implementation calling `base.ReadLineAsync()`
- [x] 3.2 Document this as an acceptable exception per design decisions

## 4. NntpClientPool Lock
- [x] 4.1 Simplified to use `object` for locking on all platforms
- [x] 4.2 Removed `#if NET9_0_OR_GREATER` conditional - `Lock` type is an optimization but `object` works everywhere

## 5. NzbParser (Documentation Only)
- [x] 5.1 Add XML documentation comment explaining `ParseAsync` is only available on .NET Standard 2.1+
- [x] 5.2 Keep existing `#if !NETSTANDARD2_0` as it controls API availability (acceptable exception)

## 6. Validation
- [x] 6.1 Run `dotnet build` for all target frameworks - PASSED
- [x] 6.2 Run `dotnet test` to ensure no regressions - 255/255 tests pass
- [x] 6.3 Verify no `#if` directives remain in `Nntp/`, `Nzb/`, `Yenc/` except acceptable exceptions
- [x] 6.4 Verify all new extension/shim files are in correct namespaces

## Summary of Changes

### Files Created
- `src/Usenet/Extensions/StreamWriterExtensions.cs` - WriteLineAsync with CancellationToken

### Files Modified  
- `src/Usenet/Extensions/StreamExtensions.cs` - Added ReadByteAsync method
- `src/Usenet/Nntp/NntpConnection.cs` - Removed #if, uses StreamWriterExtensions
- `src/Usenet/Nntp/NntpClientPool.cs` - Removed #if, simplified to object lock
- `src/Usenet/Yenc/YencEncoder.cs` - Removed #if, uses StreamExtensions
- `src/Usenet/Nzb/NzbParser.cs` - Added documentation for API availability

### Acceptable Exceptions (documented)
1. `NntpStreamReader.cs` - `#if NET8_0_OR_GREATER` controls override vs new method signature
2. `NzbParser.cs` - `#if !NETSTANDARD2_0` controls public API availability (XDocument.LoadAsync unavailable)
