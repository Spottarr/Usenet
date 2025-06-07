using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Usenet.Tests.TestHelpers;
using Xunit;

namespace Usenet.Tests;

public class LoggerTests
{
    [Fact]
    public void ShouldUseNullLogger()
    {
        Usenet.Logger.Factory = null;
        var logger = Usenet.Logger.Create<LoggerTests>();
        var actualLogger = GetActualLogger(logger);

        Assert.IsType<NullLogger>(actualLogger);
    }

    [Fact]
    public void ShouldUseSetLogger()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(new InMemoryLogger());

        Usenet.Logger.Factory = loggerFactory;
        var logger = Usenet.Logger.Create<LoggerTests>();
        var actualLogger = GetActualLogger(logger);

        Assert.IsType<InMemoryLogger>(actualLogger);
    }

    private static object? GetActualLogger(ILogger logger)
    {
        var loggerType = logger.GetType();
        var field = loggerType.GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(logger);
    }
}
