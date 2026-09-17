using System;
using System.Collections.Generic;
using CommSdk.Core.Abstractions;

namespace CommSdk.Core.Registries
{
    public class DriverRegistry
    {
        private readonly IDictionary<string, IDeviceDriverFactory> _factories =
            new Dictionary<string, IDeviceDriverFactory>(StringComparer.OrdinalIgnoreCase);
        private readonly object _sync = new object();

        public void Register(IDeviceDriverFactory factory)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            if (string.IsNullOrEmpty(factory.Model)) throw new ArgumentException("Factory model is required", "factory");
            lock (_sync) _factories[factory.Model] = factory;
        }

        public IDeviceDriverFactory Resolve(string model)
        {
            if (model == null) throw new ArgumentNullException("model");
            lock (_sync)
            {
                IDeviceDriverFactory factory;
                if (!_factories.TryGetValue(model, out factory))
                    throw new KeyNotFoundException("Device model not registered: " + model);
                return factory;
            }
        }
    }
}
