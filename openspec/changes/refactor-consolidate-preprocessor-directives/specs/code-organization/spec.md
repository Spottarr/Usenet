# Code Organization

## ADDED Requirements

### Requirement: Preprocessor Directive Isolation
All preprocessor directives (`#if`, `#else`, `#endif`) for cross-platform compatibility SHALL be located only in:
- `Usenet.Extensions` namespace (extension methods)
- `Usenet.Util` namespace (utility classes and compatibility shims)

Domain namespaces (`Usenet.Nntp`, `Usenet.Nzb`, `Usenet.Yenc`) SHALL NOT contain preprocessor directives for platform compatibility, with the following acceptable exceptions:
1. Directives that control method signature (e.g., `override` vs `new`)
2. Directives that control public API availability (e.g., methods that cannot exist on certain platforms)

#### Scenario: Extension method provides cross-platform WriteLineAsync
- **WHEN** code needs to call `StreamWriter.WriteLineAsync` with `CancellationToken`
- **THEN** the caller uses `StreamWriterExtensions.WriteLineAsync` extension method
- **AND** the `#if` directive is contained within the extension method implementation

#### Scenario: Extension method provides cross-platform ReadByteAsync
- **WHEN** code needs to call `Stream.ReadAsync` with Memory overload for single byte read
- **THEN** the caller uses `StreamExtensions.ReadByteAsync` extension method
- **AND** the `#if` directive is contained within the extension method implementation

#### Scenario: Method signature controlled by preprocessor directive
- **WHEN** a method must use different signatures on different frameworks (e.g., `override` on .NET 8+ vs `new` on older)
- **THEN** the method declaration MAY use a preprocessor directive for the signature
- **AND** the implementation logic SHOULD be kept consistent or delegated where possible

#### Scenario: API availability controlled by preprocessor directive
- **WHEN** an entire public method cannot exist on a target framework due to missing BCL support
- **THEN** the method declaration MAY be wrapped in a preprocessor directive
- **AND** the directive SHALL only control method availability, not inline implementation details
- **AND** the method SHALL include documentation explaining the availability constraints

### Requirement: Extension Method Pattern
Extension methods for cross-platform compatibility SHALL:
- Be placed in the `Usenet.Extensions` namespace
- Be declared as `internal static` classes
- Follow the naming convention `{TypeName}Extensions` (e.g., `StreamWriterExtensions`)
- Provide the same method signature across all target frameworks

#### Scenario: StreamWriter WriteLineAsync extension
- **WHEN** `StreamWriterExtensions.WriteLineAsync(StreamWriter, string, CancellationToken)` is called
- **THEN** on .NET Standard 2.0, it throws if cancelled and calls the string overload
- **AND** on newer frameworks, it calls the native `Memory<char>` overload with cancellation

#### Scenario: Stream ReadByteAsync extension
- **WHEN** `StreamExtensions.ReadByteAsync(Stream, byte[], CancellationToken)` is called
- **THEN** on .NET Standard 2.0, it throws if cancelled and calls the array overload
- **AND** on newer frameworks, it calls the native `Memory<byte>` overload with cancellation

### Requirement: Compatibility Shim Pattern
Compatibility shims for cross-platform type or static method differences SHALL:
- Be placed in the `Usenet.Util.Compatibility` namespace
- Be declared as `internal static` classes
- Follow the naming convention `{TypeName}Shims` (e.g., `ObjectDisposedExceptionShims`)

#### Scenario: ObjectDisposedException shim
- **WHEN** code calls `ObjectDisposedExceptionShims.ThrowIf(condition, instance)`
- **THEN** on .NET 7+, it delegates to `ObjectDisposedException.ThrowIf`
- **AND** on older frameworks, it throws a new `ObjectDisposedException` if condition is true
