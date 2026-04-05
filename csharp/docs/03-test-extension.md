# 测试扩展说明

当前测试项目是零依赖结构，不依赖 `xUnit`、`MSTest`、`NUnit`。扩展测试时，应该继续沿用这套轻量模式。

## 当前测试结构

- `csharp/ClawCode.Tests/Program.cs`
  测试入口，维护测试列表并执行。
- `csharp/ClawCode.Tests/Assertions.cs`
  极简断言工具。
- `csharp/ClawCode.Tests/TestEnvironment.cs`
  统一管理临时目录、session 文件、CLI 子进程运行和清理。

## 当前测试覆盖范围

已覆盖：

- manifest 与 summary
- 命令/工具快照规模与查询
- 工具权限过滤
- prompt 路由
- bootstrap session 持久化
- session 持久化与回读
- turn-loop 结构化输出
- parity audit
- 远程模式占位输出
- CLI smoke
- graph / tool pool 输出

## 扩展测试的基本原则

- 优先补当前工作台真实存在的能力，不为未来功能先写空壳测试。
- 优先写稳定断言，不依赖瞬时顺序、随机值和环境噪声。
- 能走公开入口时，优先走公开入口，不直接测试私有细节。
- 若测试会创建文件，必须接入 `TestEnvironment` 统一清理。

## 如何新增一个测试

### 第一步：在 `Program.cs` 的测试列表注册用例

当前测试入口使用 `TestCase(string Name, Action<TestEnvironment> Execute)`。

新增测试时，需要先把方法注册进数组：

```csharp
new TestCase("My_New_Test", MyNewTest),
```

### 第二步：实现测试方法

方法签名保持一致：

```csharp
private static void MyNewTest(TestEnvironment environment)
{
    var result = SomeEntryPoint.Run();
    Assertions.Contains("expected", result, "输出不符合预期。");
}
```

### 第三步：如有临时产物，交给 `TestEnvironment`

如果测试生成了文件，需要显式登记：

```csharp
environment.TrackFile(path);
```

如果测试需要临时目录，使用：

```csharp
var tempDir = environment.CreateTempDirectory(".some-temp-dir");
```

不要自己在测试里随意留目录或文件。

## 推荐的新增测试类型

### 1. 文本输出稳定性测试

适合对象：

- `summary`
- `manifest`
- `bootstrap`
- `tool-pool`

写法建议：

- 断言关键章节存在
- 断言关键字段存在
- 不要把整份长文本逐字硬编码，除非你要做黄金快照测试

### 2. 参数边界测试

适合对象：

- `commands --limit`
- `tools --deny-prefix`
- `turn-loop --max-turns`
- `route` 缺失 prompt

写法建议：

- 断言返回结果数量或关键错误信息
- 对缺参场景优先走 CLI 或公开命令入口

### 3. session / 文件落盘测试

适合对象：

- `bootstrap`
- `flush-transcript`
- `load-session`

写法建议：

- 既断言文件存在，也断言回读内容合理
- 所有落盘文件都交给 `TestEnvironment` 清理

### 4. CLI 级 smoke 测试

如果你想验证命令行入口，复用：

```csharp
var output = environment.RunCli("summary");
```

这会拉起真实 `dotnet` 子进程，更接近实际使用路径。

## 什么时候不要扩展测试

- 代码还只是占位，且行为边界未定。
- 断言只能依赖不稳定快照排序。
- 测试只能验证实现细节，无法验证公开行为。

这种情况先收敛实现边界，再补测试。

## 运行方式

构建测试项目：

```powershell
dotnet build csharp\ClawCode.Tests\ClawCode.Tests.csproj
```

运行测试：

```powershell
dotnet run --project csharp\ClawCode.Tests
```

## 扩展后的自检清单

- 新测试名是否清晰反映行为。
- 是否复用 `Assertions` 和 `TestEnvironment`。
- 是否避免把环境临时产物留在仓库里。
- 是否断言公开行为，而不是内部偶然实现。
- 是否与现有测试职责重复；若重复，应合并而不是堆新用例。
