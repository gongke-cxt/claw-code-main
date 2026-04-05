namespace ClawCode.Models;

/// <summary>
/// 顶层子系统摘要，来自仓库当前的 Python 工作台。
/// </summary>
public sealed record Subsystem(
    string Name,
    string Path,
    int FileCount,
    string Notes);

/// <summary>
/// 命令或工具镜像条目，用来承接快照里的元数据。
/// </summary>
public sealed record PortingModule(
    string Name,
    string Responsibility,
    string SourceHint,
    string Status = "planned");

/// <summary>
/// 记录工具因权限策略被拒绝的原因。
/// </summary>
public sealed record PermissionDenial(
    string ToolName,
    string Reason);

/// <summary>
/// 用空白分词近似统计输入输出规模，保持和 Python 工作台一致的轻量风格。
/// </summary>
public sealed class UsageSummary
{
    public int InputTokens { get; }
    public int OutputTokens { get; }

    public UsageSummary(int inputTokens = 0, int outputTokens = 0)
    {
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
    }

    public UsageSummary AddTurn(string prompt, string output)
    {
        return new UsageSummary(
            InputTokens + CountTokens(prompt),
            OutputTokens + CountTokens(output));
    }

    private static int CountTokens(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

/// <summary>
/// 汇总一个功能面上的待移植模块。
/// </summary>
public sealed class PortingBacklog
{
    public string Title { get; }
    public List<PortingModule> Modules { get; }

    public PortingBacklog(string title, List<PortingModule>? modules = null)
    {
        Title = title;
        Modules = modules ?? new List<PortingModule>();
    }

    public List<string> SummaryLines()
    {
        return Modules
            .Select(module => $"- {module.Name} [{module.Status}] - {module.Responsibility} (来自 {module.SourceHint})")
            .ToList();
    }
}

/// <summary>
/// 保留一个极简任务模型，方便后续继续扩展到更完整的移植看板。
/// </summary>
public sealed record PortingTask(string Id, string Description);

/// <summary>
/// 轻量查询引擎的配置。
/// </summary>
public sealed record QueryEngineConfig(
    int MaxTurns = 8,
    int MaxBudgetTokens = 2000,
    int CompactAfterTurns = 12,
    bool StructuredOutput = false,
    int StructuredRetryLimit = 2);

/// <summary>
/// 一次提交后的归档结果。
/// </summary>
public sealed record TurnResult(
    string Prompt,
    string Output,
    IReadOnlyList<string> MatchedCommands,
    IReadOnlyList<string> MatchedTools,
    IReadOnlyList<PermissionDenial> PermissionDenials,
    UsageSummary Usage,
    string StopReason);

/// <summary>
/// 路由器选出的候选项。
/// </summary>
public sealed record RoutedMatch(
    string Kind,
    string Name,
    string SourceHint,
    int Score);

/// <summary>
/// 用于渲染流式事件，避免把大量强类型层级堆到控制台项目里。
/// </summary>
public sealed record StreamEvent(
    string Type,
    string PayloadJson);
