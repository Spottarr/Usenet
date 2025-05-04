# Usenet

A library for working with [Usenet](https://en.wikipedia.org/wiki/Usenet). It offers:
* an [NNTP](https://en.wikipedia.org/wiki/Network_News_Transfer_Protocol) client
* an [NZB](https://en.wikipedia.org/wiki/NZB) file parser, builder and writer
* a [yEnc](https://en.wikipedia.org/wiki/YEnc) encoder and decoder

It is mainly focused on keeping memory usage low. Server responses can be enumerated as they come in.
Binary messages will be encoded to yEnc format streaming and yEnc-encoded data will be decoded to binary data streaming.

The NNTP client is compliant with [RFC 2980](https://tools.ietf.org/html/rfc2980), [RFC 3977](https://tools.ietf.org/html/rfc3977), [RFC 4643](https://tools.ietf.org/html/rfc4643) and [RFC 6048](https://tools.ietf.org/html/rfc6048).

[![Nuget](https://img.shields.io/nuget/v/Spottarr.Usenet)](https://www.nuget.org/packages/Spottarr.Usenet)
[![Nuget Prerelease](https://img.shields.io/nuget/vpre/Spottarr.Usenet?label=nuget%20prerelease)](https://www.nuget.org/packages/Spottarr.Usenet)

## Getting Started ##
Install [Nuget](https://www.nuget.org/packages/Spottarr.Usenet) package:
```zsh
dotnet add package Spottarr.Usenet
```

## Examples ##
Connect to Usenet server:
```csharp
var client = new NntpClient(new NntpConnection());
await client.ConnectAsync(hostname, port, useSsl);
```
Authenticate:
```csharp
client.Authenticate(username, password);
```
Enable logging:
```csharp
ILoggerFactory factory = new SomeLoggerFactory();
Usenet.Logger.Factory = factory;
```
Retrieve article:
```csharp
var response = client.Article(messageId);
if (response.Success)
{
    foreach (var line in response.Article.Body)
    {
        ...
    }
}
```
Build an article and post to server:
```csharp
var messageId = $"{Guid.NewGuid()}@example.net";

var newArticle = new NntpArticleBuilder()
    .SetMessageId(messageId)
    .SetFrom("Randomposter <randomposter@example.net>")
    .SetSubject("Random test post #1")
    .AddGroups("alt.test.clienttest", "alt.test")
    .AddLine("This is a message with id " + messageId)
    .AddLine("with multiple lines")
    .Build();

client.Post(newArticle);
```
Parse an NZB file, download, decode and write the parts streaming to a file:
```csharp
var nzbDocument = NzbParser.Parse(File.ReadAllText(nzbPath));

foreach (var file in nzbDocument.Files)
{
    foreach (var segment in file.Segments)
    {
        // retrieve article from Usenet server
        var response = client.Article(segment.MessageId);

        // decode the yEnc-encoded article
        using var yencStream = YencStreamDecoder.Decode(response.Article.Body);

        var header = yencStream.Header;

        if (!File.Exists(header.FileName))
        {
            // create file and pre-allocate disk space for it
            using var stream = File.Create(header.FileName);
            stream.SetLength(header.FileSize);
        }
        else
        {
            using var stream = File.Open(header.FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            // copy incoming parts to file
            stream.Position = header.PartOffset;
            yencStream.CopyTo(stream);
        }
    }
}
```
Build an NZB document and write to file:
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

using var file = File.Open("Pictures.nzb");
using var writer = new StreamWriter(file, UsenetEncoding.Default);

await writer.WriteNzbDocumentAsync(nzbDocument);
```
Encode the files from an NZB document in yEnc format and upload streaming:
```csharp
foreach (var file in nzbDocument.Files)
{
    // open file stream
    var fileInfo = fileProvider.GetFileInfo(file.FileName);
    using var stream = fileInfo.CreateReadStream();
    foreach (var segment in file.Segments)
    {
        stream.Position = segment.Offset;

        // encode in yEnc format
        var encodedBody = YencEncoder.Encode(new YencHeader(
            file.FileName, file.Size, 128, segment.Number, file.Segments.Count,
            segment.Size, segment.Offset), stream);

        // create article
        var article = new NntpArticleBuilder()
            .AddGroups(file.Groups)
            .SetMessageId(segment.MessageId)
            .SetFrom(file.Poster)
            .SetSubject(segment.MessageId)
            .SetBody(encodedBody)
            .Build();

        // post article
        client.Post(article);
    }
}
```
Close connection:
```csharp
client.Quit();
```

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Acknowledgments

This project was heavily inspired by [Kristian Hellang](https://github.com/khellang)'s work:
* [khellang/yEnc](https://github.com/khellang/yEnc)
* [khellang/NntpLib.Net](https://github.com/khellang/NntpLib.Net)
* [khellang/Nzb](https://github.com/khellang/Nzb)
