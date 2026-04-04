using Microsoft.Extensions.FileProviders;
using Usenet.Nzb;
using Usenet.Tests.TestHelpers;

namespace Usenet.Tests.Nzb;

internal sealed class NzbBuilderTests
{
    [Test]
    [MethodDataSource(nameof(GetYencMultipartJoystickData))]
    internal async Task ShouldBuildDocumentFromFile(IFileInfo file)
    {
        var actualDocument = new NzbBuilder()
            .AddFile(file)
            .AddGroups("alt.binaries.newzbin")
            .AddGroups("alt.binaries.mojo")
            .AddMetaData("title", "joystick")
            .AddMetaData("tag", "image")
            .SetPoster("dummy@ignorethis.com")
            .SetMessageBase("ignorethis.com")
            .Build();
        await Assert.That(actualDocument.MetaData["title"].Single()).IsEqualTo("joystick");
    }

    public static IEnumerable<Func<IFileInfo>> GetYencMultipartJoystickData()
    {
        yield return () => EmbeddedResourceHelper.GetFileInfo("yenc.multipart.joystick.jpg");
    }
}
