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

**Examples**:
- `StreamWriterExtensions.WriteLineAsync(this StreamWriter, string, CancellationToken)`
- `StreamExtensions.ReadAsync(this Stream, byte[], CancellationToken)` 
- `XDocumentExtensions.LoadAsync(Stream, CancellationToken)`

### Decision 2: Shim classes for static/type compatibility
**What**: Use static shim classes in `Usenet.Util.Compatibility` for type aliases or static method wrappers.

**Why**: Follows the pattern of `ObjectDisposedExceptionShims`, `IntShims`, etc.

**Example**: `LockShims` to provide a cross-platform lock type alias.

### Decision 3: NntpStreamReader special case
**What**: The `NntpStreamReader.ReadLineAsync(CancellationToken)` cannot be moved to an extension method because it overrides a base class method on .NET 8+.

**Why**: On .NET 8+, `StreamReader.ReadLineAsync(CancellationToken)` exists and must be overridden. On older frameworks, we add a new method.

**Solution**: Keep the `#if` in `NntpStreamReader` but wrap the actual async call in `StreamReaderExtensions.ReadLineWithCancellationAsync` so the directive only controls the method signature, not the implementation.

### Decision 4: NzbParser.ParseAsync conditional compilation
**What**: The `NzbParser.ParseAsync` methods are wrapped in `#if !NETSTANDARD2_0` because `XDocument.LoadAsync` doesn't exist on .NET Standard 2.0.

**Why**: This is an API availability issue, not an implementation compatibility issue.

**Solution**: Create `XDocumentExtensions` that provides a polyfill `LoadAsync` on .NET Standard 2.0 (using sync fallback with Task.Run), allowing the methods to exist on all platforms. Alternatively, keep the conditional compilation on the public API but document this clearly.

**Trade-off**: Adding a sync-over-async fallback could cause performance issues. The cleaner solution is to accept that these methods are only available on .NET Standard 2.1+. Keep the `#if` but move it to wrap only the method declaration, not inline in implementation code.

## Risks / Trade-offs

### Risk: NzbParser.ParseAsync availability
**Trade-off**: We could add a sync-over-async polyfill for .NET Standard 2.0, but this is an anti-pattern.

**Decision**: Accept that `NzbParser.ParseAsync(Stream)` and `NzbParser.ParseAsync(TextReader)` are only available on .NET Standard 2.1+. The `#if !NETSTANDARD2_0` around the entire method is acceptable as it controls API availability, not implementation details.

### Risk: Extension method discovery
**Mitigation**: All extension classes are in `Usenet.Extensions` namespace, which is already commonly imported in the codebase.

## Summary of Changes

| Location | Pattern | New File |
|----------|---------|----------|
| `NntpConnection.WriteLineInternalAsync` | Extension method | `StreamWriterExtensions.cs` |
| `NntpStreamReader.ReadLineAsync` | Keep `#if` for signature, move impl to extension | `StreamReaderExtensions.cs` (modify) |
| `NntpClientPool._lock` | Type alias shim | `LockShims.cs` |
| `YencEncoder.EncodeAsync` | Extension method | `StreamExtensions.cs` |
| `NzbParser.ParseAsync` | Keep as-is (API availability) | N/A |
