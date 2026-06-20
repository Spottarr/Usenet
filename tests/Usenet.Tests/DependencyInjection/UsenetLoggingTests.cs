using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Usenet.DependencyInjection;
using Usenet.Nntp.Builders;
using Usenet.Nntp.Client;
using Usenet.Nntp.Client.Pooling;
using Usenet.Nntp.Contracts;
using Usenet.Nntp.Parsers;
using Usenet.Tests.TestHelpers;

namespace Usenet.Tests.DependencyInjection;

internal sealed class UsenetLoggingTests
{
    [Test]
    public async Task ComponentsConstructWithoutLoggerFactory()
    {
        using var connection = new NntpConnection();
        var client = new NntpClient(connection);
        var builder = new NntpArticleBuilder();
        using var pool = new NntpClientPool(
            new NntpPoolOptions
            {
                MaxPoolSize = 1,
                Username = "user",
                Password = "pass",
                Connection = new NntpConnectionOptions { Host = "example.server" },
            }
        );

        await Assert.That(client).IsNotNull();
        await Assert.That(builder).IsNotNull();
        await Assert.That(pool).IsNotNull();
    }

    [Test]
    public async Task ParserUsesInjectedLoggerFactory()
    {
        using var factory = new RecordingLoggerFactory();
        var parser = new DateResponseParser(factory);

        // An unparseable DATE response triggers a warning through the injected logger.
        parser.Parse(111, "not-a-date");

        await Assert.That(factory.Logger.Buffer).IsNotEmpty();
        await Assert.That(factory.Categories).Contains("Usenet.Nntp.Parsers.DateResponseParser");
    }

    [Test]
    public async Task ParserDoesNotThrowWithoutLoggerFactory()
    {
        var response = new DateResponseParser().Parse(111, "not-a-date");

        await Assert.That(response.Success).IsFalse();
    }

    [Test]
    public async Task AddUsenetRegistersNntpClientAndConnection()
    {
        var services = new ServiceCollection();
        services.AddUsenet();

        await using var provider = services.BuildServiceProvider();

        await Assert.That(provider.GetService<INntpConnection>()).IsNotNull();
        await Assert.That(provider.GetService<INntpClient>()).IsNotNull();
    }

    [Test]
    public async Task AddUsenetWiresLoggerFactoryFromContainer()
    {
        using var factory = new RecordingLoggerFactory();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(factory);
        services.AddUsenet();

        await using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<INntpConnection>();

        await Assert.That(factory.Categories).Contains("Usenet.Nntp.Client.NntpConnection");
    }

    [Test]
    public async Task AddUsenetWorksWithoutRegisteredLoggerFactory()
    {
        var services = new ServiceCollection();
        services.AddUsenet();

        await using var provider = services.BuildServiceProvider();

        await Assert.That(provider.GetRequiredService<INntpClient>()).IsNotNull();
    }
}
