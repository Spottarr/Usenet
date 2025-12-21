using Microsoft.Extensions.Logging;

namespace Usenet.Tests.TestHelpers;

internal sealed class InMemoryLogger : ILogger
{
    internal sealed class Entry
    {
        public LogLevel LogLevel;
        public EventId EventId;
        public string? Message;
    }

    public LogLevel MinLogLevel { get; set; }

    public List<Entry> Buffer { get; }

    public InMemoryLogger()
    {
        MinLogLevel = LogLevel.Trace;
        Buffer = new List<Entry>();
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= MinLogLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            var str = formatter(state, exception);
            Buffer.Add(new Entry { LogLevel = logLevel, EventId = eventId, Message = str });
        }
    }

    public void FlushTo(ILogger logger)
    {
        foreach (var entry in Buffer)
        {
#pragma warning disable CA1848
#pragma warning disable CA2254
            logger.Log(entry.LogLevel, entry.EventId, entry.Message);
#pragma warning restore CA2254
#pragma warning restore CA1848
        }

        Buffer.Clear();
    }
}
