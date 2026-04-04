using Microsoft.Extensions.FileProviders;
using Usenet.Nzb;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;

namespace Usenet.Tests.Nzb;

internal sealed class NzbWriterTests
{
    [Test]
    [MethodDataSource(nameof(GetNzbFiles))]
    internal async Task ShouldWriteDocumentToFile(
        IFileInfo file,
        CancellationToken cancellationToken
    )
    {
        var expected = await NzbParser
            .ParseAsync(file.ReadAllText(UsenetEncoding.Default), cancellationToken)
            .ConfigureAwait(true);

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, UsenetEncoding.Default);
        using var reader = new StreamReader(stream, UsenetEncoding.Default);

        // write to file and read back for comparison
        await writer.WriteNzbDocumentAsync(expected, cancellationToken).ConfigureAwait(true);
        stream.Position = 0;
        var actual = await NzbParser.ParseAsync(reader, cancellationToken).ConfigureAwait(true);

        // compare
        await Assert.That(actual).IsEqualTo(expected);
    }

    public static IEnumerable<Func<IFileInfo>> GetNzbFiles()
    {
        yield return () => EmbeddedResourceHelper.GetFileInfo("nzb.sabnzbd.nzb");
        yield return () => EmbeddedResourceHelper.GetFileInfo("nzb.sabnzbd-no-namespace.nzb");
    }
}
