using System.Text.Json;
using ClawCode.Context;
using ClawCode.Models;

namespace ClawCode.Commands;

/// <summary>
/// 记录一次命令镜像执行的结果。
/// </summary>
public sealed record CommandExecution(
    string Name,
    string SourceHint,
    string Prompt,
    bool Handled,
    string Message);

/// <summary>
/// 从快照加载、筛选和执行命令镜像。
/// </summary>
public static class CommandManager
{
    private static readonly string SnapshotPath = Path.Combine(
        WorkspaceEnvironment.Paths.ReferenceDataRoot, "commands_snapshot.json");

    private static readonly Lazy<IReadOnlyList<PortingModule>> PortedCommandsCache = new(LoadSnapshot);

    public static IReadOnlyList<PortingModule> PortedCommands => PortedCommandsCache.Value;

    public static IReadOnlySet<string> BuiltInCommandNames =>
        PortedCommands.Select(module => module.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static PortingBacklog BuildCommandBacklog()
    {
        return new PortingBacklog("Command surface", PortedCommands.ToList());
    }

    public static List<string> CommandNames()
    {
        return PortedCommands.Select(module => module.Name).ToList();
    }

    public static PortingModule? GetCommand(string name)
    {
        return PortedCommands.FirstOrDefault(module =>
            module.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<PortingModule> GetCommands(
        bool includePluginCommands = true,
        bool includeSkillCommands = true)
    {
        var commands = PortedCommands.ToList();

        if (!includePluginCommands)
        {
            commands = commands
                .Where(module => !module.SourceHint.Contains("plugin", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!includeSkillCommands)
        {
            commands = commands
                .Where(module => !module.SourceHint.Contains("skills", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return commands.AsReadOnly();
    }

    public static List<PortingModule> FindCommands(string query, int limit = 20)
    {
        return PortedCommands
            .Where(module =>
                module.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                module.SourceHint.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList();
    }

    public static CommandExecution ExecuteCommand(string name, string prompt = "")
    {
        var module = GetCommand(name);
        if (module is null)
        {
            return new CommandExecution(name, "", prompt, false, $"未知镜像命令: {name}");
        }

        var action = $"镜像命令 '{module.Name}' 来自 {module.SourceHint}，将处理提示: {prompt}";
        return new CommandExecution(module.Name, module.SourceHint, prompt, true, action);
    }

    public static string RenderCommandIndex(int limit = 20, string? query = null)
    {
        var modules = query is not null
            ? FindCommands(query, limit)
            : PortedCommands.Take(limit).ToList();

        var lines = new List<string> { $"命令条目数: {PortedCommands.Count}", "" };
        if (query is not null)
        {
            lines.Add($"过滤关键字: {query}");
            lines.Add("");
        }

        lines.AddRange(modules.Select(module => $"- {module.Name} - {module.SourceHint}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<PortingModule> LoadSnapshot()
    {
        if (!File.Exists(SnapshotPath))
        {
            return Array.Empty<PortingModule>();
        }

        var entries = JsonSerializer.Deserialize<List<SnapshotEntry>>(File.ReadAllText(SnapshotPath)) ?? new List<SnapshotEntry>();
        return entries
            .Select(entry => new PortingModule(
                entry.Name,
                entry.Responsibility,
                entry.SourceHint,
                "mirrored"))
            .ToList()
            .AsReadOnly();
    }
}
