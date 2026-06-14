# CLAUDE.md

Guidance for Claude Code (and any other AI agent or human) working in this repository.

If you are adding documentation, prefer extending the files under `docs/` and updating the index below. Use [Mermaid](https://mermaid.js.org/) for any diagrams or charts — GitHub renders them natively. Do not draw diagrams as ASCII art.

## Key conventions

- **Use idiomatic C# with the latest language features.**

## CI

`.github/workflows/build-and-test.yml` (on push/PR to `main`): `dotnet tool restore` → `dotnet restore` → CSharpier check → `dotnet build` → CodeQL (public repos) → `dotnet test` with cobertura coverage. Test results and coverage are posted as sticky PR comments. A separate scheduled `codeql.yml` re-runs CodeQL weekly.

`.github/workflows/release.yml` (on GitHub release): `dotnet pack` then `dotnet nuget push` to publish the `Spottarr.Usenet` package to NuGet, versioned from the release tag.

## Notes for AI agents

- **Always run CSharpier** (`dotnet csharpier format .`) after writing C# — CI fails otherwise and `TreatWarningsAsErrors=true` will catch a lot too.
- **Don't pin package versions in `.csproj`** — add or update the `PackageVersion` entry in `Directory.Packages.props`.
- **Don't suppress warnings.** Don't use #pragma or SuppressMessageAttribute. Only use .editorconfig and only for global rules. Always try to fix the issue in code first.
- **Make sure to create individual commits that make sense**.
- **Only build/test when neccesary**, these can be long running operations.
- Prefer running targeted tests instead of the full suite.
- Submit changes as a PR when done.

## Agent skills

### Issue tracker

Issues are tracked as GitHub issues in `Spottarr/Usenet` via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Five canonical triage roles, using default label strings (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`CONTEXT.md` + `docs/adr/` at the repo root). See `docs/agents/domain.md`.

### Architecture

`docs/architecture.md` describes the 6.0.0 streaming/buffering architecture with Mermaid diagrams. Backed by `docs/adr/0001`–`0003`.