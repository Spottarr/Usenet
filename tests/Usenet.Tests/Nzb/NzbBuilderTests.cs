﻿using Microsoft.Extensions.FileProviders;
using Usenet.Nzb;
using Usenet.Tests.TestHelpers;
using Xunit;

namespace Usenet.Tests.Nzb;

public class NzbBuilderTests
{
    [Theory]
    [EmbeddedResourceData(@"yenc.multipart.joystick.jpg")]
    internal void ShouldBuildDocumentFromFile(IFileInfo file)
    {
        var actualDocument = new NzbBuilder()
            .AddFile(file)
            .AddGroups("alt.binaries.newzbin")
            .AddGroups("alt.binaries.mojo")
            .AddMetaData("title", "joystick")
            .AddMetaData("tag", "image")
            .SetPoster("dummy@ignorethis.com")
            .SetMessageBase("ignorethis.com")
            .Build();
        Assert.Equal("joystick", actualDocument.MetaData["title"].Single());
    }
}