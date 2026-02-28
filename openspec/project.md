# Project Context

## Purpose
A .NET library for working with Usenet. It provides:
- An NNTP client compliant with RFC 2980, RFC 3977, RFC 4643, and RFC 6048
- An NZB file parser, builder, and writer
- A yEnc encoder and decoder

The library is focused on keeping memory usage low through streaming APIs. Server responses can be enumerated as they come in, and binary messages are encoded/decoded in a streaming fashion.

Published as NuGet package: `Spottarr.Usenet`

## Tech Stack
- **Language**: C# (latest language version)
- **Runtime**: .NET 10.0, .NET Standard 2.0, .NET Standard 2.1 (multi-targeting)
- **SDK**: .NET 10.0.100+
- **Testing Framework**: xUnit v3 with Microsoft.Testing.Platform runner
- **Mocking**: NSubstitute
- **CI/CD**: GitHub Actions with CodeQL analysis
- **Package Management**: Central Package Management (Directory.Packages.props)

### Key Dependencies
- `Microsoft.Extensions.Logging.Abstractions` - Logging abstractions
- `Microsoft.Extensions.FileProviders.Abstractions` - File provider abstractions
- `System.Collections.Immutable` - Immutable collections
- `Microsoft.SourceLink.GitHub` - Source linking for debugging

## Project Conventions

### Code Style
- **Indentation**: 4 spaces, no tabs
- **Line Length**: Max 160 characters
- **Braces**: Allman style (new line before open brace)
- **Namespaces**: File-scoped namespace declarations (`namespace Foo;`)
- **Usings**: Outside namespace, System directives first
- **`var` usage**: Preferred when type is apparent
- **`this.` qualifier**: Avoid unless absolutely necessary
- **Primary constructors**: Disabled (IDE0290 suppressed)
- **Trailing whitespace**: Trimmed
- **Final newline**: Required

### Naming Conventions
- **Constants**: PascalCase
- **Private/internal fields**: `_camelCase` (underscore prefix)
- **Public members**: PascalCase
- **Parameters/locals**: camelCase

### Architecture Patterns
- **Builders**: Use builder pattern for constructing complex objects (e.g., `NntpArticleBuilder`, `NzbBuilder`)
- **Parsers**: Dedicated parser classes for parsing formats (e.g., `NzbParser`, response parsers)
- **Streaming**: Prefer streaming APIs over loading entire content into memory
- **Immutable models**: Use immutable collections where appropriate
- **Contracts**: Define interfaces in `Contracts/` folder for dependency injection
- **Resources**: Use `.resx` files for localizable strings and error messages

### Project Structure
```
src/Usenet/           # Main library
├── Exceptions/       # Custom exception types
├── Extensions/       # Extension methods
├── Nntp/             # NNTP client implementation
│   ├── Builders/     # Article/group builders
│   ├── Contracts/    # Interfaces
│   ├── Models/       # Data models
│   ├── Parsers/      # Response parsers
│   ├── Responses/    # Response types
│   └── Writers/      # Article writers
├── Nzb/              # NZB parsing/building/writing
├── Resources/        # Resource files (.resx)
├── Util/             # Utility classes and compatibility shims
└── Yenc/             # yEnc encoding/decoding

tests/Usenet.Tests/   # Test project
├── testdata/         # Test data files (embedded resources)
├── TestHelpers/      # Test utilities and helpers
└── [mirrors src structure]
```

### Testing Strategy
- **Framework**: xUnit v3 with `[Fact]` and `[Theory]` attributes
- **Mocking**: NSubstitute for mocking interfaces
- **Test Data**: Embedded resources for test files (NZB, yEnc samples)
- **Test Naming**: `MethodOrScenario_Condition_ExpectedResult` pattern
- **Visibility**: Tests can access internals via `InternalsVisibleTo`
- **Custom Attributes**: `EmbeddedResourceDataAttribute` for loading test data

### Git Workflow
- **Main branch**: `main`
- **CI**: Runs on push/PR to main
- **Build verification**: `dotnet restore` → `dotnet build` → `dotnet test`
- **Code analysis**: CodeQL security scanning enabled
- **Releases**: Automated via GitHub Actions (release.yml)

## Domain Context

### Usenet Protocols & Formats
- **NNTP (Network News Transfer Protocol)**: Protocol for reading/posting articles to newsgroups
- **NZB**: XML format describing files split into articles on Usenet (like a .torrent for Usenet)
- **yEnc**: Binary-to-text encoding for Usenet articles (more efficient than uuencode)

### Key Concepts
- **Article**: A single message/post on Usenet, identified by Message-ID
- **Segment**: A part of a larger file, each segment is one article
- **Newsgroup**: A discussion category (e.g., `alt.binaries.test`)
- **Message-ID**: Unique identifier for an article (e.g., `<unique-id@domain.com>`)

### RFC Compliance
- RFC 2980: NNTP extensions
- RFC 3977: NNTP core protocol
- RFC 4643: NNTP authentication
- RFC 6048: NNTP additions

## Important Constraints
- **Memory efficiency**: Streaming APIs are critical - avoid loading large content into memory
- **Multi-target compatibility**: Must work on .NET Standard 2.0 (includes compatibility shims in `Util/Compatibility/`)
- **Warnings as errors**: All warnings are treated as errors (`TreatWarningsAsErrors=true`)
- **Analysis level**: Full .NET analyzers enabled with `AnalysisMode=All`

## External Dependencies
- **Usenet servers**: NNTP servers (e.g., Eweka, Giganews, etc.) for testing real connections
- **NuGet**: Package published to nuget.org as `Spottarr.Usenet`
- **GitHub**: Repository at https://github.com/Spottarr/Usenet
