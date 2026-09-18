using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using CommSdk.Core.Models;

namespace CommSdk.Core.Configuration
{
    public static class DeviceProfileJsonLoader
    {
        public static DeviceProfile Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON profile is required", "json");
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return Load(stream);
        }

        public static DeviceProfile Load(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            ProfileDto dto;
            try
            {
                var serializer = new DataContractJsonSerializer(
                    typeof(ProfileDto),
                    new DataContractJsonSerializerSettings
                    {
                        // Device custom parameters are represented as a JSON object:
                        // { "energyAddress": "40000" }.
                        UseSimpleDictionaryFormat = true
                    });
                dto = serializer.ReadObject(stream) as ProfileDto;
            }
            catch (SerializationException ex)
            {
                throw new FormatException("Invalid device profile JSON", ex);
            }

            if (dto == null || dto.Device == null || dto.Transport == null || dto.Protocol == null)
                throw new FormatException("Device, transport and protocol sections are required");
            if (string.IsNullOrWhiteSpace(dto.Device.Model))
                throw new FormatException("device.model is required");
            if (string.IsNullOrWhiteSpace(dto.Transport.Type))
                throw new FormatException("transport.type is required");
            if (string.IsNullOrWhiteSpace(dto.Protocol.Type))
                throw new FormatException("protocol.type is required");

            var profile = new DeviceProfile
            {
                Id = dto.Device.Id,
                Model = dto.Device.Model,
                Driver = dto.Device.Driver,
                Custom = dto.Device.Custom == null
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(dto.Device.Custom, StringComparer.OrdinalIgnoreCase),
                Transport = new TransportConfig
                {
                    Type = dto.Transport.Type,
                    Parameters = BuildTransportParameters(dto.Transport)
                },
                Protocol = new ProtocolConfig
                {
                    Type = dto.Protocol.Type,
                    Parameters = BuildProtocolParameters(dto.Protocol)
                },
                Retry = dto.Retry == null ? null : new RetryConfig
                {
                    Count = Math.Max(0, dto.Retry.Count),
                    TimeoutMs = Math.Max(0, dto.Retry.TimeoutMs),
                    DelayMs = Math.Max(0, dto.Retry.DelayMs),
                    ExponentialBackoff = dto.Retry.ExponentialBackoff
                },
                Logging = dto.Logging == null ? null : new LoggingConfig { Level = dto.Logging.Level }
            };
            return profile;
        }

        private static IDictionary<string, string> BuildTransportParameters(TransportDto dto)
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Put(parameters, "host", dto.Host);
            Put(parameters, "port", dto.Port);
            Put(parameters, "baud", dto.Baud);
            Put(parameters, "databits", dto.DataBits);
            Put(parameters, "parity", dto.Parity);
            Put(parameters, "stopbits", dto.StopBits);
            Put(parameters, "connectTimeoutMs", dto.ConnectTimeoutMs);
            Put(parameters, "readTimeoutMs", dto.ReadTimeoutMs);
            Put(parameters, "writeTimeoutMs", dto.WriteTimeoutMs);
            Put(parameters, "receiveTimeoutMs", dto.ReceiveTimeoutMs);
            Put(parameters, "bufferSize", dto.BufferSize);
            return parameters;
        }

        private static IDictionary<string, string> BuildProtocolParameters(ProtocolDto dto)
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Put(parameters, "station", dto.Station);
            Put(parameters, "endianness", dto.Endianness);
            return parameters;
        }

        private static void Put(IDictionary<string, string> target, string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) target[key] = value;
        }

        private static void Put(IDictionary<string, string> target, string key, int value)
        {
            if (value > 0) target[key] = value.ToString(CultureInfo.InvariantCulture);
        }

        [DataContract]
        public class ProfileDto
        {
            [DataMember(Name = "device")] public DeviceDto Device { get; set; }
            [DataMember(Name = "transport")] public TransportDto Transport { get; set; }
            [DataMember(Name = "protocol")] public ProtocolDto Protocol { get; set; }
            [DataMember(Name = "retry")] public RetryDto Retry { get; set; }
            [DataMember(Name = "logging")] public LoggingDto Logging { get; set; }
        }

        [DataContract]
        public class DeviceDto
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "model")] public string Model { get; set; }
            [DataMember(Name = "driver")] public string Driver { get; set; }
            [DataMember(Name = "custom")] public Dictionary<string, string> Custom { get; set; }
        }

        [DataContract]
        public class TransportDto
        {
            [DataMember(Name = "type")] public string Type { get; set; }
            [DataMember(Name = "host")] public string Host { get; set; }
            [DataMember(Name = "port")] public string Port { get; set; }
            [DataMember(Name = "baud")] public int Baud { get; set; }
            [DataMember(Name = "databits")] public int DataBits { get; set; }
            [DataMember(Name = "parity")] public string Parity { get; set; }
            [DataMember(Name = "stopbits")] public string StopBits { get; set; }
            [DataMember(Name = "connectTimeoutMs")] public int ConnectTimeoutMs { get; set; }
            [DataMember(Name = "readTimeoutMs")] public int ReadTimeoutMs { get; set; }
            [DataMember(Name = "writeTimeoutMs")] public int WriteTimeoutMs { get; set; }
            [DataMember(Name = "receiveTimeoutMs")] public int ReceiveTimeoutMs { get; set; }
            [DataMember(Name = "bufferSize")] public int BufferSize { get; set; }
        }

        [DataContract]
        public class ProtocolDto
        {
            [DataMember(Name = "type")] public string Type { get; set; }
            [DataMember(Name = "station")] public int Station { get; set; }
            [DataMember(Name = "endianness")] public string Endianness { get; set; }
        }

        [DataContract]
        public class RetryDto
        {
            [DataMember(Name = "count")] public int Count { get; set; }
            [DataMember(Name = "timeoutMs")] public int TimeoutMs { get; set; }
            [DataMember(Name = "delayMs")] public int DelayMs { get; set; }
            [DataMember(Name = "exponentialBackoff")] public bool ExponentialBackoff { get; set; }
        }

        [DataContract]
        public class LoggingDto
        {
            [DataMember(Name = "level")] public string Level { get; set; }
        }
    }
}
