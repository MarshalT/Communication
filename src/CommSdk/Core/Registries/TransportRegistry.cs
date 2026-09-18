using System;
using System.Collections.Generic;
using CommSdk.Core.Abstractions;

namespace CommSdk.Core.Registries
{
    public class TransportRegistry
    {
        private readonly IDictionary<string, ITransportFactory> _factories =
            new Dictionary<string, ITransportFactory>(StringComparer.OrdinalIgnoreCase);
        private readonly object _sync = new object();

        public void Register(ITransportFactory factory)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            if (string.IsNullOrEmpty(factory.Type)) throw new ArgumentException("Factory type is required", "factory");
            lock (_sync) _factories[factory.Type] = factory;
        }

        public ITransportFactory Resolve(string type)
        {
            if (type == null) throw new ArgumentNullException("type");
            lock (_sync)
            {
                ITransportFactory factory;
                if (!_factories.TryGetValue(type, out factory))
                    throw new KeyNotFoundException("Transport type not registered: " + type);
                return factory;
            }
        }
    }
}
