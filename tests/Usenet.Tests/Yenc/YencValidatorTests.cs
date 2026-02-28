using Microsoft.Extensions.FileProviders;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Usenet.Yenc;
using Xunit;

namespace Usenet.Tests.Yenc;

public class YencValidatorTests
{
    [Theory]
    [EmbeddedResourceData(@"yenc.singlepart.00000005.ntx")]
    [EmbeddedResourceData(@"yenc.multipart.00000020.ntx")]
    [EmbeddedResourceData(@"yenc.multipart.00000021.ntx")]
    [EmbeddedResourceData(@"yenc.singlepart.test (1.2).ntx")]
    [EmbeddedResourceData(@"yenc.multipart.test (1.2).ntx")]
    internal void ArticleShouldBeValid(IFileInfo file)
    {
        var article = YencArticleDecoder.Decode(file.ReadAllLines(UsenetEncoding.Default));

        var actual = YencValidator.Validate(article).IsValid;
        Assert.True(actual);
    }

    [Theory]
    [EmbeddedResourceData(
        @"yenc.singlepart.00000005 (checksum mismatch).ntx",
        AdditionalData = [YencValidationErrorCodes.ChecksumMismatch]
    )]
    [EmbeddedResourceData(
        @"yenc.singlepart.00000005 (missing checksum).ntx",
        AdditionalData = [YencValidationErrorCodes.MissingChecksum]
    )]
    [EmbeddedResourceData(
        @"yenc.singlepart.00000005 (size mismatch).ntx",
        AdditionalData = [YencValidationErrorCodes.SizeMismatch]
    )]
    [EmbeddedResourceData(
        @"yenc.multipart.00000021 (checksum mismatch).ntx",
        AdditionalData = [YencValidationErrorCodes.ChecksumMismatch]
    )]
    [EmbeddedResourceData(
        @"yenc.multipart.00000021 (missing checksum).ntx",
        AdditionalData = [YencValidationErrorCodes.MissingChecksum]
    )]
    [EmbeddedResourceData(
        @"yenc.multipart.00000021 (part mismatch).ntx",
        AdditionalData = [YencValidationErrorCodes.PartMismatch]
    )]
    [EmbeddedResourceData(
        @"yenc.multipart.00000021 (size mismatch).ntx",
        AdditionalData = [YencValidationErrorCodes.SizeMismatch]
    )]
    internal void ArticleShouldBeInvalid(IFileInfo file, string errorCode)
    {
        var article = YencArticleDecoder.Decode(file.ReadAllLines(UsenetEncoding.Default));

        var result = YencValidator.Validate(article);
        Assert.False(result.IsValid);
        Assert.Equal(errorCode, result.Failures.Single().Code);
    }
}
