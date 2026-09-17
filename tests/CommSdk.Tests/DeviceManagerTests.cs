using System.Collections.Generic;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Managers;
using CommSdk.Core.Models;
using CommSdk.Core.Registries;
using Xunit;

namespace CommSdk.Tests
{
    public class DeviceManagerTests
    {
        [Fact]
        public void CreatesTracksAndDisposesSessionWithClientContext()
        {
            var transport = new FakeTransport();
            var driver = new CapturingDriver();
            var transports = new TransportRegistry();
            transports.Register(new TransportFactory(transport));
            var protocols = new ProtocolRegistry();
            protocols.Register(new ProtocolFactory());
            var drivers = new DriverRegistry();
            drivers.Register(new DriverFactory(driver));

            var profile = new DeviceProfile
            {
                Id = "d1",
                Model = "test-device",
                Transport = new TransportConfig { Type = "fake", Parameters = new Dictionary<string, string>() },
                Protocol = new ProtocolConfig { Type = "test", Parameters = new Dictionary<string, string>() }
            };

            using (var manager = new DeviceManager(transports, protocols, drivers))
            {
                var session = manager.Create(profile);
                Assert.Same(session, manager.Get("D1"));
                Assert.Same(session.Client, driver.Context.Client);
                Assert.Single(manager.Sessions);
                Assert.True(manager.Remove("d1"));
                Assert.Empty(manager.Sessions);
            }

            Assert.True(transport.CloseCount >= 1);
        }

        private sealed class TransportFactory : ITransportFactory
        {
            private readonly ITransport _transport;
            public TransportFactory(ITransport transport) { _transport = transport; }
            public string Type { get { return "fake"; } }
            public ITransport Create(TransportConfig config) { return _transport; }
        }

        private sealed class ProtocolFactory : IProtocolFactory
        {
            public string Type { get { return "test"; } }
            public IProtocol Create(ProtocolConfig config) { return new TestProtocol(); }
        }

        private sealed class DriverFactory : IDeviceDriverFactory
        {
            private readonly CapturingDriver _driver;
            public DriverFactory(CapturingDriver driver) { _driver = driver; }
            public string Model { get { return "test-device"; } }
            public IDeviceDriver Create(DeviceProfile profile) { return _driver; }
        }

        private sealed class TestProtocol : IProtocol
        {
            public string Name { get { return "test"; } }
            public byte[] Encode(IProtocolRequest request) { return new byte[] { 1 }; }
            public IProtocolResponse Decode(byte[] frame) { return new TestResponse(); }
        }

        private sealed class TestResponse : IProtocolResponse { }

        private sealed class CapturingDriver : IDeviceDriver
        {
            public DeviceContext Context { get; private set; }
            public string Model { get { return "test-device"; } }
            public void Initialize(DeviceContext context) { Context = context; }
            public object Read(DeviceCommand command) { return null; }
            public void Write(DeviceCommand command, object value) { }
            public object Execute(DeviceCommand command) { return null; }
        }
    }
}
