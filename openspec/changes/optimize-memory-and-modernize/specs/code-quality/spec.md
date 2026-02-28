## ADDED Requirements

### Requirement: Memory-Efficient Buffer Management
The library SHALL use `ArrayPool<byte>` for temporary buffer allocations in encoding and decoding operations to reduce garbage collection pressure.

#### Scenario: yEnc encoding uses pooled buffers
- **WHEN** encoding data using the yEnc encoder
- **THEN** temporary buffers SHALL be rented from ArrayPool
- **AND** buffers SHALL be returned to the pool after use

#### Scenario: yEnc decoding uses pooled buffers
- **WHEN** decoding yEnc data using stream or article decoders
- **THEN** temporary buffers SHALL be rented from ArrayPool
- **AND** buffers SHALL be returned to the pool even on exception

### Requirement: Span-Based Hot Path APIs
The library SHALL provide `Span<byte>` and `ReadOnlySpan<byte>` overloads for performance-critical operations to enable zero-allocation processing.

#### Scenario: CRC32 calculation accepts span input
- **WHEN** calculating CRC32 checksum
- **THEN** a `ReadOnlySpan<byte>` overload SHALL be available
- **AND** the overload SHALL not allocate additional memory

#### Scenario: yEnc line decoding accepts span input
- **WHEN** decoding a single yEnc-encoded line
- **THEN** a `ReadOnlySpan<byte>` overload SHALL be available for internal use

### Requirement: Static Separator Caching
Parser implementations SHALL cache string split separator arrays as static readonly fields to avoid repeated allocations.

#### Scenario: NNTP parsers cache separators
- **WHEN** parsing NNTP responses that require string splitting
- **THEN** separator arrays SHALL be defined as private static readonly fields
- **AND** the same separator instance SHALL be reused across all parse calls

### Requirement: Modern C# Patterns
The codebase SHALL use modern C# language patterns for clarity and consistency.

#### Scenario: Null checks use pattern matching
- **WHEN** checking for null values
- **THEN** the `x is null` pattern SHALL be used instead of `(object)x == null`

#### Scenario: Target-typed new expressions
- **WHEN** instantiating objects where the type is apparent from context
- **THEN** the `new()` syntax MAY be used instead of `new ClassName()`

### Requirement: .NET Standard 2.0 Compatibility
All memory optimization features SHALL maintain compatibility with .NET Standard 2.0 through appropriate polyfills and conditional compilation.

#### Scenario: Span APIs work on netstandard2.0
- **WHEN** building for .NET Standard 2.0
- **THEN** Span-based APIs SHALL be available via System.Memory polyfill

#### Scenario: Preprocessor directives in designated locations
- **WHEN** conditional compilation is required for compatibility
- **THEN** `#if` directives SHALL be placed in `Extensions/` or `Util/` namespaces only
