﻿using Microsoft.Extensions.FileProviders;
using Usenet.Nzb;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Xunit;

namespace Usenet.Tests.Nzb;

public class NzbWriterTests
{
    [Theory]
    [EmbeddedResourceData(@"nzb.sabnzbd.nzb")]
    [EmbeddedResourceData(@"nzb.sabnzbd-no-namespace.nzb")]
    internal void ShouldWriteDocumentToFile(IFileInfo file)
    {
        var expected = NzbParser.Parse(file.ReadAllText(UsenetEncoding.Default));

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, UsenetEncoding.Default);
        using var reader = new StreamReader(stream, UsenetEncoding.Default);

        // write to file and read back for comparison
        writer.WriteNzbDocument(expected);
        stream.Position = 0;
        var actual = NzbParser.Parse(reader.ReadToEnd());

        // compare
        Assert.Equal(expected, actual);
    }
}