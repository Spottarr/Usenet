using Microsoft.Extensions.FileProviders;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Usenet.Yenc;
using Xunit;

namespace Usenet.Tests.Yenc;

public class YencEncoderTests
{
    [Theory]
    [EmbeddedResourceData(@"yenc.singlepart.test (1.2).ntx", @"yenc.singlepart.test (1.2).dat")]
    internal async Task ShouldBeEncodedAsSinglePartFile(IFileInfo expected, IFileInfo actual)
    {
        var expectedText = expected
            .ReadAllLines(UsenetEncoding.Default)
            .Skip(3)
            .Take(9)
            .ToList();

        var data = actual.ReadAllBytes();

        using var stream = new MemoryStream(data);

        var header = new YencHeader("test (1.2).txt", data.Length, 10, 0, 1, data.Length, 0);
        var actualText = await YencEncoder.EncodeAsync(header, stream, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(expectedText, actualText);
    }

    [Theory]
    [EmbeddedResourceData(@"yenc.multipart.test (1.2).ntx", @"yenc.multipart.test (1.2).dat")]
    internal async Task ShouldBeEncodedAsPartOfMultiPartFile(IFileInfo expected, IFileInfo actual)
    {
        var expectedText = expected
            .ReadAllLines(UsenetEncoding.Default)
            .Skip(3)
            .Take(10)
            .ToList();

        var data = actual.ReadAllBytes();

        using var stream = new MemoryStream(data);

        var header = new YencHeader("test (1.2).txt", 120, 10, 1, 2, data.Length, 0);
        var actualText = await YencEncoder.EncodeAsync(header, stream, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(expectedText, actualText);
    }
}