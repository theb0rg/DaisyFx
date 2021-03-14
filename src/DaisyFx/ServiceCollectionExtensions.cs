using DaisyFx;
using DaisyFx.Events;
using DaisyFx.Hosting;
using DaisyFx.Sources.Http;
using Microsoft.Extensions.Configuration;
using System;

// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDaisy(this IServiceCollection serviceCollection,
            Action<IDaisyServiceCollection> configureDaisy)
        {
            var provider = serviceCollection.BuildServiceProvider();
            var configuration = new DaisyConfiguration(provider.GetService<IConfiguration>()?.GetValue<string>("daisy:mode"));
            configureDaisy(new DaisyServiceCollection(configuration, serviceCollection));

            serviceCollection
                .AddSingleton<IDaisyConfiguration>(configuration)
                .AddSingleton<HttpChainRouter>()
                .AddSingleton(typeof(EventHandlerCollection<>))
                .AddHostedService(s =>
            {
                if (s.GetService<IHostInterface>() is { } hostInterface)
                    return hostInterface;

                var configuration = s.GetRequiredService<IDaisyConfiguration>();                

                return configuration.HostMode switch
                {
                    ConsoleHostInterface.Mode => ActivatorUtilities.CreateInstance<ConsoleHostInterface>(s),
                    ServiceHostInterface.Mode => ActivatorUtilities.CreateInstance<ServiceHostInterface>(s),
                    _ => throw new ArgumentException($"{configuration.HostMode} is not a valid value.", nameof(configuration.HostMode))
                };
            });

            return serviceCollection;
        }
    }
}