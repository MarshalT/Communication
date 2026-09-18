using System;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Models;

namespace CommSdk.Core.Configuration
{
    /// <summary>
    /// Resolves the supported transport/protocol combination for a device profile.
    /// </summary>
    public static class CommunicationProfileResolver
    {
        public static CommunicationMode Resolve(DeviceProfile profile)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            if (profile.Transport == null || profile.Protocol == null)
                throw new DeviceException("Transport and protocol configuration are required");

            if (Matches(profile.Transport.Type, "tcp") &&
                Matches(profile.Protocol.Type, "modbus-tcp"))
                return CommunicationMode.ModbusTcp;

            if (Matches(profile.Transport.Type, "serial") &&
                Matches(profile.Protocol.Type, "modbus-rtu"))
                return CommunicationMode.ModbusRtu;

            throw new DeviceException("Supported combinations are serial/modbus-rtu and tcp/modbus-tcp");
        }

        private static bool Matches(string value, string expected)
        {
            return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
