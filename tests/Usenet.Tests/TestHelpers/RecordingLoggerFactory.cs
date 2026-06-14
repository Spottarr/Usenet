using Microsoft.Extensions.Logging;

namespace Usenet.Tests.TestHelpers;

/// <summary>
/// An <see cref="ILoggerFactory"/> that records the categories it is asked to create loggers for
/// and hands back a single shared <see cref="InMemoryLogger"/> so log entries can be inspected.
/// </summary>
internal sealed class RecordingLoggerFactory : ILoggerFactory
{
    public InMemoryLogger Logger { get; } = new();

    public List<string> Categories { get; } = [];

    public ILogger CreateLogger(string categoryName)
    {
        Categories.Add(categoryName);
        return Logger;
    }

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }
}
