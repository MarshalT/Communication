using System;
using System.Collections.Generic;
using System.Reflection;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Registries;

namespace CommSdk.Devices.Drivers
{
    public static class DeviceDriverAssemblyLoader
    {
        public static int RegisterAll(string assemblyPath, DriverRegistry registry)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath)) throw new ArgumentException("Assembly path is required", "assemblyPath");
            if (registry == null) throw new ArgumentNullException("registry");

            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex)
            {
                throw new DeviceException("Failed to load driver assembly", ex);
            }

            var registered = 0;
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type.IsAbstract || !typeof(IDeviceDriver).IsAssignableFrom(type)) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                IDeviceDriver driver;
                try
                {
                    driver = (IDeviceDriver)Activator.CreateInstance(type);
                }
                catch (Exception ex)
                {
                    throw new DeviceException("Failed to instantiate driver type: " + type.FullName, ex);
                }
                if (string.IsNullOrWhiteSpace(driver.Model)) continue;

                registry.Register(new ReflectionDeviceDriverFactory(driver.Model, type));
                registered++;
            }
            return registered;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var types = new List<Type>();
                foreach (var type in ex.Types)
                    if (type != null) types.Add(type);
                return types;
            }
        }
    }
}
