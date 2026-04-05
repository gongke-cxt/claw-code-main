namespace ClawCode.Runtime;

/// <summary>
/// 记录一次 bootstrap 过程中的关键事件。
/// </summary>
public sealed record HistoryEvent(string Title, string Detail);

/// <summary>
/// 轻量历史日志，保持和 Python 版本一样可读。
/// </summary>
public sealed class HistoryLog
{
    private readonly List<HistoryEvent> _events = new();

    public void Add(string title, string detail)
    {
        _events.Add(new HistoryEvent(title, detail));
    }

    public string ToMarkdown()
    {
        var lines = new List<string> { "# Session History", "" };
        lines.AddRange(_events.Select(item => $"- {item.Title}: {item.Detail}"));
        return string.Join(Environment.NewLine, lines);
    }
}
