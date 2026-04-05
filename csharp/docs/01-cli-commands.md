# CLI 子命令说明

这份文档只说明当前 `ClawCode` 已实现的 CLI 子命令，不扩展到尚未存在的能力。

## 基本调用方式

```powershell
dotnet run --project csharp\ClawCode -- <command> [options] [arguments]
```

查看帮助：

```powershell
dotnet run --project csharp\ClawCode -- help
```

## 总览类命令

### `summary`

输出当前 C# 移植工作台摘要，包括 Python 工作区规模、命令面、工具面等概览。

```powershell
dotnet run --project csharp\ClawCode -- summary
```

### `manifest`

输出工作区 manifest，适合查看顶层模块、文件规模和当前移植边界。

```powershell
dotnet run --project csharp\ClawCode -- manifest
```

### `parity-audit`

读取 `archive_surface_snapshot.json`，输出当前 C# 侧和参考快照之间的同口径对照结果。

```powershell
dotnet run --project csharp\ClawCode -- parity-audit
```

### `setup-report`

输出当前工作台 setup 报告，用于快速确认上下文和初始化状态。

```powershell
dotnet run --project csharp\ClawCode -- setup-report
```

## 结构观察类命令

### `command-graph`

输出命令侧结构图的 Markdown 文本。

```powershell
dotnet run --project csharp\ClawCode -- command-graph
```

### `tool-pool`

输出当前工具池，可配合过滤参数观察可用工具集合。

参数：

- `--simple-mode`：切换到简化工具视图。
- `--no-mcp`：排除 MCP 工具。
- `--deny-tool NAME`：按工具名排除。
- `--deny-prefix PREFIX`：按前缀排除。

```powershell
dotnet run --project csharp\ClawCode -- tool-pool --simple-mode --no-mcp
dotnet run --project csharp\ClawCode -- tool-pool --deny-prefix mcp
```

### `bootstrap-graph`

输出 bootstrap 过程结构图的 Markdown 文本。

```powershell
dotnet run --project csharp\ClawCode -- bootstrap-graph
```

### `subsystems`

列出顶层子系统摘要。

参数：

- `--limit N`：限制输出条目数，默认 `32`。

```powershell
dotnet run --project csharp\ClawCode -- subsystems --limit 10
```

## 快照检索类命令

### `commands`

列出命令快照，或按关键字检索。

参数：

- `--limit N`：限制输出条目数，默认 `20`。
- `--query TEXT`：按关键字过滤。
- `--no-plugin-commands`：排除插件命令。
- `--no-skill-commands`：排除 skill 命令。

```powershell
dotnet run --project csharp\ClawCode -- commands --limit 8
dotnet run --project csharp\ClawCode -- commands --limit 8 --query review
```

### `tools`

列出工具快照，或按关键字检索。

参数：

- `--limit N`：限制输出条目数，默认 `20`。
- `--query TEXT`：按关键字过滤。
- `--simple-mode`：简化输出视图。
- `--no-mcp`：排除 MCP 工具。
- `--deny-tool NAME`：按工具名排除。
- `--deny-prefix PREFIX`：按前缀排除。

```powershell
dotnet run --project csharp\ClawCode -- tools --limit 8
dotnet run --project csharp\ClawCode -- tools --limit 8 --query MCP
dotnet run --project csharp\ClawCode -- tools --deny-prefix mcp
```

### `show-command`

按名称查看单个命令详情。

```powershell
dotnet run --project csharp\ClawCode -- show-command review
```

### `show-tool`

按名称查看单个工具详情。

```powershell
dotnet run --project csharp\ClawCode -- show-tool MCPTool
```

## 运行时模拟类命令

### `route <prompt>`

对输入 prompt 做匹配，返回最可能命中的命令和工具。

参数：

- `--limit N`：限制命中条目数，默认 `5`。

```powershell
dotnet run --project csharp\ClawCode -- route "review MCP tool" --limit 5
```

### `bootstrap <prompt>`

基于 prompt 生成运行时会话启动结果，并持久化 session 文件。

参数：

- `--limit N`：限制候选条目数，默认 `5`。

```powershell
dotnet run --project csharp\ClawCode -- bootstrap "review MCP tool" --limit 5
```

### `turn-loop <prompt>`

模拟多轮运行时回合，可选择结构化输出。

参数：

- `--limit N`：限制候选条目数，默认 `5`。
- `--max-turns N`：最大轮数，默认 `3`。
- `--structured-output`：改为结构化输出。

```powershell
dotnet run --project csharp\ClawCode -- turn-loop "review MCP tool" --max-turns 2
dotnet run --project csharp\ClawCode -- turn-loop "review MCP tool" --max-turns 2 --structured-output
```

### `flush-transcript <prompt>`

提交一条消息并立即落盘当前会话转录。

```powershell
dotnet run --project csharp\ClawCode -- flush-transcript "review MCP tool"
```

### `load-session <session_id>`

回读先前落盘的 session。

```powershell
dotnet run --project csharp\ClawCode -- load-session session-001
```

## 远程模式占位类命令

这些命令当前用于输出远程模式占位信息，不是完整远程执行实现。

### `remote-mode <target>`
### `ssh-mode <target>`
### `teleport-mode <target>`
### `direct-connect-mode <target>`
### `deep-link-mode <target>`

```powershell
dotnet run --project csharp\ClawCode -- remote-mode workspace
dotnet run --project csharp\ClawCode -- ssh-mode workspace
dotnet run --project csharp\ClawCode -- teleport-mode workspace
dotnet run --project csharp\ClawCode -- direct-connect-mode workspace
dotnet run --project csharp\ClawCode -- deep-link-mode workspace
```

## 镜像执行类命令

### `exec-command <name> <prompt>`

执行镜像命令入口。

```powershell
dotnet run --project csharp\ClawCode -- exec-command review "review MCP tool"
```

### `exec-tool <name> <payload>`

执行镜像工具入口。

```powershell
dotnet run --project csharp\ClawCode -- exec-tool MCPTool "{\"query\":\"review\"}"
```

## 返回码约定

- `0`：执行成功。
- `1`：参数缺失，或命令本身返回未处理状态。
- `2`：未知命令。

## 边界说明

- 这套 CLI 是当前 Python 工作台的 C# 镜像工作台，不是完整生产运行时。
- 命令和工具的主体数据来自 `src/reference_data/*.json`。
- 远程模式相关命令目前只输出模式说明和占位信息。
