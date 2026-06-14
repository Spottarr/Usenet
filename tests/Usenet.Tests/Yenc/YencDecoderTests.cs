using System.Buffers;
using System.Globalization;
using Microsoft.Extensions.FileProviders;
using Usenet.Exceptions;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Usenet.Yenc;

namespace Usenet.Tests.Yenc;

internal sealed class YencDecoderTests
{
    [Test]
    [MethodDataSource(nameof(GetSinglePartData))]
    internal async Task SinglePartFileShouldBeDecoded(IFileInfo expected, IFileInfo actual)
    {
        var expectedData = expected.ReadAllBytes();

        using var part = YencDecoder.Decode(actual.ReadAllBytes());

        await Assert.That(part.Header.IsFilePart).IsFalse();
        await Assert.That(part.Header.LineLength).IsEqualTo(128);
        await Assert.That(part.Header.FileSize).IsEqualTo(584);
        await Assert.That(part.Header.FileName).IsEqualTo("testfile.txt");
        await Assert.That(part.Footer).IsNotNull();
        await Assert.That(part.Footer!.PartSize).IsEqualTo(584);
        await Assert
            .That(
                ((int)part.Footer.Crc32.GetValueOrDefault()).ToString(
                    "x",
                    CultureInfo.InvariantCulture
                )
            )
            .IsEqualTo("ded29f4f");
        await Assert.That(part.Data.ToArray()).IsEquivalentTo(expectedData);
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

        using var part = YencDecoder.Decode(actual.ReadAllBytes());

        await Assert.That(part.Header.IsFilePart).IsTrue();
        await Assert.That(part.Data.Length).IsEqualTo(expectedDataLength);
        await Assert.That(part.Footer).IsNotNull();
        await Assert.That(part.Footer!.PartCrc32).IsNotNull();
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

        using var part1 = YencDecoder.Decode(part1File.ReadAllBytes());
        using var part2 = YencDecoder.Decode(part2File.ReadAllBytes());

        using var actual = new MemoryStream();

        actual.Seek(part1.Header.PartOffset, SeekOrigin.Begin);
        await actual.WriteAsync(part1.Data[..(int)part1.Header.PartSize]);

        actual.Seek(part2.Header.PartOffset, SeekOrigin.Begin);
        await actual.WriteAsync(part2.Data[..(int)part2.Header.PartSize]);

        await Assert.That(part1.Header.FileName).IsEqualTo(expectedFileName);
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

    [Test]
    [MethodDataSource(nameof(GetSinglePartData))]
    internal async Task DataShouldMatchStringBasedDecoder(IFileInfo _, IFileInfo actual)
    {
        var expected = YencArticleDecoder.Decode(actual.ReadAllLines(UsenetEncoding.Default));

        using var part = YencDecoder.Decode(actual.ReadAllBytes());

        await Assert.That(part.Data.ToArray()).IsEquivalentTo(expected.Data);
    }

    [Test]
    [MethodDataSource(nameof(GetFilePartData))]
    internal async Task ChecksumMismatchShouldThrow(IFileInfo actual)
    {
        var bytes = actual.ReadAllBytes();

        // Corrupt a byte in the middle of the encoded data, well clear of the
        // header and footer keyword lines, so the part checksum no longer matches.
        bytes[bytes.Length / 2] ^= 0x01;

        await Assert.That(() => YencDecoder.Decode(bytes)).Throws<InvalidYencDataException>();
    }

    [Test]
    [MethodDataSource(nameof(GetFilePartData))]
    internal async Task SequenceOverloadShouldDecode(IFileInfo actual)
    {
        var bytes = actual.ReadAllBytes();
        var sequence = new ReadOnlySequence<byte>(bytes);

        using var part = YencDecoder.Decode(sequence);

        await Assert.That(part.Data.Length).IsEqualTo(11250);
    }
}
