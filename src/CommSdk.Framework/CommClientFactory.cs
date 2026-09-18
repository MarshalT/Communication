using System;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Configuration;
using CommSdk.Core.Managers;
using CommSdk.Core.Models;
using CommSdk.Protocols.Modbus.Factories;
using CommSdk.Transports.Factories;

namespace CommSdk.Framework
{
    /// <summary>
    /// Creates a communication client from a device profile.
    /// </summary>
    public static class CommClientFactory
    {
        public static CommClient Create(DeviceProfile profile)
        {
            var mode = CommunicationProfileResolver.Resolve(profile);
            var isTcpModbus = mode == CommunicationMode.ModbusTcp;

            var transportFactory = isTcpModbus
                ? (ITransportFactory)new TcpTransportFactory()
                : new SerialTransportFactory();
            var protocolFactory = isTcpModbus
                ? (IProtocolFactory)new ModbusTcpProtocolFactory()
                : new ModbusRtuProtocolFactory();

            ITransport transport = null;
            CommClient client = null;
            try
            {
                transport = transportFactory.Create(profile.Transport);
                var protocol = protocolFactory.Create(profile.Protocol);
                client = new CommClient(transport, protocol, CreateClientOptions(profile.Retry));
                return client;
            }
            catch
            {
                if (client != null) client.Dispose();
                else if (transport != null) transport.Dispose();
                throw;
            }
        }

        private static CommClientOptions CreateClientOptions(RetryConfig retry)
        {
            var options = new CommClientOptions();
            if (retry == null) return options;

            options.RetryCount = Math.Max(0, retry.Count);
            options.TimeoutMs = Math.Max(0, retry.TimeoutMs);
            options.RetryDelayMs = Math.Max(0, retry.DelayMs > 0 ? retry.DelayMs : options.RetryDelayMs);
            options.ExponentialBackoff = retry.ExponentialBackoff;
            return options;
        }
    }
}
