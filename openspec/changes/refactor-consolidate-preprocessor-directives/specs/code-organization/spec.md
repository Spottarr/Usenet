# Code Organization

## ADDED Requirements

### Requirement: Preprocessor Directive Isolation
All preprocessor directives (`#if`, `#else`, `#endif`) for cross-platform compatibility SHALL be located only in:
- `Usenet.Extensions` namespace (extension methods)
- `Usenet.Util` namespace (utility classes and compatibility shims)

Domain namespaces (`Usenet.Nntp`, `Usenet.Nzb`, `Usenet.Yenc`) SHALL NOT contain preprocessor directives for platform compatibility, except where the directive controls public API availability (e.g., methods that cannot exist on certain platforms).

#### Scenario: Extension method provides cross-platform WriteLineAsync
- **WHEN** code needs to call `StreamWriter.WriteLineAsync` with `CancellationToken`
- **THEN** the caller uses `StreamWriterExtensions.WriteLineAsync` extension method
- **AND** the `#if` directive is contained within the extension method implementation

#### Scenario: Shim class provides cross-platform lock type
- **WHEN** code needs a lock synchronization primitive
- **THEN** the caller uses `LockShims.LockType` type alias
- **AND** the `#if` directive is contained within the shim class

#### Scenario: API availability controlled by preprocessor directive
- **WHEN** an entire public method cannot exist on a target framework due to missing BCL support
- **THEN** the method declaration MAY be wrapped in a preprocessor directive
- **AND** the directive SHALL only control method availability, not implementation details

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

### Requirement: Compatibility Shim Pattern
Compatibility shims for cross-platform type or static method differences SHALL:
- Be placed in the `Usenet.Util.Compatibility` namespace
- Be declared as `internal static` classes
- Follow the naming convention `{TypeName}Shims` (e.g., `LockShims`)

#### Scenario: Lock type shim
- **WHEN** code uses `LockShims.LockType`
- **THEN** on .NET 9+, it resolves to `System.Threading.Lock`
- **AND** on older frameworks, it resolves to `object`
