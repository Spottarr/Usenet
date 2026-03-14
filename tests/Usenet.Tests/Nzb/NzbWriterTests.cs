using Microsoft.Extensions.FileProviders;
using Usenet.Nzb;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;
using Xunit;

namespace Usenet.Tests.Nzb;

public class NzbWriterTests
{
    [Theory]
    [EmbeddedResourceData(@"nzb.sabnzbd.nzb")]
    [EmbeddedResourceData(@"nzb.sabnzbd-no-namespace.nzb")]
    internal async Task ShouldWriteDocumentToFile(IFileInfo file)
    {
        var expected = await NzbParser
            .ParseAsync(
                file.ReadAllText(UsenetEncoding.Default),
                TestContext.Current.CancellationToken
            )
            .ConfigureAwait(true);

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, UsenetEncoding.Default);
        using var reader = new StreamReader(stream, UsenetEncoding.Default);

        // write to file and read back for comparison
        await writer
            .WriteNzbDocumentAsync(expected, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        stream.Position = 0;
        var actual = await NzbParser
            .ParseAsync(reader, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        // compare
        Assert.Equal(expected, actual);
    }
}
