using System.Buffers;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Usenet.Yenc;

namespace Usenet.Tests.Yenc;

internal sealed class YencEncoderTests
{
    [Test]
    [MethodDataSource(nameof(GetSinglePartData))]
    internal async Task ShouldBeEncodedAsSinglePartFile(
        IFileInfo expected,
        IFileInfo actual,
        CancellationToken cancellationToken
    )
    {
        var expectedText = expected.ReadAllLines(UsenetEncoding.Default).Skip(3).Take(9).ToList();

        var data = actual.ReadAllBytes();

        using var stream = new MemoryStream(data);

        var header = new YencHeader("test (1.2).txt", data.Length, 10, 0, 1, data.Length, 0);
        var actualText = await YencEncoder.EncodeAsync(header, stream, cancellationToken);

        await Assert.That(actualText).IsEquivalentTo(expectedText);
    }

    public static IEnumerable<Func<(IFileInfo, IFileInfo)>> GetSinglePartData()
    {
        yield return () =>
            (
                EmbeddedResourceHelper.GetFileInfo("yenc.singlepart.test (1.2).ntx"),
                EmbeddedResourceHelper.GetFileInfo("yenc.singlepart.test (1.2).dat")
            );
    }

    [Test]
    [MethodDataSource(nameof(GetMultiPartData))]
    internal async Task ShouldBeEncodedAsPartOfMultiPartFile(
        IFileInfo expected,
        IFileInfo actual,
        CancellationToken cancellationToken
    )
    {
        var expectedText = expected.ReadAllLines(UsenetEncoding.Default).Skip(3).Take(10).ToList();

        var data = actual.ReadAllBytes();

        using var stream = new MemoryStream(data);

        var header = new YencHeader("test (1.2).txt", 120, 10, 1, 2, data.Length, 0);
        var actualText = await YencEncoder.EncodeAsync(header, stream, cancellationToken);

        await Assert.That(actualText).IsEquivalentTo(expectedText);
    }

    public static IEnumerable<Func<(IFileInfo, IFileInfo)>> GetMultiPartData()
    {
        yield return () =>
            (
                EmbeddedResourceHelper.GetFileInfo("yenc.multipart.test (1.2).ntx"),
                EmbeddedResourceHelper.GetFileInfo("yenc.multipart.test (1.2).dat")
            );
    }

    [Test]
    [MethodDataSource(nameof(GetSinglePartData))]
    internal async Task ShouldStreamEncodeSinglePartFile(
        IFileInfo expected,
        IFileInfo actual,
        CancellationToken cancellationToken
    )
    {
        var expectedText = expected.ReadAllLines(UsenetEncoding.Default).Skip(3).Take(9).ToList();

        var data = actual.ReadAllBytes();

        using var stream = new MemoryStream(data);

        var header = new YencHeader("test (1.2).txt", data.Length, 10, 0, 1, data.Length, 0);

        var writer = new ArrayBufferWriter<byte>();
        await YencEncoder.EncodeAsync(header, stream, writer, cancellationToken);

        var actualText = SplitLines(writer.WrittenSpan, UsenetEncoding.Default);

        await Assert.That(actualText).IsEquivalentTo(expectedText);
    }

    [Test]
    [MethodDataSource(nameof(GetMultiPartData))]
    internal async Task ShouldStreamEncodePartOfMultiPartFile(
        IFileInfo expected,
        IFileInfo actual,
        CancellationToken cancellationToken
    )
    {
        var expectedText = expected.ReadAllLines(UsenetEncoding.Default).Skip(3).Take(10).ToList();

        var data = actual.ReadAllBytes();

        using var stream = new MemoryStream(data);

        var header = new YencHeader("test (1.2).txt", 120, 10, 1, 2, data.Length, 0);

        var writer = new ArrayBufferWriter<byte>();
        await YencEncoder.EncodeAsync(header, stream, writer, cancellationToken);

        var actualText = SplitLines(writer.WrittenSpan, UsenetEncoding.Default);

        await Assert.That(actualText).IsEquivalentTo(expectedText);
    }

    [Test]
    [MethodDataSource(nameof(GetSinglePartData))]
    internal async Task ShouldRoundTripSinglePartFile(
        IFileInfo expected,
        IFileInfo actual,
        CancellationToken cancellationToken
    )
    {
        _ = expected;

        var data = actual.ReadAllBytes();

        using var stream = new MemoryStream(data);

        var header = new YencHeader("test (1.2).txt", data.Length, 128, 0, 1, data.Length, 0);

        var writer = new ArrayBufferWriter<byte>();
        await YencEncoder.EncodeAsync(header, stream, writer, cancellationToken);

        var article = YencArticleDecoder.Decode(
            SplitLines(writer.WrittenSpan, UsenetEncoding.Default)
        );

        await Assert.That(article.Data).IsEquivalentTo(data);
    }

    private static List<string> SplitLines(ReadOnlySpan<byte> bytes, Encoding encoding)
    {
        var lines = new List<string>();
        var start = 0;
        for (var i = 0; i < bytes.Length - 1; i++)
        {
            if (bytes[i] != 13 || bytes[i + 1] != 10)
            {
                continue;
            }

            lines.Add(encoding.GetString(bytes[start..i]));
            i++;
            start = i + 1;
        }

        return lines;
    }
}
