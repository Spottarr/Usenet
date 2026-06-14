# Coding Standards

The reviewer agent loads this file during code review via `@.sandcastle/CODING_STANDARDS.md`
so the standards are enforced during review without costing tokens during implementation.

The authoritative sources are `CLAUDE.md`, `CONTEXT.md`, and `docs/adr/` at the repo
root — read them. The highlights the reviewer should hold the change to:

## Build and formatting

- **Formatting is non-negotiable.** CSharpier is the source of truth; CI fails on any
  deviation. Check with `dotnet csharpier check .` and fix with `dotnet csharpier format .`
  (it reads `.csharpierignore` automatically). The `.editorconfig` keeps a trailing comma
  in multiline lists so the editor does not fight CSharpier.
- **Warnings are errors.** `Directory.Build.props` sets `TreatWarningsAsErrors=true`,
  `Nullable=enable`, and `AnalysisMode=All`. Do **not** suppress warnings with `#pragma`
  or `SuppressMessageAttribute` — fix them in code. Global rules go in `.editorconfig`
  only.
- **Package versions** are centralized in `Directory.Packages.props` via `PackageVersion`
  — never pin a version in a `.csproj`.
- The library multi-targets `net8.0;net10.0`. Keep new code building on both; guard
  framework-specific APIs (and `PackageReference` `Condition`s) the way the existing
  `.csproj` does. `PolySharp` backfills newer language/runtime features on `net8.0`.

## Public API

- This is a published NuGet package (`Spottarr.Usenet`) with the
  `Microsoft.CodeAnalysis.PublicApiAnalyzers` enabled. **Any added or changed public
  surface must be recorded in `src/Usenet/PublicAPI.Unshipped.txt`** or the analyzer
  (RS0016/RS0017) fails the build. Keep the public surface intentional — prefer
  `internal` unless the type/member is meant for consumers. `Usenet.Tests` already sees
  internals via `InternalsVisibleTo`.

## C# style

- **File-scoped namespaces**; `internal` by default.
- **Logging.** Use the source-generated `[LoggerMessage]` partial methods in
  `src/Usenet/Extensions/LogExtensions.cs`, not `ILogger.LogXxx` directly. Add a new
  partial method there rather than logging inline.
- Use idiomatic C# with the latest language features (the repo sets `LangVersion=latest`).
- Prefer clarity over brevity; avoid nested ternaries (use `switch` or `if`/`else`).
- The library is built for **low memory usage** — prefer streaming and incremental
  enumeration over buffering whole articles/files in memory. See
  `docs/architecture.md` and ADRs `0001`–`0003` before changing the
  buffering/streaming model.

## Domain language

- `CONTEXT.md` is the canonical glossary. Use the right term for the layer you are in
  and do not collapse the layers:
  - **Article** (NNTP) / **Segment** (NZB) / **Part** (yEnc) all refer to the same
    posted message — keep each within its own layer.
  - **Connection** (transport) vs **Client** (command API) vs **Pool** vs **Lease** are
    distinct — keep them separate.
  - **Body** (transmitted lines, often yEnc-encoded) vs **Data** (decoded bytes).
  - Prefer **Newsgroup** in prose (the public `NntpGroup` type name is the one allowed
    exception, kept for API stability).

## General

- **US English everywhere** — identifiers, comments, XML doc comments, docs
  (`color`, `behavior`). Avoid gratuitous em-dashes.
- New behavior must be covered by tests (TUnit; mocks via `TUnit.Mocks`). Test data
  lives as embedded resources under `tests/Usenet.Tests/testdata`.
- Commits are concise, imperative, sentence-case summaries (e.g. `Add NZB segment
  validation`) — this repo does **not** use Conventional Commits. Make individual
  commits that each make sense on their own.
