# TASK

Review the code changes on branch `{{BRANCH}}` and improve code clarity, consistency, and maintainability while preserving exact functionality.

# CONTEXT

## Branch diff

!`git diff {{SOURCE_BRANCH}}...{{BRANCH}}`

## Commits on this branch

!`git log {{SOURCE_BRANCH}}..{{BRANCH}} --oneline`

# REVIEW PROCESS

1. **Understand the change**: Read the diff and commits above to understand the intent.

2. **Analyze for improvements**: Look for opportunities to:
   - Reduce unnecessary complexity and nesting
   - Eliminate redundant code and abstractions
   - Improve readability through clear variable and function names
   - Consolidate related logic
   - Remove unnecessary comments that describe obvious code
   - Avoid nested ternary operators - prefer switch statements or if/else chains
   - Choose clarity over brevity - explicit code is often better than overly compact code

3. **Check correctness**:
   - Does the implementation match the intent? Are edge cases handled?
   - Are new/changed behaviors covered by tests (TUnit)?
   - Does it hold to the library's low-memory goal — streaming and incremental
     enumeration rather than buffering whole articles/files? (See `docs/architecture.md`
     and ADRs `0001`–`0003`.)
   - Does any added/changed **public** API have a matching entry in
     `src/Usenet/PublicAPI.Unshipped.txt`?
   - Does the change introduce injection vulnerabilities, credential leaks, or other security issues?

4. **Maintain balance**: Avoid over-simplification that could:
   - Reduce code clarity or maintainability
   - Create overly clever solutions that are hard to understand
   - Combine too many concerns into single methods or types
   - Remove helpful abstractions that improve code organization
   - Make the code harder to debug or extend

5. **Apply project standards**: Follow the coding standards defined in @.sandcastle/CODING_STANDARDS.md.
   In particular, check the domain language against `CONTEXT.md` (Article/Segment/Part,
   Connection/Client/Pool/Lease, Body/Data are distinct per layer) and confirm logging
   goes through the `[LoggerMessage]` partial methods in
   `src/Usenet/Extensions/LogExtensions.cs`.

6. **Preserve functionality**: Never change what the code does - only how it does it. All original features, outputs, and behaviors must remain intact.

# EXECUTION

If you find improvements to make:

1. Make the changes directly on this branch
2. Verify nothing is broken (the library multi-targets `net8.0;net10.0`):
   - `dotnet csharpier check .`
   - `dotnet build --no-restore`
   - `dotnet test --no-build`
3. Commit the refinements with a concise, imperative, sentence-case summary (e.g.
   `Simplify yEnc part enumeration`). This repo does not use Conventional Commits.

If the code is already clean and well-structured, do nothing.

Once complete, output <promise>COMPLETE</promise>.
