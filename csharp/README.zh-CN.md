# ClawCode C# 方案说明

这个目录不是去重写整套 TypeScript 或 Rust 运行时，而是用 C# 复刻当前仓库里那套 Python 移植工作台的方案边界。也就是：

- 读取同一份 `src/` Python 工作区
- 读取同一份 `src/reference_data/*.json` 快照
- 提供同类 CLI 能力：`summary`、`manifest`、`commands`、`tools`、`route`、`bootstrap`、`parity-audit`
- 保留轻量会话存档、路由模拟、工具/命令镜像执行、模式占位输出

## 目录结构

- `ClawCode/Program.cs`
  统一 CLI 入口，负责子命令分发。
- `ClawCode/Models/`
  放共享数据模型、快照模型。
- `ClawCode/Context/`
  负责定位仓库根目录，并读取 `src/ tests/ assets/ archive/`。
- `ClawCode/Commands/` 与 `ClawCode/Tools/`
  负责命令/工具快照加载、筛选和镜像执行。
- `ClawCode/QueryEngine/`
  负责轻量会话、转录、结构化输出。
- `ClawCode/Runtime/`
  负责 prompt 路由、bootstrap 会话和 turn-loop。
- `ClawCode/Parity/`
  负责读取 `archive_surface_snapshot.json` 做同口径 parity audit。
- `ClawCode.Tests/`
  放零依赖自动化测试。

## 数据来源

`ClawCode.csproj` 没有复制一套新的 JSON 快照，而是直接链接仓库根目录下的：

- `src/reference_data/commands_snapshot.json`
- `src/reference_data/tools_snapshot.json`
- `src/reference_data/archive_surface_snapshot.json`
- `src/reference_data/subsystems/*.json`

这样 Python 工作台和 C# 工作台始终共用同一份源数据，不会产生双份真相。

## 构建与运行

项目级命令：

```powershell
dotnet build csharp\ClawCode\ClawCode.csproj
dotnet run --project csharp\ClawCode -- summary
dotnet run --project csharp\ClawCode -- commands --limit 8 --query review
dotnet run --project csharp\ClawCode -- tools --limit 8 --query MCP
dotnet run --project csharp\ClawCode -- bootstrap "review MCP tool" --limit 5
```

方案级命令：

```powershell
dotnet build csharp\ClawCode.sln /m:1 /p:BuildInParallel=false
```

说明：

- `ClawCode` 和 `ClawCode.Tests` 都关闭了 Roslyn shared compilation。
- 当前受限环境下，方案级构建还需要串行参数，避免 solution 驱动层的并发问题。

## 操作文档

- `docs/01-cli-commands.md`
  逐个说明当前 CLI 子命令、参数和返回码。
- `docs/02-common-usage.md`
  收敛常见运行路径和可直接复制的命令示例。
- `docs/03-test-extension.md`
  说明当前零依赖测试结构，以及如何继续补测试。
- `docs/04-build-troubleshooting.md`
  记录当前已验证过的构建失败现象与排查路径。

## 自动化测试

测试项目放在 `csharp/ClawCode.Tests/`，不依赖 `xUnit/MSTest/NUnit`，而是使用仓库内置的零依赖测试运行器，适合当前离线和受限环境。

```powershell
dotnet build csharp\ClawCode.Tests\ClawCode.Tests.csproj
dotnet run --project csharp\ClawCode.Tests
```

当前测试覆盖：

- Python 工作台 manifest 与 summary
- 命令/工具快照规模与查询
- 工具权限过滤
- prompt 路由与 bootstrap session
- session 持久化与回读
- structured output turn-loop
- parity audit
- 远程模式占位输出
- C# CLI smoke test

## 设计边界

- 这是工作台级 C# 镜像，不是假装已经完成完整业务移植。
- 输出重点放在结构观察、快照索引、报告生成和轻量运行时模拟。
- 代码里的注释和说明统一使用中文，方便继续扩展。
