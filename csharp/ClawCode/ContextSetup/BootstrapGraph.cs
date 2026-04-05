namespace ClawCode.ContextSetup;

/// <summary>
/// 启动图的线性阶段表达。
/// </summary>
public sealed class BootstrapGraph
{
    public IReadOnlyList<string> Stages { get; }

    public BootstrapGraph(IReadOnlyList<string> stages)
    {
        Stages = stages;
    }

    public string ToMarkdown()
    {
        var lines = new List<string> { "# Bootstrap Graph", "" };
        lines.AddRange(Stages.Select(stage => $"- {stage}"));
        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// 构建与 Python 版同边界的 bootstrap 阶段列表。
/// </summary>
public static class BootstrapGraphBuilder
{
    public static BootstrapGraph Build()
    {
        return new BootstrapGraph(new[]
        {
            "top-level prefetch side effects",
            "warning handler and environment guards",
            "CLI parser and pre-action trust gate",
            "setup() + commands/agents parallel load",
            "deferred init after trust",
            "mode routing: local / remote / ssh / teleport / direct-connect / deep-link",
            "query engine submit loop",
        });
    }
}
