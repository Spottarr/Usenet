using Usenet.Nntp.Contracts;
using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Writers;

internal sealed class MockConnection : INntpConnection
{
    private readonly List<string> _lines = [];
    public void Dispose() { }
    public Task<TResponse> ConnectAsync<TResponse>(string hostname, int port, bool useSsl, IResponseParser<TResponse> parser, CancellationToken cancellationToken) => throw new NotImplementedException();
    public TResponse Command<TResponse>(string command, IResponseParser<TResponse> parser) => throw new NotImplementedException();
    public TResponse MultiLineCommand<TResponse>(string command, IMultiLineResponseParser<TResponse> parser) => throw new NotImplementedException();
    public void WriteLine(string line) => _lines.Add(line);
    public long BytesRead => 0;
    public long BytesWritten => 0;
    public void ResetCounters() => throw new NotImplementedException();
    public TResponse GetResponse<TResponse>(IResponseParser<TResponse> parser) => throw new NotImplementedException();
    public string[] GetLines() => _lines.ToArray();
}
