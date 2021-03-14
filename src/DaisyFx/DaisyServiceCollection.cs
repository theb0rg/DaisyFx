using System;
using System.Collections.Generic;
using System.Linq;
using DaisyFx.Events;
using DaisyFx.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DaisyFx
{
    public class DaisyServiceCollection : IDaisyServiceCollection
    {
        private readonly HashSet<string> _registeredModes = new();
        private readonly HashSet<object> _registeredChains = new();
        private readonly IServiceCollection _serviceCollection;
        private readonly IDaisyConfiguration _configuration;

        public DaisyServiceCollection(IDaisyConfiguration configuration, IServiceCollection serviceCollection)
        {
            _configuration = configuration;
            _serviceCollection = serviceCollection;
        }

        IServiceCollection IDaisyServiceCollection.ServiceCollection => _serviceCollection;
        IDaisyConfiguration IDaisyServiceCollection.Configuration => _configuration;

        public IDaisyServiceCollection AddHostMode<THostInterface>(string alias,
            Action<IDaisyServiceCollection, IServiceCollection> configureServices)
            where THostInterface : class, IHostInterface
        {
            if (!_registeredModes.Add(alias))
            {
                throw new NotSupportedException($"{nameof(alias)}: {alias} is already registered");
            }

            if (_configuration.HostMode.Equals(alias, StringComparison.OrdinalIgnoreCase))
            {
                _serviceCollection.TryAddSingleton<IHostInterface, THostInterface>();
                configureServices(this, _serviceCollection);
            }

            return this;
        }

        public IDaisyServiceCollection AddChain<TChainBuilder>()
            where TChainBuilder : class, IChainBuilder
        {
            if (!_registeredChains.Add(typeof(TChainBuilder)))
            {
                throw new NotSupportedException($"{typeof(TChainBuilder).Name} is already registered");
            }

            _serviceCollection.AddSingleton<TChainBuilder>();
            _serviceCollection.AddSingleton(s => s.GetRequiredService<TChainBuilder>().BuildChain(s));
            return this;
        }

        public IDaisyServiceCollection AddEventHandlerSingleton<TEventHandler>()
            where TEventHandler : class, IDaisyEventHandler
        {
            var handlerType = typeof(TEventHandler);

            var implementedEventHandlerInterfaces = handlerType
                .GetInterfaces()
                .Where(type =>
                    type.IsGenericType &&
                    (type.GetGenericTypeDefinition() == typeof(IDaisyEventHandler<>) ||
                     type.GetGenericTypeDefinition() == typeof(IDaisyEventHandlerAsync<>)));

            _serviceCollection.TryAddSingleton<TEventHandler>();

            // This delegate needs to be explicitly typed to make TryAddEnumerable work.
            Func<IServiceProvider, TEventHandler> serviceFactory = s => s.GetRequiredService<TEventHandler>();

            foreach (var handlerInterface in implementedEventHandlerInterfaces)
            {
                var descriptor = new ServiceDescriptor(handlerInterface, serviceFactory, ServiceLifetime.Singleton);
                _serviceCollection.TryAddEnumerable(descriptor);
            }

            return this;
        }
    }
}