using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Configuration;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Managers;
using CommSdk.Core.Models;
using CommSdk.Protocols.Modbus.Factories;
using CommSdk.Protocols.Modbus.Models;
using CommSdk.Transports.Factories;

namespace CommSdk.Samples
{
    /// <summary>
    /// 串口 Modbus RTU 电表客户端。
    /// 电表协议参数直接封装在这里，使用者不需要注册驱动或了解 DeviceSession。
    /// </summary>
    public sealed class ElectricityMeterClient : IDisposable
    {
        private readonly DeviceProfile _profile;
        private readonly CommClient _client;
        private int _disposed;

        private ElectricityMeterClient(
            DeviceProfile profile,
            CommClient client)
        {
            _profile = profile;
            _client = client;
        }

        /// <summary>
        /// 根据配置创建客户端。当前示例固定使用串口和 Modbus RTU。
        /// </summary>
        public static ElectricityMeterClient Create(DeviceProfile profile)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            if (profile.Transport == null || !string.Equals(profile.Transport.Type, "serial", StringComparison.OrdinalIgnoreCase))
                throw new DeviceException("ElectricityMeterClient requires a serial transport");
            if (profile.Protocol == null || !string.Equals(profile.Protocol.Type, "modbus-rtu", StringComparison.OrdinalIgnoreCase))
                throw new DeviceException("ElectricityMeterClient requires the modbus-rtu protocol");

            ITransport transport = null;
            CommClient client = null;
            try
            {
                // 直接创建内置传输和协议，不需要注册表，也不需要电表驱动类。
                transport = new SerialTransportFactory().Create(profile.Transport);
                var protocol = new ModbusRtuProtocolFactory().Create(profile.Protocol);
                client = new CommClient(transport, protocol, CreateClientOptions(profile.Retry));
                return new ElectricityMeterClient(profile, client);
            }
            catch
            {
                if (client != null) client.Dispose();
                else if (transport != null) transport.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 从 JSON 字符串创建客户端。
        /// </summary>
        public static ElectricityMeterClient FromJson(string json)
        {
            return Create(DeviceProfileJsonLoader.Load(json));
        }

        /// <summary>
        /// 从 JSON 文件创建客户端。
        /// </summary>
        public static ElectricityMeterClient FromJsonFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("配置文件路径不能为空", "path");
            return FromJson(File.ReadAllText(Path.GetFullPath(path)));
        }

        public bool IsOpen
        {
            get
            {
                ThrowIfDisposed();
                return _client.IsOpen;
            }
        }

        /// <summary>
        /// 打开串口连接。
        /// </summary>
        public void Open()
        {
            ThrowIfDisposed();
            _client.Open();
        }

        /// <summary>
        /// 关闭串口连接。
        /// </summary>
        public void Close()
        {
            ThrowIfDisposed();
            _client.Close();
        }

        /// <summary>
        /// 读取累计电量，返回单位为 kWh。
        /// parameters 可临时覆盖站号、寄存器地址、数量、倍率和字节序。
        /// </summary>
        public double ReadEnergy(IDictionary<string, string> parameters = null)
        {
            ThrowIfDisposed();

            // 参数优先级：本次命令参数 > profile 配置 > 默认值。
            var station = ParseByte(
                FirstValue(
                    GetParameter(parameters, "station"),
                    GetParameter(_profile.Protocol.Parameters, "station"),
                    GetParameter(_profile.Custom, "station"),
                    "1"),
                "station");
            if (station == 0 || station > 247)
                throw new DeviceException("Modbus station must be between 1 and 247");

            // 03 读取保持寄存器，04 读取输入寄存器。
            var functionCode = ParseByte(
                FirstValue(
                    GetParameter(parameters, "functionCode"),
                    GetParameter(parameters, "energyFunction"),
                    GetParameter(_profile.Custom, "energyFunction"),
                    "3"),
                "functionCode");
            if (functionCode != 0x03 && functionCode != 0x04)
                throw new DeviceException("Energy function code must be 0x03 or 0x04");

            // Modbus 地址使用零基地址。例如手册写 40001 时，通常配置为 0。
            var address = ParseUShort(
                FirstValue(
                    GetParameter(parameters, "address"),
                    GetParameter(parameters, "energyAddress"),
                    GetParameter(_profile.Custom, "energyAddress"),
                    "0"),
                "energyAddress");

            // UInt32 电量至少占用两个 16 位寄存器。
            var quantity = ParseUShort(
                FirstValue(
                    GetParameter(parameters, "quantity"),
                    GetParameter(parameters, "energyQuantity"),
                    GetParameter(_profile.Custom, "energyQuantity"),
                    "2"),
                "energyQuantity");
            if (quantity < 2 || quantity > 125)
                throw new DeviceException("Energy quantity must be between 2 and 125 registers");

            // 原始寄存器值乘倍率后得到实际 kWh。
            var scale = ParseScale(
                FirstValue(
                    GetParameter(parameters, "scale"),
                    GetParameter(parameters, "energyScale"),
                    GetParameter(_profile.Custom, "energyScale"),
                    "0.01"),
                "energyScale");
            var byteOrder = ParseByteOrder(
                FirstValue(
                    GetParameter(parameters, "byteOrder"),
                    GetParameter(parameters, "energyByteOrder"),
                    GetParameter(_profile.Custom, "energyByteOrder"),
                    "UInt32BE"));

            // CommClient 负责串口收发、RTU 帧组装、CRC 校验、超时和重试。
            var response = _client.Send(new ModbusRequest
            {
                TransactionId = 0,
                SlaveId = station,
                FunctionCode = functionCode,
                Address = address,
                Quantity = quantity
            }) as ModbusResponse;

            if (response == null)
                throw new DeviceException("Modbus response type is invalid");
            if (response.IsException)
                throw new DeviceException(
                    "Electricity meter returned Modbus exception 0x" +
                    response.ExceptionCode.Value.ToString("X2", CultureInfo.InvariantCulture));
            if (response.Data == null || response.Data.Length < 4)
                throw new DeviceException("Energy response must contain at least two registers");

            // ModbusResponse.Data 已去掉 byte-count 字段，直接解析前两个寄存器。
            return DecodeUInt32(response.Data, byteOrder) * scale;
        }

        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _client.Dispose();
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

