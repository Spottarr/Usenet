using JetBrains.Annotations;

namespace Usenet.Nntp.Contracts;

[PublicAPI]
public interface INntpClient
    : INntpClientRfc2980,
        INntpClientRfc3977,
        INntpClientRfc4643,
        INntpClientRfc6048,
        INntpClientCompression,
        INntpClientConnection
{
    // Intentionally left blank
}
