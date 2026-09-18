# Communication SDK Completion Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

> **Current layout note:** This historical implementation plan was written for the original multi-project layout. The completed framework is now consolidated into `src/CommSdk/CommSdk.csproj`; the responsibility folders and namespaces described below are retained inside that single assembly.

**Goal:** Turn the existing .NET Framework 4.8 communication SDK skeleton into a testable first release with framed request/response handling, lifecycle management, JSON profiles, and usable examples.

**Architecture:** Keep the existing Core/Transports/Protocols/Devices layering as separated folders and namespaces inside one assembly. Add small optional protocol capabilities for frame boundary detection and response validation so `CommClient` can serialize requests without coupling Core to Modbus. Make `DeviceSession` own the transport/client lifecycle, while transport implementations expose cancellable chunk I/O and protocol implementations assemble complete frames.

**Tech Stack:** C# SDK-style projects targeting `net48`, `DataContractJsonSerializer` for dependency-free profile loading, xUnit for tests, TCP loopback integration tests, and Visual Studio/.NET CLI solution metadata.

---

### Task 1: Extend core contracts and profile models

**Files:**
- Create: `src/CommSdk.Core/Abstractions/IProtocolFrameParser.cs`
- Create: `src/CommSdk.Core/Abstractions/IProtocolResponseValidator.cs`
- Create: `src/CommSdk.Core/Models/RetryConfig.cs`
- Create: `src/CommSdk.Core/Models/LoggingConfig.cs`
- Create: `src/CommSdk.Core/Models/CommClientOptions.cs`
- Modify: `src/CommSdk.Core/Abstractions/ITransport.cs`
- Modify: `src/CommSdk.Core/Models/DeviceProfile.cs`

**Steps:**
1. Add `BufferSize` to `ITransport` and optional protocol capability interfaces.
2. Add retry/logging profile settings and client options with sane defaults.
3. Keep all new APIs net48-compatible and validate option values at construction/use sites.

### Task 2: Implement framed, cancellable transports

**Files:**
- Modify: `src/CommSdk.Transports/Options/*.cs`
- Modify: `src/CommSdk.Transports/SerialTransport.cs`
- Modify: `src/CommSdk.Transports/TcpTransport.cs`
- Modify: `src/CommSdk.Transports/UdpTransport.cs`
- Modify: `src/CommSdk.Transports/Factories/*.cs`

**Steps:**
1. Add buffer-size settings and make `TimeoutMs` affect active transport handles.
2. Return bounded chunks from serial/TCP reads instead of assuming `DataAvailable` is a complete frame.
3. Clean up failed/repeated opens and dispose paths; resolve UDP hostnames as well as IP literals.
4. Use native async socket/stream APIs where available and honor cancellation before/while waiting.

### Task 3: Complete protocol framing and validation

**Files:**
- Create: `src/CommSdk.Protocols.Modbus/Utils/ModbusResponseValidator.cs`
- Modify: `src/CommSdk.Protocols.Modbus/ModbusRtuProtocol.cs`
- Modify: `src/CommSdk.Protocols.Modbus/ModbusTcpProtocol.cs`
- Modify: `src/CommSdk.Protocols.Modbus/ModbusAsciiProtocol.cs`
- Modify: `src/CommSdk.Protocols.Modbus/Utils/ModbusCodec.cs`

**Steps:**
1. Implement frame-length detection for RTU, TCP MBAP, and ASCII CRLF frames.
2. Validate CRC/LRC, MBAP protocol/length fields, Modbus function/data lengths, and response identity.
3. Reject malformed or truncated frames deterministically with `ProtocolException`.

### Task 4: Make `CommClient` and `DeviceManager` production-shaped

**Files:**
- Modify: `src/CommSdk.Core/Managers/CommClient.cs`
- Modify: `src/CommSdk.Core/Managers/DeviceManager.cs`
- Modify: `src/CommSdk.Core/Models/DeviceContext.cs`

**Steps:**
1. Serialize sync/async request-response operations with one gate and preserve extra received bytes.
2. Add auto-open, bounded retry, reconnect, backoff, timeout propagation, and cancellation checks.
3. Make sessions disposable/openable/closeable and let `DeviceManager` track IDs and remove sessions safely.
4. Inject the client into driver context so drivers can perform real protocol calls.

### Task 5: Add typed JSON profile loading and delivery templates

**Files:**
- Create: `src/CommSdk.Core/Configuration/DeviceProfileJsonLoader.cs`
- Create: `docs/examples/device-profile.json`
- Create: `docs/templates/DeviceDriverTemplate.cs`
- Create: `README.md`

**Steps:**
1. Deserialize the design-document JSON shape without external runtime dependencies.
2. Map flat transport/protocol fields into existing parameter dictionaries and typed retry/logging settings.
3. Document registration, lifecycle, profile loading, and a TCP loopback usage path.

### Task 6: Add automated tests and solution metadata

**Files:**
- Create: `CommSdk.sln`
- Create: `tests/CommSdk.Tests/CommSdk.Tests.csproj`
- Create: `tests/CommSdk.Tests/ModbusProtocolTests.cs`
- Create: `tests/CommSdk.Tests/ConfigurationTests.cs`
- Create: `tests/CommSdk.Tests/CommClientTests.cs`
- Create: `tests/CommSdk.Tests/FakeTransport.cs`

**Steps:**
1. Add unit tests for RTU/TCP/ASCII encoding, CRC/LRC and malformed-frame rejection.
2. Add profile-loader and transport-chunk assembly tests, including retry and response matching.
3. Add solution references and document `dotnet test CommSdk.sln`/Visual Studio verification.

### Verification

Run on a machine with a .NET Framework-compatible SDK/build toolchain:

```text
dotnet test CommSdk.sln
dotnet build CommSdk.sln --configuration Release
```

Expected result: all unit/integration tests pass and all SDK/sample projects compile for `net48`. In the current macOS environment the repository will still be statically inspected because no `dotnet`, MSBuild, Mono, or xUnit runner is installed.
