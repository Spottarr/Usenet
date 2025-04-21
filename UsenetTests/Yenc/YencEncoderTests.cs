using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Usenet.Util;
using Usenet.Yenc;
using UsenetTests.Extensions;
using UsenetTests.TestHelpers;
using Xunit;

namespace UsenetTests.Yenc
{
    public class YencEncoderTests
    {
        [Theory]
        [EmbeddedResourceData(@"yenc.singlepart.test (1.2).ntx", @"yenc.singlepart.test (1.2).dat")]
        internal void ShouldBeEncodedAsSinglePartFile(IFileInfo expected, IFileInfo actual)
        {
            List<string> expectedText = expected
                .ReadAllLines(UsenetEncoding.Default)
                .Skip(3)
                .Take(9)
                .ToList();

            byte[] data = actual.ReadAllBytes();
            
            using var stream = new MemoryStream(data);
            
            var header = new YencHeader("test (1.2).txt", data.Length, 10, 0, 1, data.Length, 0);
            List<string> actualText = YencEncoder.Encode(header, stream).ToList();

            Assert.Equal(expectedText, actualText);
        }

        [Theory]
        [EmbeddedResourceData(@"yenc.multipart.test (1.2).ntx", @"yenc.multipart.test (1.2).dat")]
        internal void ShouldBeEncodedAsPartOfMultiPartFile(IFileInfo expected, IFileInfo actual)
        {
            List<string> expectedText = expected
                .ReadAllLines(UsenetEncoding.Default)
                .Skip(3)
                .Take(10)
                .ToList();

            byte[] data = actual.ReadAllBytes();
            
            using var stream = new MemoryStream(data);
            
            var header = new YencHeader("test (1.2).txt", 120, 10, 1, 2, data.Length, 0);
            List<string> actualText = YencEncoder.Encode(header, stream).ToList();

            Assert.Equal(expectedText, actualText);
        }

    }
}
