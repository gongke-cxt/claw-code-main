# 常见使用示例

这份文档只给出当前 C# 工作台最常见的使用路径，重点是能直接复制运行。

## 1. 先确认项目能跑

```powershell
dotnet build csharp\ClawCode\ClawCode.csproj
dotnet run --project csharp\ClawCode -- summary
```

适用场景：

- 第一次拉起 C# 工作台
- 确认当前环境能读取快照和工作区

## 2. 查看当前工作台边界

```powershell
dotnet run --project csharp\ClawCode -- manifest
dotnet run --project csharp\ClawCode -- parity-audit
dotnet run --project csharp\ClawCode -- setup-report
```

建议顺序：

1. `manifest` 看结构。
2. `parity-audit` 看和参考快照的覆盖关系。
3. `setup-report` 看初始化上下文。

## 3. 查命令快照

列出前几个命令：

```powershell
dotnet run --project csharp\ClawCode -- commands --limit 8
```

按关键字查命令：

```powershell
dotnet run --project csharp\ClawCode -- commands --limit 8 --query review
```

排除插件命令：

```powershell
dotnet run --project csharp\ClawCode -- commands --limit 8 --no-plugin-commands
```

适用场景：

- 想知道当前已经镜像了哪些命令
- 想快速定位某类命令是否在快照里

## 4. 查工具快照

列出前几个工具：

```powershell
dotnet run --project csharp\ClawCode -- tools --limit 8
```

按关键字查工具：

```powershell
dotnet run --project csharp\ClawCode -- tools --limit 8 --query MCP
```

排除 MCP 工具：

```powershell
dotnet run --project csharp\ClawCode -- tools --limit 8 --no-mcp
```

按前缀过滤：

```powershell
dotnet run --project csharp\ClawCode -- tools --deny-prefix mcp
```

适用场景：

- 想知道当前工具池规模
- 想模拟权限过滤后的工具集合

## 5. 用 prompt 做路由匹配

```powershell
dotnet run --project csharp\ClawCode -- route "review MCP tool" --limit 5
```

输出会给出命中的 `command/tool`、名称、分数、来源提示。

适用场景：

- 想知道某个 prompt 会被路由到哪些候选项
- 想快速检查路由规则是否合理

## 6. 生成 bootstrap 会话

```powershell
dotnet run --project csharp\ClawCode -- bootstrap "review MCP tool" --limit 5
```

这个命令会：

- 生成运行时会话摘要
- 给出 startup steps
- 持久化一个 session 文件

适用场景：

- 想验证 prompt 到启动会话的完整路径
- 想拿到落盘 session 做后续回读

## 7. 跑一轮或多轮 turn-loop

普通文本输出：

```powershell
dotnet run --project csharp\ClawCode -- turn-loop "review MCP tool" --max-turns 2
```

结构化输出：

```powershell
dotnet run --project csharp\ClawCode -- turn-loop "review MCP tool" --max-turns 2 --structured-output
```

适用场景：

- 想看回合式输出是否能跑通
- 想拿结构化结果做后续自动处理

## 8. 落盘并回读 session

先落盘：

```powershell
dotnet run --project csharp\ClawCode -- flush-transcript "review MCP tool"
```

再回读：

```powershell
dotnet run --project csharp\ClawCode -- load-session <session_id>
```

适用场景：

- 想验证轻量 session 持久化
- 想检查会话统计和消息数量

## 9. 查看结构图和工具池报告

```powershell
dotnet run --project csharp\ClawCode -- command-graph
dotnet run --project csharp\ClawCode -- bootstrap-graph
dotnet run --project csharp\ClawCode -- tool-pool --simple-mode
```

适用场景：

- 想看当前移植工作台的结构关系
- 想确认不同过滤条件下的工具池

## 10. 运行远程模式占位命令

```powershell
dotnet run --project csharp\ClawCode -- remote-mode workspace
dotnet run --project csharp\ClawCode -- ssh-mode workspace
dotnet run --project csharp\ClawCode -- teleport-mode workspace
dotnet run --project csharp\ClawCode -- direct-connect-mode workspace
dotnet run --project csharp\ClawCode -- deep-link-mode workspace
```

适用场景：

- 验证模式占位输出是否存在
- 对接未来远程模式时先看当前边界

## 11. 运行自动化测试

```powershell
dotnet build csharp\ClawCode.Tests\ClawCode.Tests.csproj
dotnet run --project csharp\ClawCode.Tests
```

适用场景：

- 改完代码后回归
- 提交前先检查核心工作台能力

## 12. 做方案级整体构建

```powershell
dotnet build csharp\ClawCode.sln /m:1 /p:BuildInParallel=false
```

说明：

- 当前环境下，方案级构建建议固定使用串行参数。
- 这是已验证稳定的做法，不要先默认并发构建。
