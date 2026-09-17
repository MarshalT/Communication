using System;
using System.Collections.Generic;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Configuration;
using CommSdk.Core.Logging;
using CommSdk.Core.Models;
using CommSdk.Core.Registries;

namespace CommSdk.Core.Managers
{
    public class DeviceManager : IDisposable
    {
        private readonly TransportRegistry _transportRegistry;
        private readonly ProtocolRegistry _protocolRegistry;
        private readonly DriverRegistry _driverRegistry;
        private readonly IDictionary<string, DeviceSession> _sessions =
            new Dictionary<string, DeviceSession>(StringComparer.OrdinalIgnoreCase);
        private readonly object _sync = new object();
        private int _disposed;

        public DeviceManager(
            TransportRegistry transportRegistry,
            ProtocolRegistry protocolRegistry,
            DriverRegistry driverRegistry)
        {
            _transportRegistry = transportRegistry ?? throw new ArgumentNullException("transportRegistry");
            _protocolRegistry = protocolRegistry ?? throw new ArgumentNullException("protocolRegistry");
            _driverRegistry = driverRegistry ?? throw new ArgumentNullException("driverRegistry");
        }

        public DeviceSession Create(DeviceProfile profile)
        {
            ThrowIfDisposed();
            ValidateProfile(profile);
            LogManager.Configure(profile.Logging);

            var transportFactory = _transportRegistry.Resolve(profile.Transport.Type);
            var protocolFactory = _protocolRegistry.Resolve(profile.Protocol.Type);
            var driverFactory = _driverRegistry.Resolve(profile.Model);

            ITransport transport = null;
            CommClient client = null;
            try
            {
                transport = transportFactory.Create(profile.Transport);
                var protocol = protocolFactory.Create(profile.Protocol);
                client = new CommClient(transport, protocol, CreateClientOptions(profile.Retry));
                var driver = driverFactory.Create(profile);
                var context = new DeviceContext
                {
                    Profile = profile,
                    Transport = transport,
                    Protocol = protocol,
                    Client = client
                };
                driver.Initialize(context);

                var session = new DeviceSession(profile, transport, protocol, driver, client);
                if (!string.IsNullOrEmpty(profile.Id))
                {
                    lock (_sync)
                    {
                        DeviceSession existing;
                        if (_sessions.TryGetValue(profile.Id, out existing))
                            throw new InvalidOperationException("Device id already exists: " + profile.Id);
                        _sessions.Add(profile.Id, session);
                    }
                }
                return session;
            }
            catch
            {
                if (client != null) client.Dispose();
                else if (transport != null) transport.Dispose();
                throw;
            }
        }

        public DeviceSession CreateFromJson(string json)
        {
            return Create(DeviceProfileJsonLoader.Load(json));
        }

        public DeviceSession Get(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Device id is required", "id");
            lock (_sync)
            {
                DeviceSession session;
                return _sessions.TryGetValue(id, out session) ? session : null;
            }
        }

        public bool Remove(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            DeviceSession session = null;
            lock (_sync)
            {
                if (!_sessions.TryGetValue(id, out session)) return false;
                _sessions.Remove(id);
            }
            session.Dispose();
            return true;
        }

        public IList<DeviceSession> Sessions
        {
            get
            {
                lock (_sync) return new List<DeviceSession>(_sessions.Values);
            }
        }

        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0) return;
            IList<DeviceSession> sessions;
            lock (_sync)
            {
                sessions = new List<DeviceSession>(_sessions.Values);
                _sessions.Clear();
            }

            foreach (var session in sessions) session.Dispose();
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

        private static void ValidateProfile(DeviceProfile profile)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            if (string.IsNullOrEmpty(profile.Model)) throw new ArgumentException("Device model is required", "profile");
            if (profile.Transport == null) throw new ArgumentException("Transport configuration is required", "profile");
            if (profile.Protocol == null) throw new ArgumentException("Protocol configuration is required", "profile");
            if (string.IsNullOrEmpty(profile.Transport.Type)) throw new ArgumentException("Transport type is required", "profile");
            if (string.IsNullOrEmpty(profile.Protocol.Type)) throw new ArgumentException("Protocol type is required", "profile");
        }

        private void ThrowIfDisposed()
        {
            if (System.Threading.Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException("DeviceManager");
        }
    }

    public sealed class DeviceSession : IDisposable
    {
        private int _disposed;

        public DeviceProfile Profile { get; private set; }
        public ITransport Transport { get; private set; }
        public IProtocol Protocol { get; private set; }
        public IDeviceDriver Driver { get; private set; }
        public CommClient Client { get; private set; }

        public DeviceSession(DeviceProfile profile, ITransport transport, IProtocol protocol, IDeviceDriver driver)
            : this(profile, transport, protocol, driver, new CommClient(transport, protocol))
        {
        }

        public DeviceSession(
            DeviceProfile profile,
            ITransport transport,
            IProtocol protocol,
            IDeviceDriver driver,
            CommClient client)
        {
            Profile = profile ?? throw new ArgumentNullException("profile");
            Transport = transport ?? throw new ArgumentNullException("transport");
            Protocol = protocol ?? throw new ArgumentNullException("protocol");
            Driver = driver ?? throw new ArgumentNullException("driver");
            Client = client ?? throw new ArgumentNullException("client");
        }

        public void Open()
        {
            ThrowIfDisposed();
            Client.Open();
        }

        public System.Threading.Tasks.Task OpenAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            ThrowIfDisposed();
            return Client.OpenAsync(cancellationToken);
        }

        public void Close()
        {
            ThrowIfDisposed();
            Client.Close();
        }

        public object Read(DeviceCommand command)
        {
            ThrowIfDisposed();
            return Driver.Read(command);
        }

        public void Write(DeviceCommand command, object value)
        {
            ThrowIfDisposed();
            Driver.Write(command, value);
        }

        public object Execute(DeviceCommand command)
        {
            ThrowIfDisposed();
            return Driver.Execute(command);
        }

        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0) return;
            Client.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (System.Threading.Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException("DeviceSession");
        }
    }
}
