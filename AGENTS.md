<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->

## Build & Test Commands
- Build: `dotnet build`
- Test all: `dotnet test`
- Single test: `dotnet test --filter "FullyQualifiedName~ClassName.MethodName"`
- Deep clean: `dotnet clean -p:deep=true && dotnet restore`

## Code Style
- C# latest, file-scoped namespaces, 4-space indent, max 160 chars/line
- Private fields: `_camelCase`, constants: `PascalCase`, avoid `this.`
- Prefer `var` when type is apparent, use Allman braces (newline before `{`)
- No primary constructors; use streaming APIs for memory efficiency
- Warnings are errors; all .NET analyzers enabled
