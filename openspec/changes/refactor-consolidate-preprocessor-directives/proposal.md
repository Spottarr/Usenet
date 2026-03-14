# Change: Consolidate Preprocessor Directives to Extensions/Util Namespaces

## Why
Preprocessor directives (`#if`) are currently scattered across multiple namespaces (Nntp, Nzb, Yenc), making the codebase harder to maintain and reason about. Consolidating all compatibility code into `Usenet.Extensions` and `Usenet.Util` namespaces centralizes platform-specific logic, making it easier to:
- Find and update compatibility code when target frameworks change
- Keep domain code (NNTP, NZB, yEnc) clean and focused on business logic
- Follow consistent patterns already established in `QueueExtensions` and `Util/Compatibility/`

## What Changes

### Files with `#if` directives to be refactored:

| File | Current `#if` Usage | Proposed Solution |
|------|---------------------|-------------------|
| `Nntp/NntpConnection.cs` | `WriteLineAsync` Memory overload | Create `StreamWriterExtensions.WriteLineAsync` |
| `Nntp/NntpStreamReader.cs` | `ReadLineAsync` with CancellationToken | Move to `StreamReaderExtensions` |
| `Nntp/NntpClientPool.cs` | `Lock` vs `object` for locking | Create `LockShims` in Util/Compatibility |
| `Nzb/NzbParser.cs` | `XDocument.LoadAsync` availability | Create `XDocumentExtensions` |
| `Yenc/YencEncoder.cs` | `Stream.ReadAsync` Memory overload | Create `StreamExtensions.ReadAsync` |

### Files already compliant (no changes needed):
- `Extensions/QueueExtensions.cs`
- `Extensions/StreamReaderExtensions.cs`
- `Extensions/DictionaryExtensions.cs`
- `Extensions/StringExtensions.cs`
- `Extensions/TcpClientExtensions.cs`
- `Util/Compatibility/*` (all shim files)
- `Util/Guard.cs`

## Impact
- **Affected namespaces**: `Usenet.Nntp`, `Usenet.Nzb`, `Usenet.Yenc`
- **New files**: 
  - `Extensions/StreamWriterExtensions.cs`
  - `Extensions/StreamExtensions.cs`
  - `Extensions/XDocumentExtensions.cs`
  - `Util/Compatibility/LockShims.cs`
- **No breaking changes**: Internal refactoring only, public API unchanged
- **No behavioral changes**: Same runtime behavior, just reorganized code
