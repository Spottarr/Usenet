using Microsoft.Extensions.FileProviders;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Yenc;

namespace Usenet.Tests.Yenc;

/// <summary>
/// Allocation-regression guard for the byte-oriented yEnc decode path (ADR-0002). The decoded
/// data lands in a pooled buffer, so a full part decode should allocate only the small set of
/// per-call objects (header dictionary, meta-data records, the part itself) and stay well clear
/// of the per-line <see cref="string"/> churn of the text-based decoder it replaced.
/// </summary>
internal sealed class YencDecoderAllocationTests
{
    // Decoding the ~11 KB multipart sample allocates ~3.7 KB of per-call objects on the rebuilt
    // byte path (header records, meta-data, the part itself) with the decoded data landing in a
    // pooled buffer; the text decoder it replaced allocated ~140 KB for the same input. The
    // ceiling sits ~60% above the measured number for runtime variation but more than an order of
    // magnitude below the old cost, so a regression back to per-line strings trips it.
    private const long MaxBytesPerDecode = 6_144;
    private const int Iterations = 200;

    [Test]
    [MethodDataSource(nameof(GetPartData))]
    internal async Task DecodeShouldStayUnderAllocationCeiling(IFileInfo part)
    {
        var encoded = part.ReadAllBytes();

        var perDecode = AllocationMeasurement.PerIteration(
            () =>
            {
                using var decoded = YencDecoder.Decode(encoded);
                _ = decoded.Data.Length;
            },
            Iterations
        );

        await Assert.That(perDecode).IsLessThanOrEqualTo(MaxBytesPerDecode);
    }

    public static IEnumerable<Func<IFileInfo>> GetPartData()
    {
        yield return () => EmbeddedResourceHelper.GetFileInfo("yenc.multipart.00000020.ntx");
    }
}
