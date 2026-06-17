# Cancellation tokens and a de-overloaded command API

## Status

accepted (6.0.0)

## Decision

Every command method takes its cancellation token as a trailing optional parameter
(`CancellationToken cancellationToken = default`) rather than shipping a separate
`CancellationToken.None`-forwarding overload. To keep optional parameters legal under the
public-API analyzer **without suppressing it**, each command is a **single, non-overloaded
method**: the article/range/current selectors that previously drove overloads are split into
distinctly named methods.

The naming convention is:

- **Bare verb = the primary selector for that command family.** Message-id for the
  single-article commands (`ArticleAsync`, `HeadAsync`, `BodyAsync`, `StatAsync`); the article
  range for the overview/header scans (`OverAsync`, `XoverAsync`, `HdrAsync`, `XhdrAsync`),
  because the range scan is the hot indexing path.
- **`...ByNumberAsync` / `...ByMessageIdAsync`** for the secondary selector.
- **`Current...Async`** for the current-article-pointer variant (used in `NEXT`/`LAST`
  navigation).

The three `*.Overloads.cs` files and the duplicated declarations across the `INntpClient*`
interfaces are deleted.

## Context

The pre-decision surface carried ~145 `CancellationToken.None` forwarder methods plus a matching
doubling of every interface declaration — roughly 280 members that existed only to provide a
no-token convenience overload. The idiomatic .NET shape is a single method with a trailing
optional `CancellationToken`.

The blocker was `Microsoft.CodeAnalysis.PublicApiAnalyzers` rule **RS0026** ("Do not add multiple
public overloads with optional parameters"). Its recommended approach is to "use the maximal set
of optional parameters in a *single* public overload, and use required parameters for all other
overloads" — i.e. optional parameters are only allowed on a method that has no optional-param
overload siblings. Every command had 2–4 overloads (by message-id / by number / current), so
adding an optional token tripped RS0026 on all of them. (The lone `NntpStreamResponse<T>.GetAsyncEnumerator(CancellationToken = default)`
is accepted precisely because it stands alone.)

## Considered options

- **Suppress RS0026 globally and keep the strongly-typed selector overloads.** A trailing optional
  `CancellationToken` is the BCL-wide exception RS0026 is conservative about, and `.editorconfig`
  is the sanctioned (non-`#pragma`) mechanism. Rejected for 6.0.0 in favor of a code-level fix that
  keeps the analyzer fully on; can be revisited if the naming convention proves awkward.
- **A single `NntpArticleId`/selector struct with implicit conversions** (message-id | number |
  current) so each command stays one method named after the verb. The biggest surface collapse and
  it satisfies RS0026, but the implicit conversions hide the accepted argument shapes at the call
  site, which cuts against the ergonomics goal. Rejected.
- **Distinct method names (chosen).** No overload groups, so optional tokens are legal with the
  analyzer untouched, and each call site is self-documenting.
- **Make the token required everywhere.** Avoids RS0026 trivially but forces every caller to pass
  `CancellationToken.None` for one-off calls. Rejected as poor ergonomics.

## Consequences

- ~280 public members and 559 lines of forwarders disappear; the interface declarations and their
  doc comments halve.
- The "bare verb = primary selector" rule means the bare verb maps to different selector *types*
  across families (message-id for `ArticleAsync`, range for `OverAsync`). This is a deliberate
  ergonomic asymmetry — the bare verb is always the most common call for that command — not an
  inconsistency to be "fixed".
- Renaming the by-number / current variants is a broad breaking change across the client, the
  pooled client, and the connection contracts. Acceptable inside the 6.0.0 break.
- RS0026 stays enabled, so the same constraint applies to any future command: prefer a single
  method with an optional token over an overload set.
