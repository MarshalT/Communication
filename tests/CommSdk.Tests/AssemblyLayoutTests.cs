using CommSdk.Core.Managers;
using CommSdk.Devices.Drivers;
using CommSdk.Framework;
using CommSdk.Protocols.Modbus;
using CommSdk.Transports;
using Xunit;

namespace CommSdk.Tests
{
    public class AssemblyLayoutTests
    {
        [Fact]
        public void FrameworkNamespacesAreCompiledIntoOneAssembly()
        {
            // 合并后的框架只输出一个 CommSdk.dll，但仍保留按职责划分的命名空间。
            var assembly = typeof(CommClient).Assembly;

            Assert.Equal("CommSdk", assembly.GetName().Name);
            Assert.Same(assembly, typeof(DeviceDriverBase).Assembly);
            Assert.Same(assembly, typeof(CommClientFactory).Assembly);
            Assert.Same(assembly, typeof(ModbusRtuProtocol).Assembly);
            Assert.Same(assembly, typeof(TcpTransport).Assembly);
        }
    }
}
