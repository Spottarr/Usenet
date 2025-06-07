namespace Usenet.Nntp.Contracts;

/// <summary>
/// An NNTP client that manages its own connection lifetime and authentication for pooling.
/// </summary>
public interface IPooledNntpClient : INntpClientRfc2980, INntpClientRfc3977, INntpClientRfc6048, INntpClientCompression, IDisposable
{

}
