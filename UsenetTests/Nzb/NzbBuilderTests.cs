﻿using System.Linq;
using Microsoft.Extensions.FileProviders;
using Usenet.Nzb;
using UsenetTests.TestHelpers;
using Xunit;

namespace UsenetTests.Nzb
{
    public class NzbBuilderTests : IClassFixture<TestData>
    {
        private readonly TestData testData;

        public NzbBuilderTests(TestData testData)
        {
            this.testData = testData;
        }

        [Theory]
        [InlineData(@"yenc.multipart.joystick.jpg")]
        internal void ShouldBuildDocumentFromFile(string fileName)
        {
            IFileInfo file = testData.GetEmbeddedFile(fileName);
            NzbDocument actualDocument = new NzbBuilder()
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
}
