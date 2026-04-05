using ClawCode.Commands;
using ClawCode.Tools;

namespace ClawCode.Execution;

/// <summary>
/// 命令镜像执行器。
/// </summary>
public sealed record MirroredCommand(string Name, string SourceHint)
{
    public string Execute(string prompt)
    {
        return CommandManager.ExecuteCommand(Name, prompt).Message;
    }
}

/// <summary>
/// 工具镜像执行器。
/// </summary>
public sealed record MirroredTool(string Name, string SourceHint)
{
    public string Execute(string payload)
    {
        return ToolManager.ExecuteTool(Name, payload).Message;
    }
}

/// <summary>
/// 按名称索引命令和工具的轻量注册表。
/// </summary>
public sealed class ExecutionRegistry
{
    public IReadOnlyList<MirroredCommand> Commands { get; }
    public IReadOnlyList<MirroredTool> Tools { get; }

    public ExecutionRegistry(IReadOnlyList<MirroredCommand> commands, IReadOnlyList<MirroredTool> tools)
    {
        Commands = commands;
        Tools = tools;
    }

    public MirroredCommand? Command(string name)
    {
        return Commands.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public MirroredTool? Tool(string name)
    {
        return Tools.FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// 构建执行注册表。
/// </summary>
public static class ExecutionRegistryBuilder
{
    public static ExecutionRegistry Build()
    {
        return new ExecutionRegistry(
            CommandManager.PortedCommands
                .Select(module => new MirroredCommand(module.Name, module.SourceHint))
                .ToList()
                .AsReadOnly(),
            ToolManager.PortedTools
                .Select(module => new MirroredTool(module.Name, module.SourceHint))
                .ToList()
                .AsReadOnly());
    }
}
