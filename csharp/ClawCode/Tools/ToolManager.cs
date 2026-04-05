using System.Text.Json;
using ClawCode.Context;
using ClawCode.Models;
using ClawCode.Permissions;

namespace ClawCode.Tools;

/// <summary>
/// 记录一次工具镜像执行的结果。
/// </summary>
public sealed record ToolExecution(
    string Name,
    string SourceHint,
    string Payload,
    bool Handled,
    string Message);

/// <summary>
/// 从快照加载、筛选和执行工具镜像。
/// </summary>
public static class ToolManager
{
    private static readonly string SnapshotPath = Path.Combine(
        WorkspaceEnvironment.Paths.ReferenceDataRoot, "tools_snapshot.json");

    private static readonly Lazy<IReadOnlyList<PortingModule>> PortedToolsCache = new(LoadSnapshot);

    public static IReadOnlyList<PortingModule> PortedTools => PortedToolsCache.Value;

    public static PortingBacklog BuildToolBacklog()
    {
        return new PortingBacklog("Tool surface", PortedTools.ToList());
    }

    public static List<string> ToolNames()
    {
        return PortedTools.Select(module => module.Name).ToList();
    }

    public static PortingModule? GetTool(string name)
    {
        return PortedTools.FirstOrDefault(module =>
            module.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<PortingModule> FilterByPermission(
        IReadOnlyList<PortingModule> tools,
        ToolPermissionContext? permissionContext)
    {
        if (permissionContext is null)
        {
            return tools;
        }

        return tools
            .Where(module => !permissionContext.Blocks(module.Name))
            .ToList()
            .AsReadOnly();
    }

    public static IReadOnlyList<PortingModule> GetTools(
        bool simpleMode = false,
        bool includeMcp = true,
        ToolPermissionContext? permissionContext = null)
    {
        var tools = PortedTools.ToList();

        if (simpleMode)
        {
            tools = tools
                .Where(module => module.Name is "BashTool" or "FileReadTool" or "FileEditTool")
                .ToList();
        }

        if (!includeMcp)
        {
            tools = tools
                .Where(module =>
                    !module.Name.Contains("mcp", StringComparison.OrdinalIgnoreCase) &&
                    !module.SourceHint.Contains("mcp", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return FilterByPermission(tools.AsReadOnly(), permissionContext);
    }

    public static List<PortingModule> FindTools(string query, int limit = 20)
    {
        return PortedTools
            .Where(module =>
                module.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                module.SourceHint.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList();
    }

    public static ToolExecution ExecuteTool(string name, string payload = "")
    {
        var module = GetTool(name);
        if (module is null)
        {
            return new ToolExecution(name, "", payload, false, $"未知镜像工具: {name}");
        }

        var action = $"镜像工具 '{module.Name}' 来自 {module.SourceHint}，将处理负载: {payload}";
        return new ToolExecution(module.Name, module.SourceHint, payload, true, action);
    }

    public static string RenderToolIndex(int limit = 20, string? query = null)
    {
        var modules = query is not null
            ? FindTools(query, limit)
            : PortedTools.Take(limit).ToList();

        var lines = new List<string> { $"工具条目数: {PortedTools.Count}", "" };
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
