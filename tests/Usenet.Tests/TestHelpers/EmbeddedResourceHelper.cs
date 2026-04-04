using Microsoft.Extensions.FileProviders;

namespace Usenet.Tests.TestHelpers;

internal static class EmbeddedResourceHelper
{
    private static readonly EmbeddedFileProvider FileProvider = new(
        typeof(EmbeddedResourceHelper).Assembly,
        "Usenet.Tests.testdata"
    );

    public static IFileInfo GetFileInfo(string fileName) => FileProvider.GetFileInfo(fileName);
}
