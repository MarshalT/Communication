# ADR 0001: 将通信框架合并为单一程序集

## 状态

已采用

## 背景

原解决方案将通信能力拆成 `CommSdk.Core`、`CommSdk.Transports`、`CommSdk.Protocols.Modbus`、`CommSdk.Devices` 和 `CommSdk.Framework` 五个类库。代码职责边界清晰，但使用方需要同时引用多个项目，Windows 下打开解决方案时也会显示较多项目节点。

## 决策

将框架合并为 `src/CommSdk/CommSdk.csproj`，目标框架仍为 .NET Framework 4.8，输出程序集名称为 `CommSdk.dll`。源代码继续按职责放在 `Core`、`Devices`、`Framework`、`Protocols/Modbus` 和 `Transports` 目录中，并保留对应命名空间：

- `CommSdk.Core`
- `CommSdk.Devices`
- `CommSdk.Framework`
- `CommSdk.Protocols.Modbus`
- `CommSdk.Transports`

`CommSdk.Samples` 保持独立可执行项目，只引用 `CommSdk`；测试项目引用 `CommSdk` 和示例项目，用于覆盖框架和完整示例流程。

## 备选方案

1. 继续保留多个类库：职责隔离最强，但使用方需要管理多个引用，不符合当前简化使用的目标。
2. 合并项目并把所有类型放入单一 `CommSdk` 命名空间：引用最简单，但会丢失已有命名空间的职责边界，并增加类型名冲突风险。
3. 合并为单一程序集、保留职责命名空间：引用数量减少，同时保留代码组织和 API 兼容性，因此采用该方案。

## 影响

- 使用方只需要引用 `CommSdk.dll`，仍可按命名空间选择所需能力。
- 组件之间不再通过项目引用隔离，程序集内部的依赖约束需要依靠目录、命名空间和代码审查维护。
- 示例仍单独编译和运行，不会把电表业务代码带入通用通信库。
- 现有公共类型命名空间保持不变，迁移成本较低。
