using Usenet.Util;

namespace Usenet.Tests.Util;

internal sealed class CountingStreamTests
{
    [Test]
    public async Task CountingStreamShouldCountBytesRead()
    {
        using var memStream = new MemoryStream(new byte[10]);
        await using var stream = new CountingStream(memStream);
        stream.ReadByte();
        stream.ReadByte();
        stream.ReadByte();
        stream.ReadByte();
        stream.ReadByte();

        await Assert.That(stream.BytesRead).IsEqualTo(5);
    }

    [Test]
    public async Task CountingStreamShouldCountBytesWritten()
    {
        using var memStream = new MemoryStream(new byte[10]);
        await using var stream = new CountingStream(memStream);
        stream.WriteByte(1);
        stream.WriteByte(2);
        stream.WriteByte(3);
        stream.WriteByte(4);
        stream.WriteByte(5);

        await Assert.That(stream.BytesWritten).IsEqualTo(5);
    }

    [Test]
    public async Task ResetCountersShouldResetBytesReadAndBytesWritten()
    {
        using var memStream = new MemoryStream(new byte[10]);
        await using var stream = new CountingStream(memStream);
        stream.ReadByte();
        stream.ReadByte();
        stream.WriteByte(1);
        stream.WriteByte(2);

        await Assert.That(stream.BytesRead).IsEqualTo(2);
        await Assert.That(stream.BytesWritten).IsEqualTo(2);

        stream.ResetCounters();

        await Assert.That(stream.BytesRead).IsEqualTo(0);
        await Assert.That(stream.BytesWritten).IsEqualTo(0);
    }
}
