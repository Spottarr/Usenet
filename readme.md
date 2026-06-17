# Usenet

A .NET library for working with [Usenet](https://en.wikipedia.org/wiki/Usenet). It offers:
* an [NNTP](https://en.wikipedia.org/wiki/Network_News_Transfer_Protocol) client
* an [NZB](https://en.wikipedia.org/wiki/NZB) document parser, builder and writer
* a [yEnc](https://en.wikipedia.org/wiki/YEnc) encoder and decoder

It is built around a byte-oriented, [`System.IO.Pipelines`](https://learn.microsoft.com/dotnet/standard/io/pipelines)
transport that keeps allocations low: article bytes are framed off the wire without being
transcoded to strings, yEnc parts decode into pooled buffers, and yEnc encoding streams straight
into an `IBufferWriter<byte>`. The NNTP client is compliant with
[RFC 2980](https://tools.ietf.org/html/rfc2980), [RFC 3977](https://tools.ietf.org/html/rfc3977),
[RFC 4643](https://tools.ietf.org/html/rfc4643) and [RFC 6048](https://tools.ietf.org/html/rfc6048).

[![Nuget](https://img.shields.io/nuget/v/Spottarr.Usenet)](https://www.nuget.org/packages/Spottarr.Usenet)
[![Nuget Prerelease](https://img.shields.io/nuget/vpre/Spottarr.Usenet?label=nuget%20prerelease)](https://www.nuget.org/packages/Spottarr.Usenet)

## Install

```zsh
dotnet add package Spottarr.Usenet
```

## Usage

### Connect and authenticate

```csharp
var client = new NntpClient(new NntpConnection());
await client.ConnectAsync(hostname, port, useSsl);
await client.AuthenticateAsync(username, password);
```

### Retrieve an article

An article response owns a pooled buffer, so dispose it (`using`/`await using`). Read the body as
raw bytes via `Body`, or as text lines on demand via `ReadBodyLines()`:

```csharp
await using var response = await client.ArticleAsync(messageId);
if (response.Success)
{
    foreach (var line in response.ReadBodyLines())
    {
        // ...
    }
}
```

To inspect headers without paying to transfer the body, use `HeadAsync` first and issue a
conditional `BodyAsync` only when needed.

### Stream an overview range

Unbounded scans (`XOVER`/`OVER`, `HDR`, `LISTGROUP`, `NEWNEWS`, …) stream typed rows as an
`IAsyncEnumerable<T>`, so memory stays flat over arbitrarily large ranges. Enumerate the result
fully, or dispose it, before issuing the next command on the connection:

```csharp
await client.GroupAsync("alt.binaries.example");

// OverAsync (RFC 3977) and XoverAsync (legacy) stream the same typed rows; servers implement one or
// the other.
await using var overviews = await client.OverAsync(NntpArticleRange.Range(1000, 2000));
await foreach (var overview in overviews)
{
    Console.WriteLine($"{overview.Number}\t{overview.Subject}");
}
```

The by-message-id forms address a single article, so they return one record directly instead of a
stream — `OverByMessageIdAsync` yields `NntpArticleOverview?` and
`HdrByMessageIdAsync`/`XhdrByMessageIdAsync` yield `NntpHeaderField?` (`null` when the article is
absent), with no enumerate-or-dispose contract to honour:

```csharp
var overview = await client.OverByMessageIdAsync(messageId);
if (overview is not null)
{
    Console.WriteLine($"{overview.Subject}\t{overview.Bytes} bytes");
}
```

### Decode a yEnc part

`YencDecoder.Decode` returns a `YencPart` that owns a buffer rented from `ArrayPool`. Its `Data`
view is valid until the part is disposed, and the per-part `pcrc32` (or `crc32` for a single-part
file) is verified during decoding:

```csharp
// encoded holds the raw yEnc bytes of one part (ReadOnlyMemory<byte> or ReadOnlySequence<byte>)
using var part = YencDecoder.Decode(encoded);

YencHeader header = part.Header;
ReadOnlyMemory<byte> data = part.Data;

using var file = File.Open(header.FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
file.Position = header.PartOffset;
await file.WriteAsync(data);
```

### Download an NZB document

Parse the document, then for each segment retrieve the article, decode its body, and write the
decoded part to the file at its offset. Only one article is held in memory at a time:

```csharp
var nzbDocument = await NzbParser.ParseAsync(await File.ReadAllTextAsync(nzbPath));

foreach (var nzbFile in nzbDocument.Files)
{
    foreach (var segment in nzbFile.Segments)
    {
        await using var response = await client.BodyAsync(segment.MessageId);
        if (!response.Success)
            continue;

        using var part = YencDecoder.Decode(response.Body);
        var header = part.Header;

        using var file = File.Open(header.FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
        if (file.Length == 0)
            file.SetLength(header.FileSize); // pre-allocate on first segment

        file.Position = header.PartOffset;
        await file.WriteAsync(part.Data);
    }
}
```

### Build and post an article

```csharp
var messageId = $"{Guid.NewGuid()}@example.net";

var article = new NntpArticleBuilder()
    .SetMessageId(messageId)
    .SetFrom("Random poster <randomposter@example.net>")
    .SetSubject("Random test post #1")
    .AddGroups("alt.test.clienttest", "alt.test")
    .AddLine("This is a message with id " + messageId)
    .AddLine("with multiple lines")
    .Build();

await client.PostAsync(article);
```

### Encode yEnc into a buffer

The encoder reads the source in blocks and writes straight into an `IBufferWriter<byte>` through a
precomputed escape table:

```csharp
var writer = new ArrayBufferWriter<byte>();
await YencEncoder.EncodeAsync(header, stream, writer);
ReadOnlyMemory<byte> encodedBytes = writer.WrittenMemory;
```

### Build and write an NZB document

```csharp
var fileProvider = new PhysicalFileProvider(Path.GetFullPath("testdata"));

var builder = new NzbBuilder()
    .AddGroups("alt.test.clienttest")
    .SetMessageBase("random.local")
    .SetPartSize(50_000)
    .SetPoster("random poster <random.poster@random.com>")
    .AddMetaData("title", "Testing upload Pictures.rar");

foreach (var fileName in fileNames)
{
    builder.AddFile(fileProvider.GetFileInfo(fileName));
}

var nzbDocument = builder.Build();

using var file = File.Create("Pictures.nzb");
await using var writer = new StreamWriter(file, UsenetEncoding.Default);
await writer.WriteNzbDocumentAsync(nzbDocument);
```

### Connection pooling

`NntpClientPool` manages a set of authenticated, connected clients and hands them out as
disposable leases. Connecting and authenticating happen lazily the first time a client is
borrowed, and idle clients are disconnected automatically:

```csharp
using var pool = new NntpClientPool(
    maxPoolSize: 10, hostname, port, useSsl, username, password);

using var lease = await pool.GetLease();
await using var response = await lease.Client.ArticleAsync(messageId);
// the client is returned to the pool when the lease is disposed
```

### Logging

Logging is optional and flows through `Microsoft.Extensions.Logging`. Hand the components an
`ILoggerFactory` directly, or register them with DI and they resolve one from the container:

```csharp
// direct
var client = new NntpClient(new NntpConnection(loggerFactory), loggerFactory);

// dependency injection
services.AddUsenet();
```

### Close the connection

```csharp
await client.QuitAsync();
```

## Architecture

The library is split into independent layers — **Connection** (transport), **Client** (the RFC
command API), **Pool** (reusable authenticated clients), and the independent **yEnc** and **NZB**
codecs. The streaming/buffering model and the reasoning behind it are documented for maintainers in
[docs/architecture.md](docs/architecture.md), backed by the [ADRs](docs/adr/).

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Acknowledgments

This is a standalone library. It descends from [keimpema/Usenet](https://github.com/keimpema/Usenet),
which was in turn based on [Kristian Hellang](https://github.com/khellang)'s work:
* [khellang/yEnc](https://github.com/khellang/yEnc)
* [khellang/NntpLib.Net](https://github.com/khellang/NntpLib.Net)
* [khellang/Nzb](https://github.com/khellang/Nzb)
