using System.Text;
using JetBrains.Annotations;

namespace Usenet.Util;

/// <summary>
/// This class defines the default usenet character encoding.
/// </summary>
[PublicAPI]
public static class UsenetEncoding
{
    /// <summary>
    /// Returns iso-8859-1 (Latin-1), the default usenet character encoding. On modern .NET this is the
    /// static, allocation-free <see cref="Encoding.Latin1"/> rather than a code-page lookup.
    /// </summary>
    public static Encoding Default { get; } = Encoding.Latin1;
}
