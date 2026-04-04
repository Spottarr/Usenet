using System.Globalization;
using Microsoft.Extensions.FileProviders;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Usenet.Yenc;

namespace Usenet.Tests.Yenc;

internal sealed class YencArticleDecoderTests
{
    [Test]
    [MethodDataSource(nameof(GetSinglePartData))]
    internal async Task SinglePartFileShouldBeDecoded(IFileInfo expected, IFileInfo actual)
    {
        var expectedData = expected.ReadAllBytes().ToList();

        var actualArticle = YencArticleDecoder.Decode(actual.ReadAllLines(UsenetEncoding.Default));

        await Assert.That(actualArticle.Header.IsFilePart).IsFalse();
        await Assert.That(actualArticle.Header.LineLength).IsEqualTo(128);
        await Assert.That(actualArticle.Header.FileSize).IsEqualTo(584);
        await Assert.That(actualArticle.Header.FileName).IsEqualTo("testfile.txt");
        await Assert.That(actualArticle.Footer).IsNotNull();
        await Assert.That(actualArticle.Footer!.PartSize).IsEqualTo(584);
        await Assert
            .That(
                ((int)actualArticle.Footer.Crc32.GetValueOrDefault()).ToString(
                    "x",
                    CultureInfo.InvariantCulture
                )
            )
            .IsEqualTo("ded29f4f");
        await Assert.That(actualArticle.Data).IsEquivalentTo(expectedData);
    }

    public static IEnumerable<Func<(IFileInfo, IFileInfo)>> GetSinglePartData()
    {
        yield return () =>
            (
                EmbeddedResourceHelper.GetFileInfo("yenc.singlepart.testfile.txt"),
                EmbeddedResourceHelper.GetFileInfo("yenc.singlepart.00000005.ntx")
            );
    }

    [Test]
    [MethodDataSource(nameof(GetFilePartData))]
    internal async Task FilePartShouldBeDecoded(IFileInfo actual)
    {
        const int expectedDataLength = 11250;

        var actualArticle = YencArticleDecoder.Decode(actual.ReadAllLines(UsenetEncoding.Default));

        await Assert.That(actualArticle.Header.IsFilePart).IsTrue();
        await Assert.That(actualArticle.Data.Count).IsEqualTo(expectedDataLength);
    }

    public static IEnumerable<Func<IFileInfo>> GetFilePartData()
    {
        yield return () => EmbeddedResourceHelper.GetFileInfo("yenc.multipart.00000020.ntx");
    }

    [Test]
    [MethodDataSource(nameof(GetMultiPartData))]
    internal async Task MultiPartFileShouldBeDecoded(
        IFileInfo expectedFile,
        IFileInfo part1File,
        IFileInfo part2File
    )
    {
        const string expectedFileName = "joystick.jpg";
        var expected = expectedFile.ReadAllBytes();

        var part1 = YencArticleDecoder.Decode(part1File.ReadAllLines(UsenetEncoding.Default));
        var part2 = YencArticleDecoder.Decode(part2File.ReadAllLines(UsenetEncoding.Default));

        using var actual = new MemoryStream();

        actual.Seek(part1.Header.PartOffset, SeekOrigin.Begin);
        await actual
            .WriteAsync(part1.Data.ToArray().AsMemory(0, (int)part1.Header.PartSize))
            .ConfigureAwait(true);

        actual.Seek(part2.Header.PartOffset, SeekOrigin.Begin);
        await actual
            .WriteAsync(part2.Data.ToArray().AsMemory(0, (int)part2.Header.PartSize))
            .ConfigureAwait(true);

        var actualFileName = part1.Header.FileName;

        await Assert.That(actualFileName).IsEqualTo(expectedFileName);
        await Assert.That(actual.ToArray()).IsEquivalentTo(expected);
    }

    public static IEnumerable<Func<(IFileInfo, IFileInfo, IFileInfo)>> GetMultiPartData()
    {
        yield return () =>
            (
                EmbeddedResourceHelper.GetFileInfo("yenc.multipart.joystick.jpg"),
                EmbeddedResourceHelper.GetFileInfo("yenc.multipart.00000020.ntx"),
                EmbeddedResourceHelper.GetFileInfo("yenc.multipart.00000021.ntx")
            );
    }
}
