# Usenet

A .NET library for working with [Usenet](https://en.wikipedia.org/wiki/Usenet). It offers:
* an [NNTP](https://en.wikipedia.org/wiki/Network_News_Transfer_Protocol) client
* an [NZB](https://en.wikipedia.org/wiki/NZB) document parser, builder and writer
* a [yEnc](https://en.wikipedia.org/wiki/YEnc) encoder and decoder

The library is built around a byte-oriented, [`System.IO.Pipelines`](https://learn.microsoft.com/dotnet/standard/io/pipelines)
based transport that keeps allocations low: article bytes are framed off the wire without
being transcoded to strings, yEnc parts are decoded into pooled buffers, and yEnc encoding
streams straight into an `IBufferWriter<byte>`. See [docs/architecture.md](docs/architecture.md)
for the full streaming/buffering model and the ADRs behind it.

The NNTP client is compliant with [RFC 2980](https://tools.ietf.org/html/rfc2980), [RFC 3977](https://tools.ietf.org/html/rfc3977), [RFC 4643](https://tools.ietf.org/html/rfc4643) and [RFC 6048](https://tools.ietf.org/html/rfc6048).

[![Nuget](https://img.shields.io/nuget/v/Spottarr.Usenet)](https://www.nuget.org/packages/Spottarr.Usenet)
[![Nuget Prerelease](https://img.shields.io/nuget/vpre/Spottarr.Usenet?label=nuget%20prerelease)](https://www.nuget.org/packages/Spottarr.Usenet)

## Architecture

The library is split into independent layers (see [docs/architecture.md](docs/architecture.md)):

* **Connection** — the transport. Owns the socket, the optional `SslStream` and the
  `PipeReader`/`PipeWriter`. It frames lines off the raw byte stream, undoes dot-stuffing
  and counts bytes (`BytesRead`/`BytesWritten`/`ResetCounters`).
* **Client** — the command API: the RFC command methods built on top of a connection.
* **Pool** — a manager of authenticated, connected clients reused across operations, handed
  out as disposable leases.
* **yEnc** and **NZB** are independent layers. The client hands over bytes and never decodes;
  consumers invoke yEnc/NZB explicitly.

The read path copies one article (one yEnc part, bounded around 1 MB) into a single buffer
rented from `ArrayPool`. The byte-input `YencDecoder` decodes that into pooled `Data` and
verifies the per-part `pcrc32` in the same pass. On the write path `YencEncoder` reads the
source in blocks and encodes into an `IBufferWriter<byte>` through a precomputed escape table,
and the connection batches a single flush per command.

## Getting started
Install the [NuGet](https://www.nuget.org/packages/Spottarr.Usenet) package:
```zsh
dotnet add package Spottarr.Usenet
```

## Examples
Connect to a Usenet server:
```csharp
var client = new NntpClient(new NntpConnection());
await client.ConnectAsync(hostname, port, useSsl);
```
Authenticate:
```csharp
await client.AuthenticateAsync(username, password);
```
Enable logging by handing the library an `ILoggerFactory`:
```csharp
ILoggerFactory factory = new SomeLoggerFactory();
Usenet.Logger.Factory = factory;
```
Retrieve an article and read its body lines:
```csharp
var response = await client.ArticleAsync(messageId);
if (response.Success && response.Article is { } article)
{
    foreach (var line in article.Body)
    {
        ...
    }
}
```
Build an article and post it to the server:
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
Decode a yEnc-encoded part into a pooled `Data` buffer. The returned `YencPart` owns a buffer
rented from `ArrayPool`; its `Data` view is valid until the part is disposed, and the per-part
`pcrc32` (or `crc32` for a single-part file) is verified during decoding:
```csharp
// encoded holds the raw yEnc bytes of one part (ReadOnlyMemory<byte> or ReadOnlySequence<byte>)
using var part = YencDecoder.Decode(encoded);

YencHeader header = part.Header;
ReadOnlyMemory<byte> data = part.Data;

using var file = File.Open(header.FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
file.Position = header.PartOffset;
await file.WriteAsync(data);
```
Parse an NZB document, download each segment, decode it and write the parts streaming to a file:
```csharp
var nzbDocument = await NzbParser.ParseAsync(await File.ReadAllTextAsync(nzbPath));

foreach (var file in nzbDocument.Files)
{
    foreach (var segment in file.Segments)
    {
        // retrieve the article for this segment from the Usenet server
        var response = await client.BodyAsync(segment.MessageId);
        if (!response.Success || response.Article is not { } article)
            continue;

        // decode the yEnc-encoded body, streaming the decoded parts out
        using var yencStream = YencStreamDecoder.Decode(article.Body);

        var header = yencStream.Header;

        if (!File.Exists(header.FileName))
        {
            // create the file and pre-allocate disk space for it
            using var stream = File.Create(header.FileName);
            stream.SetLength(header.FileSize);
        }
        else
        {
            using var stream = File.Open(header.FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            // copy the incoming parts to the file at the part offset
            stream.Position = header.PartOffset;
            yencStream.CopyTo(stream);
        }
    }
}
```
Build an NZB document and write it to a file:
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
Encode the files referenced by an NZB document in yEnc format and post them. The text overload
returns the encoded body as lines, ready to hand to the article builder:
```csharp
foreach (var file in nzbDocument.Files)
{
    // open the source file
    var fileInfo = fileProvider.GetFileInfo(file.FileName);
    using var stream = fileInfo.CreateReadStream();
    foreach (var segment in file.Segments)
    {
        stream.Position = segment.Offset;

        // encode the part in yEnc format
        var header = new YencHeader(
            file.FileName, file.Size, 128, segment.Number, file.Segments.Count,
            segment.Size, segment.Offset);
        var encodedBody = await YencEncoder.EncodeAsync(header, stream);

        // build the article
        var article = new NntpArticleBuilder()
            .AddGroups(file.Groups)
            .SetMessageId(segment.MessageId)
            .SetFrom(file.Poster)
            .SetSubject(segment.MessageId)
            .SetBody(encodedBody)
            .Build();

        // post the article
        await client.PostAsync(article);
    }
}
```
For a low-allocation write path, encode straight into an `IBufferWriter<byte>` instead of
materializing the encoded lines:
```csharp
var writer = new ArrayBufferWriter<byte>();
await YencEncoder.EncodeAsync(header, stream, writer);
ReadOnlyMemory<byte> encodedBytes = writer.WrittenMemory;
```
Close the connection:
```csharp
await client.QuitAsync();
```

## Connection pooling

`NntpClientPool` manages a set of authenticated, connected clients and hands them out as
disposable leases. Connecting and authenticating happen lazily the first time a client is
borrowed, and idle clients are disconnected automatically:
```csharp
using var pool = new NntpClientPool(
    maxPoolSize: 10, hostname, port, useSsl, username, password);

using var lease = await pool.GetLease();
var response = await lease.Client.ArticleAsync(messageId);
// the client is returned to the pool when the lease is disposed
```

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Acknowledgments

This is a standalone library. It descends from [keimpema/Usenet](https://github.com/keimpema/Usenet),
which was in turn based on [Kristian Hellang](https://github.com/khellang)'s work:
* [khellang/yEnc](https://github.com/khellang/yEnc)
* [khellang/NntpLib.Net](https://github.com/khellang/NntpLib.Net)
* [khellang/Nzb](https://github.com/khellang/Nzb)
</content>
</invoke>
