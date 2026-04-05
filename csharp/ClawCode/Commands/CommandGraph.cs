using ClawCode.Models;

namespace ClawCode.Commands;

/// <summary>
/// 命令图的粗粒度分段。
/// </summary>
public sealed class CommandGraph
{
    public IReadOnlyList<PortingModule> Builtins { get; }
    public IReadOnlyList<PortingModule> PluginLike { get; }
    public IReadOnlyList<PortingModule> SkillLike { get; }

    public CommandGraph(
        IReadOnlyList<PortingModule> builtins,
        IReadOnlyList<PortingModule> pluginLike,
        IReadOnlyList<PortingModule> skillLike)
    {
        Builtins = builtins;
        PluginLike = pluginLike;
        SkillLike = skillLike;
    }

    public string ToMarkdown()
    {
        return string.Join(Environment.NewLine, new[]
        {
            "# Command Graph",
            "",
            $"Builtins: {Builtins.Count}",
            $"Plugin-like commands: {PluginLike.Count}",
            $"Skill-like commands: {SkillLike.Count}",
        });
    }
}

/// <summary>
/// 从命令来源提示构建图分段。
/// </summary>
public static class CommandGraphBuilder
{
    public static CommandGraph Build()
    {
        var commands = CommandManager.GetCommands();
        return new CommandGraph(
            builtins: commands
                .Where(module =>
                    !module.SourceHint.Contains("plugin", StringComparison.OrdinalIgnoreCase) &&
                    !module.SourceHint.Contains("skills", StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly(),
            pluginLike: commands
                .Where(module => module.SourceHint.Contains("plugin", StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly(),
            skillLike: commands
                .Where(module => module.SourceHint.Contains("skills", StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly());
    }
}
