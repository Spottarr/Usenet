using System.Globalization;
using System.Xml;
using Microsoft.Extensions.FileProviders;
using Usenet.Exceptions;
using Usenet.Nzb;
using Usenet.Util;
using UsenetTests.Extensions;
using UsenetTests.TestHelpers;
using Xunit;

namespace UsenetTests.Nzb;

public class NzbParserTests
{
    [Theory]
    [EmbeddedResourceData(@"nzb.sabnzbd.nzb")]
    [EmbeddedResourceData(@"nzb.sabnzbd-no-namespace.nzb")]
    internal void ValidNzbDataShouldBeParsed(IFileInfo file)
    {
        var nzbData = file.ReadAllText(UsenetEncoding.Default);
        var actualDocument = NzbParser.Parse(nzbData);

        Assert.Equal("Your File!", actualDocument.MetaData["title"].Single());
        Assert.Equal("secret", actualDocument.MetaData["password"].Single());
        Assert.Equal("HD", actualDocument.MetaData["tag"].Single());
        Assert.Equal("TV", actualDocument.MetaData["category"].Single());
        Assert.Equal(106895, actualDocument.Size);
    }

    [Fact]
    internal void MinimalNzbDataShouldBeParsed()
    {
        const string nzbText = @"<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb""></nzb>";
        var actualDocument = NzbParser.Parse(nzbText);

        Assert.Empty(actualDocument.MetaData);
        Assert.Empty(actualDocument.Files);
    }

    [Fact]
    internal void MultipleMetaDataKeysShouldBeParsed()
    {
        const string nzbText = @"
<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb"">
  <head>
    <meta type=""tag"">SD</meta>
    <meta type=""tag"">avi</meta>
  </head>
</nzb>";
        var actualDocument = NzbParser.Parse(nzbText);

        Assert.Single(actualDocument.MetaData);
        Assert.Equal(2, actualDocument.MetaData["tag"].Count);
        Assert.NotNull(actualDocument.MetaData["tag"].SingleOrDefault(m => m == "SD"));
        Assert.NotNull(actualDocument.MetaData["tag"].SingleOrDefault(m => m == "avi"));
    }

    [Fact]
    internal void MinimalFileShouldBeParsed()
    {
        const string nzbText = @"
<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb"">
  <file></file>
</nzb>";

        var actualDocument = NzbParser.Parse(nzbText);

        Assert.Empty(actualDocument.MetaData);
        Assert.Single(actualDocument.Files);
    }

    [Fact]
    internal void FileDateShouldBeParsed()
    {
        var expected = DateTimeOffset.Parse(@"2017-06-01T06:49:13+00:00", CultureInfo.InvariantCulture);
        const string nzbText = @"
<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb"">
  <file date=""1496299753""></file>
</nzb>";

        var actualDocument = NzbParser.Parse(nzbText);
        Assert.Equal(expected, actualDocument.Files[0].Date);
    }

    [Fact]
    internal void InvalidFileDateShouldThrow()
    {
        const string nzbText = @"
<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb"">
  <file date=""1496xxx753""></file>
</nzb>";

        Assert.Throws<InvalidNzbDataException>(() => NzbParser.Parse(nzbText));
    }

    [Fact]
    internal void InvalidSegmentNumberShouldThrow()
    {
        const string nzbText = @"
<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb"">
  <file>
    <segments>
      <segment number=""123ffg45""></segment>
    </segments>
  </file>
</nzb>";

        Assert.Throws<InvalidNzbDataException>(() => NzbParser.Parse(nzbText));
    }

    [Fact]
    internal void MissingSegmentNumberShouldThrow()
    {
        const string nzbText = @"
<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb"">
  <file>
    <segments>
      <segment bytes=""1000""></segment>
    </segments>
  </file>
</nzb>";

        Assert.Throws<InvalidNzbDataException>(() => NzbParser.Parse(nzbText));
    }


    [Fact]
    internal void InvalidSegmentSizeShouldThrow()
    {
        const string nzbText = @"
<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb"">
  <file>
    <segments>
      <segment bytes=""123ffg45""></segment>
    </segments>
  </file>
</nzb>";

        Assert.Throws<InvalidNzbDataException>(() => NzbParser.Parse(nzbText));
    }

    [Fact]
    internal void MissingSegmentSizeShouldThrow()
    {
        const string nzbText = @"
<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb"">
  <file>
    <segments>
      <segment number=""1""></segment>
    </segments>
  </file>
</nzb>";

        Assert.Throws<InvalidNzbDataException>(() => NzbParser.Parse(nzbText));
    }

    [Fact]
    internal void InvalidXmlShouldThrow()
    {
        const string nzbText = @"sdfsfasfasdfasdf";
        Assert.Throws<XmlException>(() => NzbParser.Parse(nzbText));
    }

    [Fact]
    public void InvalidNzbShouldThrow()
    {
        const string nzbText = @"<html></html>";
        Assert.Throws<InvalidNzbDataException>(() => NzbParser.Parse(nzbText));
    }

    [Fact]
    internal void FileShouldBeExtractedFromSubjectWhenQuoted()
    {
        const string nzbText = @"
<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb"">
  <file subject=""(TWD151 - 153)[2 / 9] - &quot;TWD151 - 153.rar&quot; yEnc (001 / 249)""></file>
</nzb>";

        var actualDocument = NzbParser.Parse(nzbText);
        Assert.Equal("TWD151 - 153.rar", actualDocument.Files.Single().FileName);
    }

    [Fact]
    internal void FileShouldBeExtractedFromSubjectWhenNotQuoted()
    {
        const string nzbText = @"
<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb"">
  <file subject=""(TWD151 - 153)[2 / 9] - TWD151 - 153.rar yEnc (001 / 249)""></file>
</nzb>";

        var actualDocument = NzbParser.Parse(nzbText);
        Assert.Equal("(TWD151 - 153)[2 / 9] - TWD151 - 153.rar", actualDocument.Files.Single().FileName);
    }

    [Fact]
    internal void FileShouldBeExtractedFromSubjectWhenNotQuotedAndNoParenthesis()
    {
        const string nzbText = @"
<nzb xmlns=""http://www.newzbin.com/DTD/2003/nzb"">
  <file subject=""[2 / 9] - TWD151 - 153.rar yEnc""></file>
</nzb>";

        var actualDocument = NzbParser.Parse(nzbText);
        Assert.Equal("[2 / 9] - TWD151 - 153.rar", actualDocument.Files.Single().FileName);
    }
}