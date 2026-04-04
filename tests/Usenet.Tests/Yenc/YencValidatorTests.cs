using Microsoft.Extensions.FileProviders;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Usenet.Yenc;

namespace Usenet.Tests.Yenc;

internal sealed class YencValidatorTests
{
    [Test]
    [MethodDataSource(nameof(GetValidArticleFiles))]
    internal async Task ArticleShouldBeValid(IFileInfo file)
    {
        var article = YencArticleDecoder.Decode(file.ReadAllLines(UsenetEncoding.Default));

        var actual = YencValidator.Validate(article).IsValid;
        await Assert.That(actual).IsTrue();
    }

    public static IEnumerable<Func<IFileInfo>> GetValidArticleFiles()
    {
        yield return () => EmbeddedResourceHelper.GetFileInfo("yenc.singlepart.00000005.ntx");
        yield return () => EmbeddedResourceHelper.GetFileInfo("yenc.multipart.00000020.ntx");
        yield return () => EmbeddedResourceHelper.GetFileInfo("yenc.multipart.00000021.ntx");
        yield return () => EmbeddedResourceHelper.GetFileInfo("yenc.singlepart.test (1.2).ntx");
        yield return () => EmbeddedResourceHelper.GetFileInfo("yenc.multipart.test (1.2).ntx");
    }

    [Test]
    [MethodDataSource(nameof(GetInvalidArticleData))]
    internal async Task ArticleShouldBeInvalid(IFileInfo file, string errorCode)
    {
        var article = YencArticleDecoder.Decode(file.ReadAllLines(UsenetEncoding.Default));

        var result = YencValidator.Validate(article);
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Failures.Single().Code).IsEqualTo(errorCode);
    }

    public static IEnumerable<Func<(IFileInfo, string)>> GetInvalidArticleData()
    {
        yield return () =>
            (
                EmbeddedResourceHelper.GetFileInfo(
                    "yenc.singlepart.00000005 (checksum mismatch).ntx"
                ),
                YencValidationErrorCodes.ChecksumMismatch
            );
        yield return () =>
            (
                EmbeddedResourceHelper.GetFileInfo(
                    "yenc.singlepart.00000005 (missing checksum).ntx"
                ),
                YencValidationErrorCodes.MissingChecksum
            );
        yield return () =>
            (
                EmbeddedResourceHelper.GetFileInfo("yenc.singlepart.00000005 (size mismatch).ntx"),
                YencValidationErrorCodes.SizeMismatch
            );
        yield return () =>
            (
                EmbeddedResourceHelper.GetFileInfo(
                    "yenc.multipart.00000021 (checksum mismatch).ntx"
                ),
                YencValidationErrorCodes.ChecksumMismatch
            );
        yield return () =>
            (
                EmbeddedResourceHelper.GetFileInfo(
                    "yenc.multipart.00000021 (missing checksum).ntx"
                ),
                YencValidationErrorCodes.MissingChecksum
            );
        yield return () =>
            (
                EmbeddedResourceHelper.GetFileInfo("yenc.multipart.00000021 (part mismatch).ntx"),
                YencValidationErrorCodes.PartMismatch
            );
        yield return () =>
            (
                EmbeddedResourceHelper.GetFileInfo("yenc.multipart.00000021 (size mismatch).ntx"),
                YencValidationErrorCodes.SizeMismatch
            );
    }
}
