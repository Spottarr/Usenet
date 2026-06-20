using System.IO.Pipelines;
using System.Text;
using Usenet.Nntp.Client;

namespace Usenet.Tests.Nntp;

internal sealed class NntpLineFramerTests
{
    private static NntpLineFramer FramerOver(string payload)
    {
        var bytes = Encoding.Latin1.GetBytes(payload);
        return new NntpLineFramer { Reader = PipeReader.Create(new MemoryStream(bytes)) };
    }

    [Test]
    public async Task ReadsCrlfTerminatedLinesAndStripsTerminator(CancellationToken token)
    {
        var framer = FramerOver("200 Welcome\r\nsecond line\r\n");

        await Assert.That(await framer.ReadLineAsync(token)).IsEqualTo("200 Welcome");
        await Assert.That(await framer.ReadLineAsync(token)).IsEqualTo("second line");
        await Assert.That(await framer.ReadLineAsync(token)).IsNull();
    }

    [Test]
    public async Task CountsBytesFramedOffTheReader(CancellationToken token)
    {
        var framer = FramerOver("abc\r\n");

        await framer.ReadLineAsync(token);

        await Assert.That(framer.BytesRead).IsEqualTo(5L);
    }

    [Test]
    public async Task ReadsLineWithoutTrailingCrlfAtEndOfStream(CancellationToken token)
    {
        var framer = FramerOver("no terminator");

        await Assert.That(await framer.ReadLineAsync(token)).IsEqualTo("no terminator");
        await Assert.That(await framer.ReadLineAsync(token)).IsNull();
    }

    [Test]
    public async Task DataBlockUndoesDotStuffingAndStopsAtTerminatingDot(CancellationToken token)
    {
        var framer = FramerOver("first\r\n..stuffed\r\n.\r\nafter\r\n");

        var (buffer, length) = await framer.ReadDataBlockToBufferAsync(token);
        try
        {
            var body = Encoding.Latin1.GetString(buffer, 0, length);
            await Assert.That(body).IsEqualTo("first\r\n.stuffed\r\n");
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }

        // The line after the terminating dot is still available on the reader.
        await Assert.That(await framer.ReadLineAsync(token)).IsEqualTo("after");
    }

    [Test]
    public async Task DataBlockLinesYieldsUndotStuffedLinesUntilTerminator(CancellationToken token)
    {
        var framer = FramerOver("alpha\r\n..beta\r\n.\r\n");

        var lines = new List<string>();
        await foreach (var line in framer.ReadDataBlockLinesAsync(token))
        {
            lines.Add(line);
        }

        await Assert.That(lines.Count).IsEqualTo(2);
        await Assert.That(lines[0]).IsEqualTo("alpha");
        await Assert.That(lines[1]).IsEqualTo(".beta");
    }

    [Test]
    [Arguments("plain", "plain")]
    [Arguments("..stuffed", ".stuffed")]
    public async Task ProcessLineUndoesDotStuffing(string input, string expected)
    {
        await Assert.That(NntpLineFramer.ProcessLine(input)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(".")]
    public async Task ProcessLineMapsTerminatingDotToNull(string input)
    {
        await Assert.That(NntpLineFramer.ProcessLine(input)).IsNull();
    }
}
