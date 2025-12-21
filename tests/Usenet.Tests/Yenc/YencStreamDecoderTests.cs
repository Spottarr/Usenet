using Microsoft.Extensions.FileProviders;
using Usenet.Extensions;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Usenet.Yenc;
using Xunit;

namespace Usenet.Tests.Yenc;

public class YencStreamDecoderTests
{
    [Theory]
    [EmbeddedResourceData(@"yenc.singlepart.testfile.txt", @"yenc.singlepart.00000005.ntx")]
    internal async Task SinglePartFileShouldBeDecoded(IFileInfo expected, IFileInfo actual)
    {
        var expectedData = expected.ReadAllBytes();

        var actualStream = await YencStreamDecoder.DecodeAsync(actual.ReadAllLines(UsenetEncoding.Default), TestContext.Current.CancellationToken);

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
    internal async Task FilePartShouldBeDecoded(IFileInfo actual)
    {
        const int expectedDataLength = 11250;
        var actualStream = await YencStreamDecoder.DecodeAsync(actual.ReadAllLines(UsenetEncoding.Default), TestContext.Current.CancellationToken);
        var actualData = actualStream.ReadAllBytes();

        Assert.True(actualStream.Header.IsFilePart);
        Assert.Equal(expectedDataLength, actualData.Length);
    }

    [Theory]
    [EmbeddedResourceData(@"yenc.multipart.joystick.jpg", @"yenc.multipart.00000020.ntx", @"yenc.multipart.00000021.ntx")]
    internal async Task MultiPartFileShouldBeDecoded(IFileInfo expectedFile, IFileInfo part1File, IFileInfo partFile)
    {
        const string expectedFileName = "joystick.jpg";
        var expected = expectedFile.ReadAllBytes();

        var part1 = await YencStreamDecoder.DecodeAsync(part1File.ReadAllLines(UsenetEncoding.Default), TestContext.Current.CancellationToken);
        var part2 = await YencStreamDecoder.DecodeAsync(partFile.ReadAllLines(UsenetEncoding.Default), TestContext.Current.CancellationToken);

        using var actual = new MemoryStream();

        actual.Seek(part1.Header.PartOffset, SeekOrigin.Begin);
        await part1.CopyToAsync(actual, TestContext.Current.CancellationToken);

        actual.Seek(part2.Header.PartOffset, SeekOrigin.Begin);
        await part2.CopyToAsync(actual, TestContext.Current.CancellationToken);

        var actualFileName = part1.Header.FileName;

        Assert.Equal(expectedFileName, actualFileName);
        Assert.Equal(expected, actual.ToArray());
    }
}
