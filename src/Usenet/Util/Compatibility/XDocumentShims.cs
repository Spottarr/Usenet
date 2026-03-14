using System.Xml.Linq;

namespace Usenet.Util.Compatibility;

internal static class XDocumentShims
{
    internal static Task<XDocument> LoadAsync(Stream stream, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        return Task.FromResult(XDocument.Load(stream));
#else
        return XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
#endif
    }

    internal static Task<XDocument> LoadAsync(
        TextReader reader,
        CancellationToken cancellationToken
    )
    {
#if NETSTANDARD2_0
        return Task.FromResult(XDocument.Load(reader));
#else
        return XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
#endif
    }
}
