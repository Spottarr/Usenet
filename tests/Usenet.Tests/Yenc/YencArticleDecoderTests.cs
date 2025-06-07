using System.Globalization;
using Microsoft.Extensions.FileProviders;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Usenet.Yenc;
using Xunit;

namespace Usenet.Tests.Yenc;

public class YencArticleDecoderTests
{
    [Theory]
    [EmbeddedResourceData(@"yenc.singlepart.testfile.txt", @"yenc.singlepart.00000005.ntx")]
    internal void SinglePartFileShouldBeDecoded(IFileInfo expected, IFileInfo actual)
    {
        var expectedData = expected.ReadAllBytes().ToList();

        var actualArticle = YencArticleDecoder.Decode(actual.ReadAllLines(UsenetEncoding.Default));

        Assert.False(actualArticle.Header.IsFilePart);
        Assert.Equal(128, actualArticle.Header.LineLength);
        Assert.Equal(584, actualArticle.Header.FileSize);
        Assert.Equal("testfile.txt", actualArticle.Header.FileName);
        Assert.Equal(584, actualArticle.Footer.PartSize);
        Assert.Equal("ded29f4f", ((int)actualArticle.Footer.Crc32.GetValueOrDefault()).ToString("x", CultureInfo.InvariantCulture));
        Assert.Equal(expectedData, actualArticle.Data);
    }

    [Theory]
    [EmbeddedResourceData(@"yenc.multipart.00000020.ntx")]
    internal void FilePartShouldBeDecoded(IFileInfo actual)
    {
        const int expectedDataLength = 11250;

        var actualArticle = YencArticleDecoder.Decode(actual.ReadAllLines(UsenetEncoding.Default));

        Assert.True(actualArticle.Header.IsFilePart);
        Assert.Equal(expectedDataLength, actualArticle.Data.Count);
    }

    [Theory]
    [EmbeddedResourceData(@"yenc.multipart.joystick.jpg", @"yenc.multipart.00000020.ntx", @"yenc.multipart.00000021.ntx")]
    internal void MultiPartFileShouldBeDecoded(IFileInfo expectedFile, IFileInfo part1File, IFileInfo part2File)
    {
        const string expectedFileName = "joystick.jpg";
        var expected = expectedFile.ReadAllBytes();

        var part1 = YencArticleDecoder.Decode(part1File.ReadAllLines(UsenetEncoding.Default));
        var part2 = YencArticleDecoder.Decode(part2File.ReadAllLines(UsenetEncoding.Default));

        using var actual = new MemoryStream();

        actual.Seek(part1.Header.PartOffset, SeekOrigin.Begin);
        actual.Write(part1.Data.ToArray(), 0, (int)part1.Header.PartSize);

        actual.Seek(part2.Header.PartOffset, SeekOrigin.Begin);
        actual.Write(part2.Data.ToArray(), 0, (int)part2.Header.PartSize);

        var actualFileName = part1.Header.FileName;

        Assert.Equal(expectedFileName, actualFileName);
        Assert.Equal(expected, actual.ToArray());
    }
}