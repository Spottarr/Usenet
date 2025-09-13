namespace Usenet.Nntp.Contracts;

/// <summary>
/// An NNTP client for which connections and authentication are managed by a pool.
/// </summary>
public interface IPooledNntpClient : INntpClientRfc2980, INntpClientRfc3977, INntpClientRfc6048, INntpClientCompression;
