﻿using Usenet.Util;

namespace Usenet.Yenc;

/// <summary>
/// Represents a yEnc-encoded article validator.
/// Based on Kristian Hellang's yEnc project https://github.com/khellang/yEnc.
/// </summary>
public static class YencValidator
{
    /// <summary>
    /// Validates the specified <see cref="YencArticle"/>.
    /// </summary>
    /// <param name="article">The yEnc-encoded article to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> containing a list of 0 or more validation failures.</returns>
    public static ValidationResult Validate(YencArticle article)
    {
        Guard.ThrowIfNull(article, nameof(article));

        var failures = new List<ValidationFailure>();

        var header = article.Header;
        var footer = article.Footer;

        if (footer == null)
        {
            // nothing to validate
            return new ValidationResult(failures);
        }

        if (header.PartNumber > 0)
        {
            ValidatePart(article, failures);
            return new ValidationResult(failures);
        }

        if (footer.PartSize != article.Data.Count)
        {
            failures.Add(new ValidationFailure(
                YencValidationErrorCodes.SizeMismatch, Resources.Yenc.SizeMismatch,
                new { DataSize = article.Data.Count, FooterSize = footer.PartSize }));
        }

        if (!footer.Crc32.HasValue)
        {
            failures.Add(new ValidationFailure(
                YencValidationErrorCodes.MissingChecksum, Resources.Yenc.MissingChecksum));
            return new ValidationResult(failures);
        }

        var calculatedCrc32 = Crc32.CalculateChecksum(article.Data);
        if (calculatedCrc32 != footer.Crc32.Value)
        {
            failures.Add(new ValidationFailure(
                YencValidationErrorCodes.ChecksumMismatch, Resources.Yenc.ChecksumMismatch,
                new { CalculatedChecksum = calculatedCrc32, FooterChecksum = footer.Crc32.Value }));
        }

        return new ValidationResult(failures);
    }

    private static void ValidatePart(YencArticle article, List<ValidationFailure> failures)
    {
        var header = article.Header;
        var footer = article.Footer;

        if (header.PartNumber != footer.PartNumber)
        {
            failures.Add(new ValidationFailure(
                YencValidationErrorCodes.PartMismatch, Resources.Yenc.PartMismatch,
                new { HeaderPart = header.PartNumber, FooterPart = footer.PartNumber }));
        }

        if (!(footer.PartSize == article.Data.Count && footer.PartSize == header.PartSize))
        {
            failures.Add(new ValidationFailure(
                YencValidationErrorCodes.SizeMismatch, Resources.Yenc.PartSizeMismatch,
                new { DataSize = article.Data.Count, HeaderSize = header.PartSize, FooterSize = footer.PartSize }));
        }

        if (!footer.PartCrc32.HasValue)
        {
            failures.Add(new ValidationFailure(
                YencValidationErrorCodes.MissingChecksum, Resources.Yenc.MissingPartChecksum));
            return;
        }

        var calculatedCrc32 = Crc32.CalculateChecksum(article.Data);
        if (calculatedCrc32 != footer.PartCrc32.Value)
        {
            failures.Add(new ValidationFailure(
                YencValidationErrorCodes.ChecksumMismatch, Resources.Yenc.PartChecksumMismatch,
                new { CalculatedChecksum = calculatedCrc32, FooterChecksum = footer.PartCrc32 }));
        }
    }
}