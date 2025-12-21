# Change: Memory Optimization and Code Modernization

## Why
The library performs significant memory allocations in hot paths (yEnc decoding, NNTP parsing) that can be reduced using modern .NET APIs like `Span<T>`, `ArrayPool<T>`, and `ReadOnlySpan<T>`. Additionally, the codebase uses older C# patterns that can be modernized for clarity and consistency.

## What Changes

### Memory Optimizations (High Priority)
- Add `Span<byte>` and `ReadOnlySpan<byte>` overloads to yEnc decoding hot paths
- Replace `byte[]` buffer allocations with `ArrayPool<byte>` rentals in encoding/decoding
- Add `ReadOnlySpan<byte>` overload to `Crc32.cs` for zero-allocation CRC calculation
- Cache `String.Split()` separator arrays as static readonly fields
- Replace string concatenation in loops with `StringBuilder` (header folding)

### Code Modernization (Medium Priority)
- Replace `(object)x == null` with `x is null` pattern matching
- Use target-typed `new()` where type is apparent from context
- Add initial capacity hints to `List<T>` constructors where size is known

### .NET Standard 2.0 Compatibility
- All `Span<T>`/`Memory<T>` usage requires `System.Memory` polyfill on netstandard2.0
- New APIs will use `#if` directives in `Extensions/` and `Util/` namespaces (per established pattern)
- Public API surface remains unchanged - new overloads are additive

## Impact
- Affected specs: code-quality (new)
- Affected code:
  - `Yenc/YencLineDecoder.cs`, `YencStreamDecoder.cs`, `YencArticleDecoder.cs`
  - `Util/Crc32.cs`
  - `Nntp/Parsers/*.cs` (multiple parser files)
  - `Nntp/Models/*.cs` (pattern matching updates)
  - `Nzb/*.cs` (pattern matching updates)
