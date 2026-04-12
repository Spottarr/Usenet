using System.Globalization;
using System.Xml;
using Microsoft.Extensions.FileProviders;
using Usenet.Exceptions;
using Usenet.Nzb;
using Usenet.Tests.Extensions;
using Usenet.Tests.TestHelpers;
using Usenet.Util;

namespace Usenet.Tests.Nzb;

internal sealed class NzbParserTests
{
    [Test]
    [MethodDataSource(nameof(GetValidNzbFiles))]
    internal async Task ValidNzbDataShouldBeParsed(
        IFileInfo file,
        CancellationToken cancellationToken
    )
    {
        var nzbData = file.ReadAllText(UsenetEncoding.Default);
        var actualDocument = await NzbParser.ParseAsync(nzbData, cancellationToken);

        await Assert.That(actualDocument.MetaData["title"].Single()).IsEqualTo("Your File!");
        await Assert.That(actualDocument.MetaData["password"].Single()).IsEqualTo("secret");
        await Assert.That(actualDocument.MetaData["tag"].Single()).IsEqualTo("HD");
        await Assert.That(actualDocument.MetaData["category"].Single()).IsEqualTo("TV");
        await Assert.That(actualDocument.Size).IsEqualTo(106895);
    }

    public static IEnumerable<Func<IFileInfo>> GetValidNzbFiles()
    {
        yield return () => EmbeddedResourceHelper.GetFileInfo("nzb.sabnzbd.nzb");
        yield return () => EmbeddedResourceHelper.GetFileInfo("nzb.sabnzbd-no-namespace.nzb");
    }

    [Test]
    internal async Task MinimalNzbDataShouldBeParsed(CancellationToken cancellationToken)
    {
        const string nzbText = """<nzb xmlns="http://www.newzbin.com/DTD/2003/nzb"></nzb>""";
        var actualDocument = await NzbParser.ParseAsync(nzbText, cancellationToken);

        await Assert.That(actualDocument.MetaData).IsEmpty();
        await Assert.That(actualDocument.Files).IsEmpty();
    }

    [Test]
    internal async Task MultipleMetaDataKeysShouldBeParsed(CancellationToken cancellationToken)
    {
        const string nzbText = """
            <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
              <head>
                <meta type="tag">SD</meta>
                <meta type="tag">avi</meta>
              </head>
            </nzb>
            """;
        var actualDocument = await NzbParser.ParseAsync(nzbText, cancellationToken);

        await Assert.That(actualDocument.MetaData).HasSingleItem();
        await Assert.That(actualDocument.MetaData["tag"].Count).IsEqualTo(2);
        await Assert
            .That(actualDocument.MetaData["tag"].SingleOrDefault(m => m == "SD"))
            .IsNotNull();
        await Assert
            .That(actualDocument.MetaData["tag"].SingleOrDefault(m => m == "avi"))
            .IsNotNull();
    }

    [Test]
    internal async Task MinimalFileShouldBeParsed(CancellationToken cancellationToken)
    {
        const string nzbText = """
            <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
              <file></file>
            </nzb>
            """;

        var actualDocument = await NzbParser.ParseAsync(nzbText, cancellationToken);

        await Assert.That(actualDocument.MetaData).IsEmpty();
        await Assert.That(actualDocument.Files).HasSingleItem();
    }

    [Test]
    internal async Task FileDateShouldBeParsed(CancellationToken cancellationToken)
    {
        var expected = DateTimeOffset.Parse(
            @"2017-06-01T06:49:13+00:00",
            CultureInfo.InvariantCulture
        );
        const string nzbText = """
            <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
              <file date="1496299753"></file>
            </nzb>
            """;

        var actualDocument = await NzbParser.ParseAsync(nzbText, cancellationToken);
        await Assert.That(actualDocument.Files[0].Date).IsEqualTo(expected);
    }

    [Test]
    internal async Task InvalidFileDateShouldThrow(CancellationToken cancellationToken)
    {
        const string nzbText = """
            <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
              <file date="1496xxx753"></file>
            </nzb>
            """;

        await Assert
            .That(async () => await NzbParser.ParseAsync(nzbText, cancellationToken))
            .ThrowsExactly<InvalidNzbDataException>();
    }

