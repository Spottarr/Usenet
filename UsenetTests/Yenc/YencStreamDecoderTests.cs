using Microsoft.Extensions.FileProviders;
using Usenet.Extensions;
using Usenet.Util;
using Usenet.Yenc;
using UsenetTests.Extensions;
using UsenetTests.TestHelpers;
using Xunit;

namespace UsenetTests.Yenc
{
    public class YencStreamDecoderTests
    {
        [Theory]
        [EmbeddedResourceData(@"yenc.singlepart.testfile.txt", @"yenc.singlepart.00000005.ntx")]
        internal void SinglePartFileShouldBeDecoded(IFileInfo expected, IFileInfo actual)
        {
            var expectedData = expected.ReadAllBytes();

            var actualStream = YencStreamDecoder.Decode(actual.ReadAllLines(UsenetEncoding.Default));

            var actualData = actualStream.ReadAllBytes();

            Assert.False(actualStream.Header.IsFilePart);
            Assert.Equal(128, actualStream.Header.LineLength);
            Assert.Equal(584, actualStream.Header.FileSize);
            Assert.Equal("testfile.txt", actualStream.Header.FileName);
            Assert.Equal(584, actualData.Length);
            Assert.Equal(expectedData, actualData);
        }
        
        [Theory]
        [EmbeddedResourceData(@"yenc.multipart.00000020.ntx")]
        internal void FilePartShouldBeDecoded(IFileInfo actual)
        {
            const int expectedDataLength = 11250;
            var actualStream = YencStreamDecoder.Decode(actual.ReadAllLines(UsenetEncoding.Default));
            var actualData = actualStream.ReadAllBytes();

            Assert.True(actualStream.Header.IsFilePart);
            Assert.Equal(expectedDataLength, actualData.Length);
        }
        
        [Theory]
        [EmbeddedResourceData(@"yenc.multipart.joystick.jpg", @"yenc.multipart.00000020.ntx", @"yenc.multipart.00000021.ntx")]
        internal void MultiPartFileShouldBeDecoded(IFileInfo expectedFile, IFileInfo part1File, IFileInfo partFile)
        {
            const string expectedFileName = "joystick.jpg";
            var expected = expectedFile.ReadAllBytes();

            var part1 = YencStreamDecoder.Decode(part1File.ReadAllLines(UsenetEncoding.Default));
            var part2 = YencStreamDecoder.Decode(partFile.ReadAllLines(UsenetEncoding.Default));

            using var actual = new MemoryStream();

            actual.Seek(part1.Header.PartOffset, SeekOrigin.Begin);
            part1.CopyTo(actual);

            actual.Seek(part2.Header.PartOffset, SeekOrigin.Begin);
            part2.CopyTo(actual);

            var actualFileName = part1.Header.FileName;

            Assert.Equal(expectedFileName, actualFileName);
            Assert.Equal(expected, actual.ToArray());
        }

    }
}
