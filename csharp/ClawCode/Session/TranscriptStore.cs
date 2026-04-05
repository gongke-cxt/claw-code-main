namespace ClawCode.Session;

/// <summary>
/// 只保留最基本的消息轨迹行为，和 Python 工作台一样轻量。
/// </summary>
public sealed class TranscriptStore
{
    private readonly List<string> _entries = new();

    public IReadOnlyList<string> Entries => _entries;

    public bool Flushed { get; private set; }

    public void Append(string entry)
    {
        _entries.Add(entry);
        Flushed = false;
    }

    public void Compact(int keepLast = 10)
    {
        if (_entries.Count <= keepLast)
        {
            return;
        }

        _entries.RemoveRange(0, _entries.Count - keepLast);
    }

    public IReadOnlyList<string> Replay()
    {
        return _entries.ToList().AsReadOnly();
    }

    public void Flush()
    {
        Flushed = true;
    }
}
