using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Usenet.Tests.TestHelpers;

namespace Usenet.Tests.Util;

internal sealed class LoggerTests
{
    [Test]
    public async Task ShouldUseNullLogger()
    {
        Logger.Factory = null!;
        var logger = Logger.Create<LoggerTests>();
        var actualLogger = GetActualLogger(logger);

        await Assert.That(actualLogger).IsTypeOf<NullLogger>();
    }

    [Test]
    public async Task ShouldUseSetLogger()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(new InMemoryLogger());

        Logger.Factory = loggerFactory;
        var logger = Logger.Create<LoggerTests>();
        var actualLogger = GetActualLogger(logger);

        await Assert.That(actualLogger).IsTypeOf<InMemoryLogger>();
    }

    private static object? GetActualLogger(ILogger logger)
    {
        var loggerType = logger.GetType();
        var field = loggerType.GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(logger);
    }
}
