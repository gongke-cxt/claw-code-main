using ClawCode.Models;
using ClawCode.Permissions;

namespace ClawCode.Tools;

/// <summary>
/// 组装后的工具池视图。
/// </summary>
public sealed class ToolPool
{
    public IReadOnlyList<PortingModule> Tools { get; }
    public bool SimpleMode { get; }
    public bool IncludeMcp { get; }

    public ToolPool(IReadOnlyList<PortingModule> tools, bool simpleMode, bool includeMcp)
    {
        Tools = tools;
        SimpleMode = simpleMode;
        IncludeMcp = includeMcp;
    }

    public string ToMarkdown()
    {
        var lines = new List<string>
        {
            "# Tool Pool",
            "",
            $"Simple mode: {SimpleMode}",
            $"Include MCP: {IncludeMcp}",
            $"Tool count: {Tools.Count}",
        };
        lines.AddRange(Tools.Take(15).Select(tool => $"- {tool.Name} - {tool.SourceHint}"));
        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// 组装工具池。
/// </summary>
public static class ToolPoolBuilder
{
    public static ToolPool Assemble(
        bool simpleMode = false,
        bool includeMcp = true,
        ToolPermissionContext? permissionContext = null)
    {
        return new ToolPool(
            ToolManager.GetTools(simpleMode, includeMcp, permissionContext),
            simpleMode,
            includeMcp);
    }
}