    [Test]
    internal async Task InvalidSegmentNumberShouldThrow(CancellationToken cancellationToken)
    {
        const string nzbText = """
            <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
              <file>
                <segments>
                  <segment number="123ffg45"></segment>
                </segments>
              </file>
            </nzb>
            """;

        await Assert
            .That(async () => await NzbParser.ParseAsync(nzbText, cancellationToken))
            .ThrowsExactly<InvalidNzbDataException>();
    }

    [Test]
    internal async Task MissingSegmentNumberShouldThrow(CancellationToken cancellationToken)
    {
        const string nzbText = """
            <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
              <file>
                <segments>
                  <segment bytes="1000"></segment>
                </segments>
              </file>
            </nzb>
            """;

        await Assert
            .That(async () => await NzbParser.ParseAsync(nzbText, cancellationToken))
            .ThrowsExactly<InvalidNzbDataException>();
    }

    [Test]
    internal async Task InvalidSegmentSizeShouldThrow(CancellationToken cancellationToken)
    {
        const string nzbText = """
            <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
              <file>
                <segments>
                  <segment bytes="123ffg45"></segment>
                </segments>
              </file>
            </nzb>
            """;

        await Assert
            .That(async () => await NzbParser.ParseAsync(nzbText, cancellationToken))
            .ThrowsExactly<InvalidNzbDataException>();
    }

    [Test]
    internal async Task MissingSegmentSizeShouldThrow(CancellationToken cancellationToken)
    {
        const string nzbText = """
            <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
              <file>
                <segments>
                  <segment number="1"></segment>
                </segments>
              </file>
            </nzb>
            """;

        await Assert
            .That(async () => await NzbParser.ParseAsync(nzbText, cancellationToken))
            .ThrowsExactly<InvalidNzbDataException>();
    }

    [Test]
    internal async Task InvalidXmlShouldThrow(CancellationToken cancellationToken)
    {
        const string nzbText = "sdfsfasfasdfasdf";
        await Assert
            .That(async () => await NzbParser.ParseAsync(nzbText, cancellationToken))
            .ThrowsExactly<XmlException>();
    }

    [Test]
    internal async Task InvalidNzbShouldThrow(CancellationToken cancellationToken)
    {
        const string nzbText = "<html></html>";
        await Assert
            .That(async () => await NzbParser.ParseAsync(nzbText, cancellationToken))
            .ThrowsExactly<InvalidNzbDataException>();
    }

    [Test]
    internal async Task FileShouldBeExtractedFromSubjectWhenQuoted(
        CancellationToken cancellationToken
    )
    {
        const string nzbText = """
            <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
              <file subject="(TWD151 - 153)[2 / 9] - &quot;TWD151 - 153.rar&quot; yEnc (001 / 249)"></file>
            </nzb>
            """;

        var actualDocument = await NzbParser.ParseAsync(nzbText, cancellationToken);
        await Assert.That(actualDocument.Files.Single().FileName).IsEqualTo("TWD151 - 153.rar");
    }

    [Test]
    internal async Task FileShouldBeExtractedFromSubjectWhenNotQuoted(
        CancellationToken cancellationToken
    )
    {
        const string nzbText = """
            <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
              <file subject="(TWD151 - 153)[2 / 9] - TWD151 - 153.rar yEnc (001 / 249)"></file>
            </nzb>
            """;

        var actualDocument = await NzbParser.ParseAsync(nzbText, cancellationToken);
        await Assert
            .That(actualDocument.Files.Single().FileName)
            .IsEqualTo("(TWD151 - 153)[2 / 9] - TWD151 - 153.rar");
    }

    [Test]
    internal async Task FileShouldBeExtractedFromSubjectWhenNotQuotedAndNoParenthesis(
        CancellationToken cancellationToken
    )
    {
        const string nzbText = """
            <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
              <file subject="[2 / 9] - TWD151 - 153.rar yEnc"></file>
            </nzb>
            """;

        var actualDocument = await NzbParser.ParseAsync(nzbText, cancellationToken);
        await Assert
            .That(actualDocument.Files.Single().FileName)
            .IsEqualTo("[2 / 9] - TWD151 - 153.rar");
    }
}
