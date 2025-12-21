# Tasks: Memory Optimization and Code Modernization

## 1. Compatibility Shims
- [ ] 1.1 Create `Util/Compatibility/SpanShims.cs` with Span helper extensions for netstandard2.0
- [ ] 1.2 Add `System.Memory` package reference to project file (if not present)
- [ ] 1.3 Verify build succeeds on all target frameworks

## 2. Code Modernization (Pattern Matching)
- [ ] 2.1 Replace `(object)x == null` with `x is null` in `Nntp/Models/NntpArticle.cs`
- [ ] 2.2 Replace `(object)x == null` with `x is null` in `Nntp/Models/NntpGroup.cs`
- [ ] 2.3 Replace `(object)x == null` with `x is null` in `Nntp/Models/NntpMessageId.cs`
- [ ] 2.4 Replace `(object)x == null` with `x is null` in `Nzb/NzbDocument.cs`
- [ ] 2.5 Replace `(object)x == null` with `x is null` in remaining files

## 3. Code Modernization (Target-Typed New)
- [ ] 3.1 Update `new ClassName()` to `new()` where type is apparent from declaration
- [ ] 3.2 Focus on model classes, builders, and parsers

## 4. Static Separator Caching
- [ ] 4.1 Cache split separators in `Nntp/Parsers/GroupsParser.cs`
- [ ] 4.2 Cache split separators in `Nntp/Parsers/ArticleResponseParser.cs`
- [ ] 4.3 Cache split separators in `Nntp/Parsers/GroupResponseParser.cs`
- [ ] 4.4 Cache split separators in `Nntp/Parsers/GroupsResponseParser.cs`
- [ ] 4.5 Cache split separators in `Yenc/YencMeta.cs`

## 5. StringBuilder Optimization
- [ ] 5.1 Replace string concatenation loop in `ArticleResponseParser.cs` header folding with StringBuilder

## 6. Crc32 Span Overload
- [ ] 6.1 Add `ReadOnlySpan<byte>` overload to `Util/Crc32.cs`
- [ ] 6.2 Update internal callers to use span overload where applicable
- [ ] 6.3 Add unit tests for new overload

## 7. List Capacity Hints
- [ ] 7.1 Add initial capacity to `List<T>` constructors where size is known or estimable
- [ ] 7.2 Focus on parsers and builders that create lists from known-size sources

## 8. ArrayPool Integration
- [ ] 8.1 Create `Extensions/ArrayPoolExtensions.cs` with rent/return helpers
- [ ] 8.2 Update `Yenc/YencEncoder.cs` to use ArrayPool for encoding buffers
- [ ] 8.3 Update `Yenc/YencStreamDecoder.cs` to use ArrayPool for decoding buffers
- [ ] 8.4 Update `Yenc/YencArticleDecoder.cs` to use ArrayPool where applicable

## 9. Span-Based yEnc Decoding
- [ ] 9.1 Add `ReadOnlySpan<byte>` overload to `YencLineDecoder.Decode()`
- [ ] 9.2 Update internal callers to use span overload
- [ ] 9.3 Add unit tests for new overloads

## 10. Final Validation
- [ ] 10.1 Run full test suite: `dotnet test`
- [ ] 10.2 Verify build on all targets: `dotnet build`
- [ ] 10.3 Review for any remaining allocation opportunities
