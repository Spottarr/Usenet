using System.IO.Compression;
using System.Text;
using Usenet.Exceptions;
using Usenet.Nntp;

namespace Usenet.Tests.Nntp;

internal sealed class NntpCompressedBlockTests
{
    private const string Payload =
        "1\tSubject 1\tposter@example.com\tdate\t<1@example.com>\t\t1024\t8\r\n"
        + "2\tSubject 2\tposter@example.com\tdate\t<2@example.com>\t\t2048\t16\r\n";

    [Test]
    public async Task ShouldInflateGzipMember()
    {
        var compressed = GzipCompress(Payload);

        var inflated = NntpCompressedBlock.Inflate(compressed, compressed.Length);

        await Assert.That(Encoding.ASCII.GetString(inflated)).IsEqualTo(Payload);
    }

    [Test]
    public async Task ShouldInflateZlibStream()
    {
        var compressed = ZlibCompress(Payload);

        var inflated = NntpCompressedBlock.Inflate(compressed, compressed.Length);

        await Assert.That(Encoding.ASCII.GetString(inflated)).IsEqualTo(Payload);
    }

    [Test]
    public async Task ShouldInflateBareDeflateStream()
    {
        var compressed = DeflateCompress(Payload);

        var inflated = NntpCompressedBlock.Inflate(compressed, compressed.Length);

        await Assert.That(Encoding.ASCII.GetString(inflated)).IsEqualTo(Payload);
    }

    [Test]
    public async Task ShouldHonourLengthAndIgnoreTrailingBytes()
    {
        var compressed = GzipCompress(Payload);
        // Pad the buffer with junk past the valid length; only the first length bytes are read.
        var padded = new byte[compressed.Length + 16];
        compressed.CopyTo(padded, 0);

        var inflated = NntpCompressedBlock.Inflate(padded, compressed.Length);

        await Assert.That(Encoding.ASCII.GetString(inflated)).IsEqualTo(Payload);
    }

    [Test]
    public async Task ShouldThrowNntpExceptionOnCorruptPayload()
    {
        // A well-formed gzip header (so the gzip path is taken) over a corrupt DEFLATE body. The BCL
        // decompressor raises InvalidDataException on invalid codes, which the core wraps as an
        // NntpException so the failure surfaces on the affected command rather than dropping rows.
        var compressed = GzipCompress(Payload);
        for (var i = 13; i < compressed.Length - 8; i++)
        {
            compressed[i] ^= 0xff;
        }

        await Assert
            .That(() => NntpCompressedBlock.Inflate(compressed, compressed.Length))
            .ThrowsExactly<NntpException>();
    }

    private static byte[] GzipCompress(string text)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(Encoding.ASCII.GetBytes(text));
        }

        return output.ToArray();
    }

    private static byte[] ZlibCompress(string text)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(Encoding.ASCII.GetBytes(text));
        }

        return output.ToArray();
    }

    private static byte[] DeflateCompress(string text)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(Encoding.ASCII.GetBytes(text));
        }

        return output.ToArray();
    }
}