        private static string FirstValue(params string[] values)
        {
            if (values == null) return null;
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            return null;
        }

        private static string GetParameter(IDictionary<string, string> parameters, string key)
        {
            if (parameters == null || string.IsNullOrEmpty(key)) return null;

            string value;
            if (parameters.TryGetValue(key, out value)) return value;
            foreach (var pair in parameters)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)) return pair.Value;
            }
            return null;
        }

        private static byte ParseByte(string text, string key)
        {
            uint value;
            if (!TryParseUInt(text, out value) || value > byte.MaxValue)
                throw new DeviceException("Invalid " + key + ": " + text);
            return (byte)value;
        }

        private static ushort ParseUShort(string text, string key)
        {
            uint value;
            if (!TryParseUInt(text, out value) || value > ushort.MaxValue)
                throw new DeviceException("Invalid " + key + ": " + text);
            return (ushort)value;
        }

        private static bool TryParseUInt(string text, out uint value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            text = text.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return uint.TryParse(text.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
            return uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static double ParseScale(string text, string key)
        {
            double value;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
                throw new DeviceException("Invalid " + key + ": " + text);
            return value;
        }

        private static EnergyByteOrder ParseByteOrder(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return EnergyByteOrder.UInt32BE;

            switch (text.Trim().ToUpperInvariant())
            {
                case "BE":
                case "UINT32":
                case "UINT32BE":
                    return EnergyByteOrder.UInt32BE;
                case "LE":
                case "UINT32LE":
                    return EnergyByteOrder.UInt32LE;
                case "UINT32WORDSWAPBE":
                case "WORDSWAPBE":
                    return EnergyByteOrder.UInt32WordSwapBE;
                case "UINT32WORDSWAPLE":
                case "WORDSWAPLE":
                    return EnergyByteOrder.UInt32WordSwapLE;
                default:
                    throw new DeviceException("Unsupported energy byte order: " + text);
            }
        }

        private static uint DecodeUInt32(byte[] data, EnergyByteOrder byteOrder)
        {
            switch (byteOrder)
            {
                case EnergyByteOrder.UInt32BE:
                    return ((uint)data[0] << 24) |
                           ((uint)data[1] << 16) |
                           ((uint)data[2] << 8) |
                           data[3];
                case EnergyByteOrder.UInt32LE:
                    return ((uint)data[3] << 24) |
                           ((uint)data[2] << 16) |
                           ((uint)data[1] << 8) |
                           data[0];
                case EnergyByteOrder.UInt32WordSwapBE:
                    return ((uint)data[2] << 24) |
                           ((uint)data[3] << 16) |
                           ((uint)data[0] << 8) |
                           data[1];
                case EnergyByteOrder.UInt32WordSwapLE:
                    return ((uint)data[1] << 24) |
                           ((uint)data[0] << 16) |
                           ((uint)data[3] << 8) |
                           data[2];
                default:
                    throw new DeviceException("Unsupported energy byte order");
            }
        }

        private enum EnergyByteOrder
        {
            UInt32BE,
            UInt32LE,
            UInt32WordSwapBE,
            UInt32WordSwapLE
        }

        private void ThrowIfDisposed()
        {
            if (System.Threading.Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException("ElectricityMeterClient");
        }
    }
}
