using Microsoft.Extensions.FileProviders;
using Usenet.Extensions;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Usenet.Yenc;

namespace Usenet.Tests.Yenc;

internal sealed class YencStreamDecoderTests
{
    [Test]
    [MethodDataSource(nameof(GetSinglePartData))]
    internal async Task SinglePartFileShouldBeDecoded(
        IFileInfo expected,
        IFileInfo actual,
        CancellationToken cancellationToken
    )
    {
        var expectedData = expected.ReadAllBytes();

        var actualStream = YencStreamDecoder.Decode(
            actual.ReadAllLines(UsenetEncoding.Default),
            cancellationToken
        );

        var actualData = actualStream.ReadAllBytes();

        await Assert.That(actualStream.Header.IsFilePart).IsFalse();
        await Assert.That(actualStream.Header.LineLength).IsEqualTo(128);
        await Assert.That(actualStream.Header.FileSize).IsEqualTo(584);
        await Assert.That(actualStream.Header.FileName).IsEqualTo("testfile.txt");
        await Assert.That(actualData.Length).IsEqualTo(584);
        await Assert.That(actualData).IsEquivalentTo(expectedData);
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
    internal async Task FilePartShouldBeDecoded(
        IFileInfo actual,
        CancellationToken cancellationToken
    )
    {
        const int expectedDataLength = 11250;
        var actualStream = YencStreamDecoder.Decode(
            actual.ReadAllLines(UsenetEncoding.Default),
            cancellationToken
        );
        var actualData = actualStream.ReadAllBytes();

        await Assert.That(actualStream.Header.IsFilePart).IsTrue();
        await Assert.That(actualData.Length).IsEqualTo(expectedDataLength);
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
        IFileInfo partFile,
        CancellationToken cancellationToken
    )
    {
        const string expectedFileName = "joystick.jpg";
        var expected = expectedFile.ReadAllBytes();

        var part1 = YencStreamDecoder.Decode(
            part1File.ReadAllLines(UsenetEncoding.Default),
            cancellationToken
        );
        var part2 = YencStreamDecoder.Decode(
            partFile.ReadAllLines(UsenetEncoding.Default),
            cancellationToken
        );

        using var actual = new MemoryStream();

        actual.Seek(part1.Header.PartOffset, SeekOrigin.Begin);
        await part1.CopyToAsync(actual, cancellationToken).ConfigureAwait(true);

        actual.Seek(part2.Header.PartOffset, SeekOrigin.Begin);
        await part2.CopyToAsync(actual, cancellationToken).ConfigureAwait(true);

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
