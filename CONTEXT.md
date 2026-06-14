# Usenet

A .NET library for working with Usenet: an NNTP client, an NZB document parser/builder/writer, and a yEnc encoder/decoder. Focused on low memory usage through streaming.

This glossary is the project's canonical vocabulary. When writing docs, commit messages, issues, or new XML doc comments, prefer these terms over the listed alternatives. Type names already in the public API may differ for backwards-compatibility reasons; those exceptions are noted inline.

## Language

**Newsgroup**:
A named group on a Usenet server that articles are posted to. The canonical term in prose and documentation.
_Avoid_: Group, channel, forum. (Note: the public type is named `NntpGroup`/`NntpGroups` for API stability — the type name is the one allowed exception.)

### The posted unit

A large file is split into numbered pieces; each piece is posted to Usenet as one message and identified by a Message-ID. The same piece is named differently depending on which layer you're looking at. All three terms below refer to the same underlying posted message — keep each within its own layer rather than collapsing them.

**Article**:
A single message posted to a newsgroup, identified by a Message-ID. The NNTP-layer term. _Avoid_: post, message (when you mean the NNTP article specifically).

**Segment**:
An NZB document's reference to a single article — one entry under an NZB file, carrying the article's Message-ID and byte offset. The NZB-layer term for the same posted message. _Avoid_: using "article" or "part" when describing NZB structure.

**Part**:
One piece of a file as split for yEnc encoding (`PartNumber` of `TotalParts`). The yEnc-layer term for the same posted message. _Avoid_: using "segment" or "article" when describing yEnc structure.

**Message-ID**:
The unique identifier of an article, the value that ties a segment/part back to its article across all three layers. _Avoid_: message id without the hyphen, article id.

### NZB structure

**NZB document**:
A parsed `.nzb` container — its metadata plus the list of files it references. The canonical term for the whole `.nzb`. _Avoid_: "NZB file" when you mean the document.

**File**:
An original binary file referenced by an NZB document and reconstructed from its segments; also the unit yEnc describes via `FileName`/`FileSize`. _Avoid_: calling the `.nzb` container itself a "file".

**Poster**:
The entity that posted an article. The canonical concept-level term. The `From` header is the specific NNTP header field that carries it. _Avoid_: author, sender.

### NNTP client layering

These four terms are distinct layers; in particular, keep **Connection** and **Client** separate.

**Connection**:
The transport layer — opens the socket to a Usenet server, writes command lines, and reads and parses the typed response. _Avoid_: using "connection" to mean the command API.

**Client**:
The command-oriented API (the RFC command methods) built on top of a connection. _Avoid_: using "client" to mean the raw transport.

**Pool**:
A manager of authenticated, connected clients that can be reused across operations. _Avoid_: cache.

**Lease**:
A client borrowed from the pool for the duration of an operation and returned to the pool when disposed. _Avoid_: rental, checkout.

### Article content

**Body**:
The article body as transmitted — a list of text lines, often still yEnc-encoded. _Avoid_: using "body" for the decoded bytes.

**Data**:
The decoded binary bytes obtained after yEnc-decoding an article body. _Avoid_: using "data" for the raw transmitted body.

### Streamed results

**Overview**:
The summary metadata for an article from the NNTP `OVER`/`XOVER` database — article number, subject, poster, date, message-id, references, byte count and line count. The canonical term for an `XOVER` result. _Avoid_: calling an overview record a "header" — the article's actual headers are the `NntpHeaderCollection`; an overview is derived metadata, not the headers themselves.

**Row**:
A single parsed entry of a streamed multi-line result — one overview, one article number, one newsgroup. The unit yielded by a streamed `IAsyncEnumerable` result. _Avoid_: "line" when you mean the parsed value (a line is the raw on-the-wire text; a row is the typed result parsed from it).
