# Design: Consolidate Preprocessor Directives

## Context
The Usenet library targets multiple frameworks (.NET Standard 2.0, 2.1, .NET 10.0) and uses preprocessor directives to handle API differences. Currently, these directives are scattered across domain code files, mixing platform compatibility concerns with business logic.

The codebase already has established patterns for handling this:
- `Usenet.Extensions` namespace for extension methods (e.g., `QueueExtensions`, `StreamReaderExtensions`)
- `Usenet.Util.Compatibility` namespace for static shim classes (e.g., `ObjectDisposedExceptionShims`, `IntShims`)

## Goals
- Consolidate all `#if` preprocessor directives into `Usenet.Extensions` or `Usenet.Util.Compatibility`
- Keep domain code (Nntp, Nzb, Yenc namespaces) free of platform-specific branching
- Follow existing patterns established in the codebase
- Maintain identical runtime behavior

## Non-Goals
- Changing the target frameworks
- Adding new functionality
- Modifying public API surface

## Decisions

### Decision 1: Extension methods for instance method compatibility
**What**: Use extension methods in `Usenet.Extensions` when providing alternative method signatures for existing types.

**Why**: Extension methods provide a natural calling syntax and follow the pattern established by `QueueExtensions.TryDequeue` and `StreamReaderExtensions.ReadToEndAsync`.

**Implemented**:
- `StreamWriterExtensions.WriteLineAsync(this StreamWriter, string, CancellationToken)` - used by `NntpConnection`
- `StreamExtensions.ReadByteAsync(this Stream, byte[], CancellationToken)` - used by `YencEncoder`

### Decision 2: NntpStreamReader - Acceptable Exception
**What**: The `NntpStreamReader.ReadLineAsync(CancellationToken)` `#if` directive cannot be moved.

**Why**: 
- On .NET 8+, `StreamReader.ReadLineAsync(CancellationToken)` exists and must be overridden
- On older frameworks, it's a new method (not an override)
- The implementation must call `base.ReadLineAsync()` directly, which cannot be delegated to an extension method without causing infinite recursion

**Solution**: Keep the `#if` in `NntpStreamReader` - it controls both method signature AND implementation because they are intrinsically coupled. This is an acceptable exception documented in the spec.

### Decision 3: NntpClientPool Lock - Simplified
**What**: The `#if NET9_0_OR_GREATER` for `Lock` vs `object` was removed.

**Why**: 
- The .NET 9 `Lock` type is an optimization, not a requirement
- Using `object` with `lock()` works on all platforms
- Creating a type alias shim isn't possible because the return type would differ
- Simplifying to `object` everywhere reduces complexity without functional impact

### Decision 4: NzbParser.ParseAsync - Acceptable Exception  
**What**: The `#if !NETSTANDARD2_0` around `ParseAsync` methods is kept.

**Why**: 
- `XDocument.LoadAsync` doesn't exist on .NET Standard 2.0
- This is API availability, not implementation compatibility
- A sync-over-async polyfill would be an anti-pattern

**Solution**: Keep the `#if` and document that `ParseAsync` is only available on .NET Standard 2.1+. Added XML documentation with `<remarks>` to explain this limitation.

## Risks / Trade-offs

### Risk: NzbParser.ParseAsync availability
**Trade-off**: We could add a sync-over-async polyfill for .NET Standard 2.0, but this is an anti-pattern.

**Decision**: Accept that `NzbParser.ParseAsync(Stream)` and `NzbParser.ParseAsync(TextReader)` are only available on .NET Standard 2.1+. The `#if !NETSTANDARD2_0` around the entire method is acceptable as it controls API availability, not implementation details.

### Risk: Extension method discovery
**Mitigation**: All extension classes are in `Usenet.Extensions` namespace, which is already commonly imported in the codebase.

## Summary of Implemented Changes

| Location | Before | After |
|----------|--------|-------|
| `NntpConnection.cs` | `#if` in `WriteLineInternalAsync` | Uses `StreamWriterExtensions.WriteLineAsync` |
| `YencEncoder.cs` | `#if` in `EncodeAsync` | Uses `StreamExtensions.ReadByteAsync` |
| `NntpClientPool.cs` | `#if NET9_0_OR_GREATER` for Lock | Simplified to `object` on all platforms |
| `NntpStreamReader.cs` | `#if` for method signature | Kept (acceptable exception) |
| `NzbParser.cs` | `#if` for API availability | Kept (acceptable exception) + added docs |

## New Files
- `src/Usenet/Extensions/StreamWriterExtensions.cs`

## Modified Files
- `src/Usenet/Extensions/StreamExtensions.cs` (added `ReadByteAsync`)
