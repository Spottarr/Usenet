using Microsoft.Extensions.FileProviders;
using Usenet.Util;
using Usenet.Yenc;
using UsenetTests.Extensions;
using UsenetTests.TestHelpers;
using Xunit;

namespace UsenetTests.Yenc
{
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
            YencArticle article = YencArticleDecoder.Decode(file.ReadAllLines(UsenetEncoding.Default));


            bool actual = YencValidator.Validate(article).IsValid;
            Assert.True(actual);
        }

        [Theory]
        [EmbeddedResourceData(@"yenc.singlepart.00000005 (checksum mismatch).ntx", AdditionalData = [YencValidationErrorCodes.ChecksumMismatch])]
        [EmbeddedResourceData(@"yenc.singlepart.00000005 (missing checksum).ntx", AdditionalData = [ YencValidationErrorCodes.MissingChecksum])]
        [EmbeddedResourceData(@"yenc.singlepart.00000005 (size mismatch).ntx", AdditionalData = [ YencValidationErrorCodes.SizeMismatch])]
        [EmbeddedResourceData(@"yenc.multipart.00000021 (checksum mismatch).ntx", AdditionalData = [ YencValidationErrorCodes.ChecksumMismatch])]
        [EmbeddedResourceData(@"yenc.multipart.00000021 (missing checksum).ntx", AdditionalData = [ YencValidationErrorCodes.MissingChecksum])]
        [EmbeddedResourceData(@"yenc.multipart.00000021 (part mismatch).ntx", AdditionalData = [ YencValidationErrorCodes.PartMismatch])]
        [EmbeddedResourceData(@"yenc.multipart.00000021 (size mismatch).ntx", AdditionalData = [ YencValidationErrorCodes.SizeMismatch])]
        internal void ArticleShouldBeInvalid(IFileInfo file, string errorCode)
        {
            YencArticle article = YencArticleDecoder.Decode(file.ReadAllLines(UsenetEncoding.Default));

            ValidationResult result = YencValidator.Validate(article);
            Assert.False(result.IsValid);
            Assert.Equal(errorCode, result.Failures.Single().Code);
        }
    }
}
