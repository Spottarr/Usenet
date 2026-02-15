# Design: Memory Optimization and Code Modernization

## Context
The Usenet library processes large binary data (yEnc encoded articles) and parses text-based protocols (NNTP). Current implementation allocates temporary buffers and strings in hot paths. Modern .NET provides `Span<T>`, `Memory<T>`, and `ArrayPool<T>` to reduce allocations.

### Constraints
- Must maintain .NET Standard 2.0 support (polyfills via `System.Memory` package)
- All `#if` directives must be placed in `Extensions/` or `Util/` namespaces (per project convention)
- Public API surface must remain backward compatible
- Warnings are treated as errors

## Goals
- Reduce allocations in yEnc decoding hot paths
- Eliminate unnecessary string allocations in parsers
- Modernize C# patterns for consistency and clarity
- Maintain full backward compatibility

## Non-Goals
- Changing public API signatures (only adding overloads)
- Rewriting core algorithms
- Adding new features beyond performance improvements

## Decisions

### Decision 1: Span<T> Strategy
Use `Span<byte>` and `ReadOnlySpan<byte>` overloads for internal hot paths. Create shim extension methods in `Util/Compatibility/` for .NET Standard 2.0 compatibility.

**Rationale**: Span-based APIs avoid array allocations and enable stack-based processing. The `System.Memory` package provides polyfills for older targets.

**Alternatives Considered**:
- ArraySegment<byte>: Less ergonomic, doesn't reduce allocations as effectively
- Memory<byte> only: Would work but Span is more appropriate for synchronous hot paths

### Decision 2: ArrayPool Usage Pattern
Rent buffers using `ArrayPool<byte>.Shared.Rent()` and return in `finally` blocks. Create a helper extension method `RentBuffer()` in `Extensions/` to encapsulate the pattern.

**Rationale**: ArrayPool reduces GC pressure for frequently allocated buffers. Using `finally` ensures buffers are returned even on exceptions.

**Pattern**:
```csharp
// In Util/Compatibility/ArrayPoolShims.cs
internal static class ArrayPoolShims
{
#if NETSTANDARD2_0
    // Use System.Buffers package
    internal static byte[] Rent(int minimumLength) => ArrayPool<byte>.Shared.Rent(minimumLength);
    internal static void Return(byte[] array) => ArrayPool<byte>.Shared.Return(array);
#else
    internal static byte[] Rent(int minimumLength) => ArrayPool<byte>.Shared.Rent(minimumLength);
    internal static void Return(byte[] array) => ArrayPool<byte>.Shared.Return(array);
#endif
}
```

### Decision 3: Static Separator Caching
Cache `char[]` and `string[]` separators used in `String.Split()` as `private static readonly` fields.

**Rationale**: Avoids allocating new arrays on every parse call. Pattern is simple and has no compatibility concerns.

**Example**:
```csharp
// Before
var parts = line.Split(new[] { ' ' }, 2);

// After
private static readonly char[] SpaceSeparator = { ' ' };
var parts = line.Split(SpaceSeparator, 2);
```

### Decision 4: Pattern Matching Updates
Replace `(object)x == null` with `x is null` throughout the codebase.

**Rationale**: Modern C# pattern matching is clearer and avoids the cast. Compatible with all target frameworks.

### Decision 5: Target-Typed New
Use `new()` instead of `new ClassName()` where type is apparent from variable declaration or return type.

**Rationale**: Reduces verbosity without sacrificing clarity. Compatible with C# 9+ (available on all targets via LangVersion).

## Implementation Order

1. **Shim Layer First**: Create compatibility shims in `Util/Compatibility/`
2. **Low-Risk Modernization**: Pattern matching, target-typed new (no behavioral changes)
3. **Crc32 Span Overload**: Add `ReadOnlySpan<byte>` overload
4. **Static Separator Caching**: Cache split arrays
5. **StringBuilder for Header Folding**: Replace string concatenation
6. **ArrayPool in yEnc**: Add buffer pooling to encoders/decoders
7. **Span-Based yEnc Decoding**: Add span overloads to hot paths

## Risks / Trade-offs

| Risk | Impact | Mitigation |
|------|--------|------------|
| Behavioral changes in edge cases | Medium | Comprehensive test coverage exists |
| Buffer not returned on exception | Low | Use `finally` blocks consistently |
| Span APIs more complex | Low | Encapsulate in helper methods |

## Migration Plan
1. All changes are additive (new overloads, internal refactoring)
2. No breaking changes to public API
3. Existing tests must continue to pass
4. No migration required for consumers

## Open Questions
- None currently identified
