using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Usenet.Tests.Extensions;

internal static class FileInfoExtensions
{
    public static string ReadAllText(this IFileInfo fileInfo, Encoding encoding)
    {
        using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream, encoding);
        return reader.ReadToEnd();
    }

    public static List<string> ReadAllLines(this IFileInfo fileInfo, Encoding encoding)
    {
        using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream, encoding);

        string? line;
        var lines = new List<string>();
        while ((line = reader.ReadLine()) != null)
        {
            lines.Add(line);
        }

        return lines;
    }

    public static byte[] ReadAllBytes(this IFileInfo fileInfo)
    {
        using var stream = fileInfo.CreateReadStream();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}