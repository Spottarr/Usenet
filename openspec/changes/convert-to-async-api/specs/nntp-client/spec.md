# NNTP Client Async API

## ADDED Requirements

### Requirement: Async NNTP Connection
The `INntpConnection` interface SHALL provide async methods for all network operations.

#### Scenario: Async command execution
- **WHEN** a command is sent via `CommandAsync<T>`
- **THEN** the method returns `Task<T>` that completes when the response is fully received
- **AND** the calling thread is not blocked during network I/O
- **AND** `.ConfigureAwait(false)` is used on all internal awaits

#### Scenario: Async multi-line command execution
- **WHEN** a multi-line command is sent via `MultiLineCommandAsync<T>`
- **THEN** the method returns `Task<T>` that completes when all response lines are read
- **AND** the response data is fully consumed from the network stream before returning
- **AND** the connection is ready for the next command

#### Scenario: Cancellation support
- **WHEN** a `CancellationToken` is passed to an async method
- **AND** the token is cancelled
- **THEN** the operation throws `OperationCanceledException`

### Requirement: Async NNTP Client Methods
All `INntpClient` methods SHALL be async and return `Task<T>`.

#### Scenario: Async authentication
- **WHEN** `AuthenticateAsync` is called with valid credentials
- **THEN** a `Task<bool>` is returned that completes with `true` on success

#### Scenario: Async article retrieval
- **WHEN** `ArticleAsync` is called with a message ID
- **THEN** a `Task<NntpArticleResponse>` is returned
- **AND** the response contains the complete article (headers and body fully loaded)

#### Scenario: Async head retrieval
- **WHEN** `HeadAsync` is called with a message ID
- **THEN** a `Task<NntpArticleResponse>` is returned
- **AND** the response contains only headers (body is empty, as per NNTP protocol)

#### Scenario: Async body retrieval
- **WHEN** `BodyAsync` is called with a message ID
- **THEN** a `Task<NntpArticleResponse>` is returned
- **AND** the response contains only body lines (headers are empty, as per NNTP protocol)

#### Scenario: Async article posting
- **WHEN** `PostAsync` is called with an article
- **THEN** a `Task<bool>` is returned that completes when the server confirms posting

#### Scenario: Connection integrity after command
- **WHEN** any async command completes
- **THEN** all response data has been read from the network stream
- **AND** the connection is in a clean state for subsequent commands

### Requirement: Eager Response Loading
All multi-line responses SHALL be fully loaded before the async method returns.

#### Scenario: Article body fully loaded
- **WHEN** `ArticleAsync` completes
- **THEN** the `NntpArticle.Body` property contains all body lines
- **AND** no data remains unread in the network stream

#### Scenario: Group list fully loaded
- **WHEN** `ListActiveAsync` or similar group listing method completes
- **THEN** the response contains all group entries
- **AND** no data remains unread in the network stream

### Requirement: Async yEnc Decoding
The `YencStreamDecoder` SHALL provide async decode method.

#### Scenario: Async decode from article body
- **WHEN** `DecodeAsync` is called with article body lines
- **THEN** a `Task<YencStream>` is returned
- **AND** the yEnc data is decoded into the stream

### Requirement: Async yEnc Encoding  
The `YencEncoder` SHALL provide async encode method.

#### Scenario: Async encode from stream
- **WHEN** `EncodeAsync` is called with a source stream and header
- **THEN** a `Task<IReadOnlyList<string>>` is returned with encoded lines

## MODIFIED Requirements

### Requirement: NZB Parsing
The `NzbParser` class SHALL provide async parsing methods for stream-based input.

#### Scenario: Sync parsing from string (unchanged)
- **WHEN** `Parse(string)` is called with NZB XML
- **THEN** the NZB document is parsed synchronously
- **AND** an `NzbDocument` is returned

#### Scenario: Async parsing from stream
- **WHEN** `ParseAsync(Stream)` is called
- **THEN** the NZB document is parsed asynchronously
- **AND** a `Task<NzbDocument>` is returned

### Requirement: NZB Writing
The `NzbWriter` class SHALL provide only async writing capability.

#### Scenario: Async write (existing behavior)
- **WHEN** `WriteAsync` is called with an NZB document
- **THEN** the document is written asynchronously to the underlying stream

## REMOVED Requirements

### Requirement: Synchronous NNTP Client Methods
**Reason**: All client methods are converted to async equivalents. Synchronous network I/O blocks threads and does not scale.
**Migration**: Change all method calls to use `await` and propagate `async` up the call stack.

### Requirement: Synchronous NZB Write
**Reason**: The `NzbWriter.Write` synchronous method is removed to minimize API surface. Only `WriteAsync` remains.
**Migration**: Change `Write(doc)` calls to `await WriteAsync(doc)`.

### Requirement: Synchronous yEnc Operations
**Reason**: `YencStreamDecoder.Decode` and `YencEncoder.Encode` synchronous methods are removed.
**Migration**: Use `DecodeAsync` and `EncodeAsync` equivalents.
