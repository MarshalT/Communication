using System;
using System.Collections.Generic;
using CommSdk.Core.Abstractions;

namespace CommSdk.Core.Registries
{
    public class ProtocolRegistry
    {
        private readonly IDictionary<string, IProtocolFactory> _factories =
            new Dictionary<string, IProtocolFactory>(StringComparer.OrdinalIgnoreCase);
        private readonly object _sync = new object();

        public void Register(IProtocolFactory factory)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            if (string.IsNullOrEmpty(factory.Type)) throw new ArgumentException("Factory type is required", "factory");
            lock (_sync) _factories[factory.Type] = factory;
        }

        public IProtocolFactory Resolve(string type)
        {
            if (type == null) throw new ArgumentNullException("type");
            lock (_sync)
            {
                IProtocolFactory factory;
                if (!_factories.TryGetValue(type, out factory))
                    throw new KeyNotFoundException("Protocol type not registered: " + type);
                return factory;
            }
        }
    }
}
