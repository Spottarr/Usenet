using System.Net.Sockets;

namespace Usenet.Extensions;

internal static class TcpClientExtensions
{
    public static ValueTask ConnectAsync(this TcpClient client, string host, int port, CancellationToken cancellationToken)
    {
#if NET5_0_OR_GREATER
        return client.ConnectAsync(host, port, cancellationToken);
#else
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask(client.ConnectAsync(host, port));
#endif
    }
}
