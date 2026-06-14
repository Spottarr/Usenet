using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Usenet.Nntp;
using Usenet.Nntp.Contracts;

// Placed in the conventional DI namespace so consumers only need a single using.
namespace Usenet.DependencyInjection;

/// <summary>
/// Extension methods for registering the Usenet library with an <see cref="IServiceCollection"/>.
/// </summary>
[PublicAPI]
public static class UsenetServiceCollectionExtensions
{
    /// <summary>
    /// Registers the NNTP client and connection with the given <see cref="IServiceCollection"/>.
    /// The components resolve an <see cref="ILoggerFactory"/> from the container when one is
    /// registered, so logging flows through the application's existing logging configuration.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <returns>The same <paramref name="services"/> instance so that calls can be chained.</returns>
    public static IServiceCollection AddUsenet(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<INntpConnection>(sp => new NntpConnection(
            sp.GetService<ILoggerFactory>()
        ));

        services.TryAddTransient<INntpClient>(sp => new NntpClient(
            sp.GetRequiredService<INntpConnection>(),
            sp.GetService<ILoggerFactory>()
        ));

        return services;
    }
}
