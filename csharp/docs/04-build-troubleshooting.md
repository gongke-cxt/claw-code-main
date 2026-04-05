# 构建失败排查

这份文档只记录当前这套 C# 工作台已经真实遇到过、并已验证过的构建问题与处理方式。

## 先区分是哪一层失败

优先按下面顺序排查：

1. 主项目是否能单独构建。
2. 测试项目是否能单独构建。
3. 方案级构建是否失败。

对应命令：

```powershell
dotnet build csharp\ClawCode\ClawCode.csproj
dotnet build csharp\ClawCode.Tests\ClawCode.Tests.csproj
dotnet build csharp\ClawCode.sln /m:1 /p:BuildInParallel=false
```

如果前两个成功、只有第三个失败，优先按“方案级并发问题”处理，不要先怀疑业务代码。

## 已验证稳定的方案级构建命令

```powershell
dotnet build csharp\ClawCode.sln /m:1 /p:BuildInParallel=false
```

这是当前环境下已经验证稳定的命令。

原因：

- 方案级构建在当前受限环境里对并发更敏感。
- 串行参数可以绕开 solution 驱动层并发导致的不稳定行为。

## 已做过的项目侧稳定化处理

`ClawCode.csproj` 和 `ClawCode.Tests.csproj` 都已经关闭 Roslyn shared compilation：

```xml
<UseSharedCompilation>false</UseSharedCompilation>
```

这不是可选说明，而是当前方案稳定构建的一部分。

## 真实遇到过的问题

### 1. 方案级构建无诊断失败或恢复流程异常

表现：

- `dotnet build csharp\ClawCode.sln` 直接失败
- 输出对业务代码帮助不大
- 项目级构建却可能正常

处理：

- 改用串行方案级构建命令：

```powershell
dotnet build csharp\ClawCode.sln /m:1 /p:BuildInParallel=false
```

### 2. `ClawCode.runtimeconfig.json` 被占用或文件锁冲突

表现：

- 构建或运行时提示目标文件正在被占用
- 常见于刚跑完 CLI、测试，马上又做方案级构建

处理：

1. 先确认没有其他 `dotnet` 进程占着构建产物。
2. 重新执行串行构建命令。
3. 如果仍冲突，再清理 `bin/obj` 后重建。

### 3. 编译器服务或 shared compilation 导致的不稳定

表现：

- 方案级构建和项目级构建行为不一致
- 偶发失败，且不是固定业务代码错误

处理：

- 保持两个 `.csproj` 里的 `<UseSharedCompilation>false</UseSharedCompilation>` 不变。
- 不要把这项配置删掉再试。

## 推荐排查顺序

### 场景 A：只想尽快构建成功

直接执行：

```powershell
dotnet build csharp\ClawCode.sln /m:1 /p:BuildInParallel=false
```

### 场景 B：怀疑是代码改坏了

先分开构建：

```powershell
dotnet build csharp\ClawCode\ClawCode.csproj
dotnet build csharp\ClawCode.Tests\ClawCode.Tests.csproj
```

判断：

- 两个项目都过，说明大概率不是代码编译错误，而是方案级驱动问题。
- 某个项目不过，再按具体编译错误修代码。

### 场景 C：改完代码后想做完整验证

建议固定顺序：

```powershell
dotnet build csharp\ClawCode.sln /m:1 /p:BuildInParallel=false
dotnet run --project csharp\ClawCode.Tests
```

这样能同时覆盖：

- 主项目编译
- 测试项目编译
- 关键能力回归

## 如需清理后重试

可清理这些目录后再构建：

- `csharp/ClawCode/bin`
- `csharp/ClawCode/obj`
- `csharp/ClawCode.Tests/bin`
- `csharp/ClawCode.Tests/obj`

然后重新执行：

```powershell
dotnet build csharp\ClawCode.sln /m:1 /p:BuildInParallel=false
```

## 不建议的做法

- 不要先默认跑 `dotnet build csharp\ClawCode.sln` 无串行参数。
- 不要删掉 `<UseSharedCompilation>false</UseSharedCompilation>` 再试。
- 不要把方案级失败直接误判成业务逻辑错误。

## 最后确认

如果你只需要一个最稳妥的结论，当前建议是：

1. 开发时项目级构建可直接用各自 `.csproj`。
2. 交叉验证或整体验证时，方案级构建固定用串行参数。
3. 回归检查直接跑 `csharp/ClawCode.Tests`。
