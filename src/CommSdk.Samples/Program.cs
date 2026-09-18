using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CommSdk.Core.Configuration;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Logging;
using CommSdk.Core.Models;

namespace CommSdk.Samples
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            LogManager.Current = new ConsoleLog();

            var profilePath =
     //@"C:\Users\Administrator\source\repos\Communication\docs\examples\electricity-meter-tcp-profile.json";
            @"C:\Users\Administrator\source\repos\Communication\docs\examples\electricity-meter-profile.json";

            var profile = DeviceProfileJsonLoader.Load(File.ReadAllText(profilePath));


            //var profile = LoadProfile(args);
            // ElectricityMeterClient 已经封装了串口/TCP 和对应的 Modbus 通信。
            using (var meter = ElectricityMeterClient.Create(profile))
            {
                try
                {
                    // 显式打开连接，便于展示设备会话的生命周期。
                    meter.Open();
                    // 客户端会发送 03/04 读寄存器请求并返回换算后的 kWh 数值。


                    for (int i = 0; i < 10; i++)
                    {
                        var energy = meter.ReadEnergy();
                        Console.WriteLine("Energy read: " + energy.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " kWh");
                    }

                    for (int i = 0; i < 10; i++)
                    {
                        Thread.Sleep(1000);
                        meter.test();
                    }
                    // Console.WriteLine("累计电量: " + energy.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " kWh");
                }
                catch (CommException ex)
                {
                    Console.Error.WriteLine("电表通信失败: " + ex.Message);
                    Environment.ExitCode = 1;
                }
            }
        }

        private static DeviceProfile LoadProfile(string[] args)
        {
            // 传入配置文件时：dotnet run --project src/CommSdk.Samples -- profile.json
            if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                return DeviceProfileJsonLoader.Load(File.ReadAllText(Path.GetFullPath(args[0])));

            // 未传入文件时使用下面的默认配置；实际使用时请修改 COM3 和电量寄存器参数。
            return new DeviceProfile
            {
                Id = "meter-1",
                Model = "electricity-meter",
                Custom = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // 电量寄存器地址、寄存器数量、倍率和字节序必须以电表手册为准。
                    { "energyAddress", "0" },
                    { "energyQuantity", "2" },
                    { "energyScale", "0.01" },
                    { "energyByteOrder", "UInt32BE" }
                },
                Transport = new TransportConfig
                {
                    Type = "serial",
                    Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "port", "COM3" },
                        { "baud", "9600" },
                        { "databits", "8" },
                        { "parity", "None" },
                        { "stopbits", "One" },
                        { "readTimeoutMs", "1000" },
                        { "writeTimeoutMs", "1000" }
                    }
                },
                Protocol = new ProtocolConfig
                {
                    Type = "modbus-tcp",
                    Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "station", "1" }
                    }
                },
                Retry = new RetryConfig { Count = 12, TimeoutMs = 1000, DelayMs = 100 }
            };
        }
    }
}
