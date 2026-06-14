# TASK

Fix issue {{TASK_ID}}: {{ISSUE_TITLE}}

Pull in the issue using `gh issue view <ID>`. If it has a parent PRD, pull that in too.

Only work on the issue specified.

Work on branch {{BRANCH}}. Make commits and run tests.

# CONTEXT

Here are the last 10 commits:

<recent-commits>

!`git log -n 10 --format="%H%n%ad%n%B---" --date=short`

</recent-commits>

# EXPLORATION

Explore the repo and fill your context window with relevant information that will allow you to complete the task.

Read `CLAUDE.md` for conventions, `CONTEXT.md` for the canonical domain glossary, and
the relevant ADRs under `docs/adr/` (plus `docs/architecture.md`) before touching the
streaming/buffering model. Pay extra attention to test files that touch the relevant
parts of the code.

# EXECUTION

Follow the repo conventions in `CLAUDE.md` and the domain language in `CONTEXT.md`. Use
the right term for the layer you are in — Article (NNTP) / Segment (NZB) / Part (yEnc)
are the same posted message but must not be collapsed.

Use red-green-refactor to complete the task (the test suite is TUnit, mocks via
`TUnit.Mocks`):

1. RED: write one test
2. GREEN: write the implementation to pass that test
3. REPEAT until done
4. REFACTOR the code

Logging goes through the source-generated `[LoggerMessage]` partial methods in
`src/Usenet/Extensions/LogExtensions.cs`, not `ILogger.LogXxx` directly. If you add or
change any **public** API, record it in `src/Usenet/PublicAPI.Unshipped.txt` — the
public-API analyzer fails the build otherwise.

# FEEDBACK LOOPS

Before committing, run the full check sequence and make sure it passes (the library
multi-targets `net8.0;net10.0`, so the build must be clean on both):

- `dotnet csharpier check .` (CI fails on any deviation; run `dotnet csharpier format .` to fix)
- `dotnet build --no-restore` (`TreatWarningsAsErrors=true` and `AnalysisMode=All`, so warnings and analyzer findings fail the build)
- `dotnet test --no-build`

Do not suppress warnings with `#pragma` or `SuppressMessageAttribute` — fix them in
code. Add package versions in `Directory.Packages.props`, never pin them in a `.csproj`.

# COMMIT

Make a git commit with a concise, imperative, sentence-case summary (e.g. `Add NZB
segment validation`). This repo does **not** use Conventional Commits — match the
existing history. Reference the issue in the body (`Refs: #NN`), not the subject. Use
the body for key decisions and any notes for the next iteration.

# THE ISSUE

If the task is not complete, leave a comment on the issue with what was done.

Do not close the issue - this will be done later.

Once complete, output <promise>COMPLETE</promise>.

# FINAL RULES

ONLY WORK ON A SINGLE TASK.
