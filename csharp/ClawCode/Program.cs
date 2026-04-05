using System.Text;
using ClawCode.CLI;
using ClawCode.Commands;
using ClawCode.ContextSetup;
using ClawCode.Parity;
using ClawCode.Permissions;
using ClawCode.QueryEngine;
using ClawCode.Remote;
using ClawCode.Runtime;
using ClawCode.Session;
using ClawCode.Setup;
using ClawCode.Subsystems;
using ClawCode.Tools;

namespace ClawCode;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var parsed = CommandArguments.Parse(args);
        if (string.IsNullOrWhiteSpace(parsed.Command))
        {
            PrintHelp();
            return 1;
        }

        return parsed.Command switch
        {
            "summary" => RunSummary(),
            "manifest" => RunManifest(),
            "parity-audit" => RunParityAudit(),
            "setup-report" => RunSetupReport(),
            "command-graph" => RunCommandGraph(),
            "tool-pool" => RunToolPool(parsed),
            "bootstrap-graph" => RunBootstrapGraph(),
            "subsystems" => RunSubsystems(parsed),
            "commands" => RunCommands(parsed),
            "tools" => RunTools(parsed),
            "route" => RunRoute(parsed),
            "bootstrap" => RunBootstrap(parsed),
            "turn-loop" => RunTurnLoop(parsed),
            "flush-transcript" => RunFlushTranscript(parsed),
            "load-session" => RunLoadSession(parsed),
            "remote-mode" => RunRemoteMode(parsed),
            "ssh-mode" => RunSshMode(parsed),
            "teleport-mode" => RunTeleportMode(parsed),
            "direct-connect-mode" => RunDirectConnectMode(parsed),
            "deep-link-mode" => RunDeepLinkMode(parsed),
            "show-command" => RunShowCommand(parsed),
            "show-tool" => RunShowTool(parsed),
            "exec-command" => RunExecCommand(parsed),
            "exec-tool" => RunExecTool(parsed),
            "help" => PrintHelpAndReturn(),
            _ => UnknownCommand(parsed.Command),
        };
    }

    private static int RunSummary()
    {
        Console.WriteLine(QueryEnginePort.FromWorkspace().RenderSummary());
        return 0;
    }

    private static int RunManifest()
    {
        Console.WriteLine(PortManifestBuilder.Build().ToMarkdown());
        return 0;
    }

    private static int RunParityAudit()
    {
        Console.WriteLine(ParityAudit.Run().ToMarkdown());
        return 0;
    }

    private static int RunSetupReport()
    {
        Console.WriteLine(SetupService.Run(trusted: true).ToMarkdown());
        return 0;
    }

    private static int RunCommandGraph()
    {
        Console.WriteLine(CommandGraphBuilder.Build().ToMarkdown());
        return 0;
    }

    private static int RunToolPool(CommandArguments parsed)
    {
        var permissionContext = ToolPermissionContext.FromCollections(
            parsed.GetOptions("deny-tool"),
            parsed.GetOptions("deny-prefix"));
        Console.WriteLine(ToolPoolBuilder.Assemble(
            simpleMode: parsed.HasFlag("simple-mode"),
            includeMcp: !parsed.HasFlag("no-mcp"),
            permissionContext: permissionContext).ToMarkdown());
        return 0;
    }

    private static int RunBootstrapGraph()
    {
        Console.WriteLine(BootstrapGraphBuilder.Build().ToMarkdown());
        return 0;
    }

    private static int RunSubsystems(CommandArguments parsed)
    {
        var limit = parsed.GetIntOption("limit", 32);
        foreach (var subsystem in PortManifestBuilder.Build().TopLevelModules.Take(limit))
        {
            Console.WriteLine($"{subsystem.Name}\t{subsystem.FileCount}\t{subsystem.Notes}");
        }

        return 0;
    }

    private static int RunCommands(CommandArguments parsed)
    {
        var limit = parsed.GetIntOption("limit", 20);
        var query = parsed.GetOption("query");
        if (!string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine(CommandManager.RenderCommandIndex(limit, query));
            return 0;
        }

        var commands = CommandManager.GetCommands(
            includePluginCommands: !parsed.HasFlag("no-plugin-commands"),
            includeSkillCommands: !parsed.HasFlag("no-skill-commands"));
        var lines = new List<string> { $"命令条目数: {commands.Count}", "" };
        lines.AddRange(commands.Take(limit).Select(module => $"- {module.Name} - {module.SourceHint}"));
        Console.WriteLine(string.Join(Environment.NewLine, lines));
        return 0;
    }

    private static int RunTools(CommandArguments parsed)
    {
        var limit = parsed.GetIntOption("limit", 20);
        var query = parsed.GetOption("query");
        if (!string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine(ToolManager.RenderToolIndex(limit, query));
            return 0;
        }

        var permissionContext = ToolPermissionContext.FromCollections(
            parsed.GetOptions("deny-tool"),
            parsed.GetOptions("deny-prefix"));
        var tools = ToolManager.GetTools(
            simpleMode: parsed.HasFlag("simple-mode"),
            includeMcp: !parsed.HasFlag("no-mcp"),
            permissionContext: permissionContext);

        var lines = new List<string> { $"工具条目数: {tools.Count}", "" };
        lines.AddRange(tools.Take(limit).Select(module => $"- {module.Name} - {module.SourceHint}"));
        Console.WriteLine(string.Join(Environment.NewLine, lines));
        return 0;
    }

    private static int RunRoute(CommandArguments parsed)
    {
        var prompt = parsed.JoinPositionals();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Console.Error.WriteLine("route 需要一个 prompt。");
            return 1;
        }

        var matches = PortRuntime.RoutePrompt(prompt, parsed.GetIntOption("limit", 5));
        if (matches.Count == 0)
        {
            Console.WriteLine("No mirrored command/tool matches found.");
            return 0;
        }

        foreach (var match in matches)
        {
            Console.WriteLine($"{match.Kind}\t{match.Name}\t{match.Score}\t{match.SourceHint}");
        }

        return 0;
    }

    private static int RunBootstrap(CommandArguments parsed)
    {
        var prompt = parsed.JoinPositionals();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Console.Error.WriteLine("bootstrap 需要一个 prompt。");
            return 1;
        }

        Console.WriteLine(PortRuntime.BootstrapSession(prompt, parsed.GetIntOption("limit", 5)).ToMarkdown());
        return 0;
    }

    private static int RunTurnLoop(CommandArguments parsed)
    {
        var prompt = parsed.JoinPositionals();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Console.Error.WriteLine("turn-loop 需要一个 prompt。");
            return 1;
        }

        var results = PortRuntime.RunTurnLoop(
            prompt,
            limit: parsed.GetIntOption("limit", 5),
            maxTurns: parsed.GetIntOption("max-turns", 3),
            structuredOutput: parsed.HasFlag("structured-output"));
        for (var index = 0; index < results.Count; index++)
        {
            Console.WriteLine($"## Turn {index + 1}");
            Console.WriteLine(results[index].Output);
            Console.WriteLine($"stop_reason={results[index].StopReason}");
        }

        return 0;
    }

    private static int RunFlushTranscript(CommandArguments parsed)
    {
        var prompt = parsed.JoinPositionals();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Console.Error.WriteLine("flush-transcript 需要一个 prompt。");
            return 1;
        }

        var engine = QueryEnginePort.FromWorkspace();
        engine.SubmitMessage(prompt);
        var path = engine.PersistSession();
        Console.WriteLine(path);
        Console.WriteLine($"flushed={engine.TranscriptStore.Flushed}");
        return 0;
    }

    private static int RunLoadSession(CommandArguments parsed)
    {
        var sessionId = parsed.Positionals.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            Console.Error.WriteLine("load-session 需要一个 session_id。");
            return 1;
        }

        var session = SessionStore.Load(sessionId);
        Console.WriteLine($"{session.SessionId}{Environment.NewLine}{session.Messages.Count} messages{Environment.NewLine}in={session.InputTokens} out={session.OutputTokens}");
        return 0;
    }

    private static int RunRemoteMode(CommandArguments parsed)
    {
        Console.WriteLine(RuntimeModes.RunRemoteMode(parsed.JoinPositionals()).AsText());
        return 0;
    }

    private static int RunSshMode(CommandArguments parsed)
    {
        Console.WriteLine(RuntimeModes.RunSshMode(parsed.JoinPositionals()).AsText());
        return 0;
    }

    private static int RunTeleportMode(CommandArguments parsed)
    {
        Console.WriteLine(RuntimeModes.RunTeleportMode(parsed.JoinPositionals()).AsText());
        return 0;
    }

    private static int RunDirectConnectMode(CommandArguments parsed)
    {
        Console.WriteLine(RuntimeModes.RunDirectConnect(parsed.JoinPositionals()).AsText());
        return 0;
    }

    private static int RunDeepLinkMode(CommandArguments parsed)
    {
        Console.WriteLine(RuntimeModes.RunDeepLink(parsed.JoinPositionals()).AsText());
        return 0;
    }

    private static int RunShowCommand(CommandArguments parsed)
    {
        var name = parsed.JoinPositionals();
        var module = CommandManager.GetCommand(name);
        if (module is null)
        {
            Console.Error.WriteLine($"Command not found: {name}");
            return 1;
        }

        Console.WriteLine(string.Join(Environment.NewLine, new[] { module.Name, module.SourceHint, module.Responsibility }));
        return 0;
    }

    private static int RunShowTool(CommandArguments parsed)
    {
        var name = parsed.JoinPositionals();
        var module = ToolManager.GetTool(name);
        if (module is null)
        {
            Console.Error.WriteLine($"Tool not found: {name}");
            return 1;
        }

        Console.WriteLine(string.Join(Environment.NewLine, new[] { module.Name, module.SourceHint, module.Responsibility }));
        return 0;
    }

    private static int RunExecCommand(CommandArguments parsed)
    {
        var name = parsed.Positionals.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("exec-command 需要命令名。");
            return 1;
        }

        var prompt = parsed.JoinPositionals(1);
        var result = CommandManager.ExecuteCommand(name, prompt);
        Console.WriteLine(result.Message);
        return result.Handled ? 0 : 1;
    }

    private static int RunExecTool(CommandArguments parsed)
    {
        var name = parsed.Positionals.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("exec-tool 需要工具名。");
            return 1;
        }

        var payload = parsed.JoinPositionals(1);
        var result = ToolManager.ExecuteTool(name, payload);
        Console.WriteLine(result.Message);
        return result.Handled ? 0 : 1;
    }

    private static int PrintHelpAndReturn()
    {
        PrintHelp();
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"未知命令: {command}");
        PrintHelp();
        return 2;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            ClawCode C# 工作台

            常用命令:
              summary
              manifest
              parity-audit
              setup-report
              command-graph
              tool-pool [--simple-mode] [--no-mcp] [--deny-tool NAME] [--deny-prefix PREFIX]
              bootstrap-graph
              subsystems [--limit N]
              commands [--limit N] [--query TEXT] [--no-plugin-commands] [--no-skill-commands]
              tools [--limit N] [--query TEXT] [--simple-mode] [--no-mcp] [--deny-tool NAME] [--deny-prefix PREFIX]
              route <prompt> [--limit N]
              bootstrap <prompt> [--limit N]
              turn-loop <prompt> [--limit N] [--max-turns N] [--structured-output]
              flush-transcript <prompt>
              load-session <session_id>
              remote-mode <target>
              ssh-mode <target>
              teleport-mode <target>
              direct-connect-mode <target>
              deep-link-mode <target>
              show-command <name>
              show-tool <name>
              exec-command <name> <prompt>
              exec-tool <name> <payload>
            """);
    }
}
