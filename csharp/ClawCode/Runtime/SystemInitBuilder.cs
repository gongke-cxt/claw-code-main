using ClawCode.Commands;
using ClawCode.Setup;
using ClawCode.Tools;

namespace ClawCode.Runtime;

/// <summary>
/// 构建启动提示信息。
/// </summary>
public static class SystemInitBuilder
{
    public static string Build(bool trusted = true)
    {
        var setup = SetupService.Run(trusted: trusted);
        var commands = CommandManager.GetCommands();
        var tools = ToolManager.GetTools();

        var lines = new List<string>
        {
            "# System Init",
            "",
            $"Trusted: {setup.Trusted}",
            $"Built-in command names: {CommandManager.BuiltInCommandNames.Count}",
            $"Loaded command entries: {commands.Count}",
            $"Loaded tool entries: {tools.Count}",
            "",
            "Startup steps:",
        };
        lines.AddRange(setup.Setup.StartupSteps().Select(step => $"- {step}"));
        return string.Join(Environment.NewLine, lines);
    }
}
