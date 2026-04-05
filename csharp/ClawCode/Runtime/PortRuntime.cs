using ClawCode.Commands;
using ClawCode.Context;
using ClawCode.Execution;
using ClawCode.Models;
using ClawCode.QueryEngine;
using ClawCode.Setup;
using ClawCode.Tools;

namespace ClawCode.Runtime;

/// <summary>
/// bootstrap 后的一次会话快照。
/// </summary>
public sealed class RuntimeSession
{
    public string Prompt { get; }
    public PortContext Context { get; }
    public WorkspaceSetup Setup { get; }
    public SetupReport SetupReport { get; }
    public string SystemInitMessage { get; }
    public HistoryLog History { get; }
    public IReadOnlyList<RoutedMatch> RoutedMatches { get; }
    public TurnResult TurnResult { get; }
    public IReadOnlyList<string> CommandExecutionMessages { get; }
    public IReadOnlyList<string> ToolExecutionMessages { get; }
    public IReadOnlyList<StreamEvent> StreamEvents { get; }
    public string PersistedSessionPath { get; }

    public RuntimeSession(
        string prompt,
        PortContext context,
        WorkspaceSetup setup,
        SetupReport setupReport,
        string systemInitMessage,
        HistoryLog history,
        IReadOnlyList<RoutedMatch> routedMatches,
        TurnResult turnResult,
        IReadOnlyList<string> commandExecutionMessages,
        IReadOnlyList<string> toolExecutionMessages,
        IReadOnlyList<StreamEvent> streamEvents,
        string persistedSessionPath)
    {
        Prompt = prompt;
        Context = context;
        Setup = setup;
        SetupReport = setupReport;
        SystemInitMessage = systemInitMessage;
        History = history;
        RoutedMatches = routedMatches;
        TurnResult = turnResult;
        CommandExecutionMessages = commandExecutionMessages;
        ToolExecutionMessages = toolExecutionMessages;
        StreamEvents = streamEvents;
        PersistedSessionPath = persistedSessionPath;
    }

    public string ToMarkdown()
    {
        var lines = new List<string>
        {
            "# Runtime Session",
            "",
            $"Prompt: {Prompt}",
            "",
            "## Context",
            PortContextBuilder.Render(Context),
            "",
            "## Setup",
            $"- .NET: {Setup.DotnetVersion} ({Setup.RuntimeDescription})",
            $"- Platform: {Setup.PlatformName}",
            $"- Test command: {Setup.TestCommand}",
            "",
            "## Startup Steps",
        };
        lines.AddRange(Setup.StartupSteps().Select(step => $"- {step}"));
        lines.Add("");
        lines.Add("## System Init");
        lines.Add(SystemInitMessage);
        lines.Add("");
        lines.Add("## Routed Matches");
        lines.AddRange(RoutedMatches.Count > 0
            ? RoutedMatches.Select(match => $"- [{match.Kind}] {match.Name} ({match.Score}) - {match.SourceHint}")
            : new[] { "- none" });
        lines.Add("");
        lines.Add("## Command Execution");
        lines.AddRange(CommandExecutionMessages.Count > 0 ? CommandExecutionMessages : new[] { "none" });
        lines.Add("");
        lines.Add("## Tool Execution");
        lines.AddRange(ToolExecutionMessages.Count > 0 ? ToolExecutionMessages : new[] { "none" });
        lines.Add("");
        lines.Add("## Stream Events");
        lines.AddRange(StreamEvents.Select(streamEvent => $"- {streamEvent.Type}: {streamEvent.PayloadJson}"));
        lines.Add("");
        lines.Add("## Turn Result");
        lines.Add(TurnResult.Output);
        lines.Add("");
        lines.Add($"Persisted session path: {PersistedSessionPath}");
        lines.Add("");
        lines.Add(History.ToMarkdown());
        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// 轻量运行时，负责路由、bootstrap 和 turn-loop。
/// </summary>
public static class PortRuntime
{
    public static IReadOnlyList<RoutedMatch> RoutePrompt(string prompt, int limit = 5)
    {
        var tokens = prompt
            .Replace("/", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var byKind = new Dictionary<string, List<RoutedMatch>>
        {
            ["command"] = CollectMatches(tokens, CommandManager.PortedCommands, "command"),
            ["tool"] = CollectMatches(tokens, ToolManager.PortedTools, "tool"),
        };

        var selected = new List<RoutedMatch>();
        foreach (var kind in new[] { "command", "tool" })
        {
            if (byKind[kind].Count > 0)
            {
                selected.Add(byKind[kind][0]);
                byKind[kind].RemoveAt(0);
            }
        }

        var leftovers = byKind.Values
            .SelectMany(item => item)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Kind, StringComparer.Ordinal)
            .ThenBy(match => match.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        selected.AddRange(leftovers.Take(Math.Max(0, limit - selected.Count)));
        return selected.Take(limit).ToList().AsReadOnly();
    }

    public static RuntimeSession BootstrapSession(string prompt, int limit = 5)
    {
        var context = PortContextBuilder.Build();
        var setupReport = SetupService.Run(trusted: true);
        var history = new HistoryLog();
        var engine = QueryEnginePort.FromWorkspace();

        history.Add("context", $"python_files={context.PythonFileCount}, archive_available={context.ArchiveAvailable}");
        history.Add("registry", $"commands={CommandManager.PortedCommands.Count}, tools={ToolManager.PortedTools.Count}");

        var matches = RoutePrompt(prompt, limit);
        var registry = ExecutionRegistryBuilder.Build();
        var commandExecs = matches
            .Where(match => match.Kind == "command")
            .Select(match => registry.Command(match.Name))
            .Where(command => command is not null)
            .Select(command => command!.Execute(prompt))
            .ToList()
            .AsReadOnly();
        var toolExecs = matches
            .Where(match => match.Kind == "tool")
            .Select(match => registry.Tool(match.Name))
            .Where(tool => tool is not null)
            .Select(tool => tool!.Execute(prompt))
            .ToList()
            .AsReadOnly();
        var denials = InferPermissionDenials(matches).AsReadOnly();

        var turnResult = engine.SubmitMessage(
            prompt,
            matches.Where(match => match.Kind == "command").Select(match => match.Name).ToList().AsReadOnly(),
            matches.Where(match => match.Kind == "tool").Select(match => match.Name).ToList().AsReadOnly(),
            denials);
        var streamEvents = engine.BuildStreamEvents(prompt, turnResult);
        var persistedSessionPath = engine.PersistSession();

        history.Add("routing", $"matches={matches.Count} for prompt='{prompt}'");
        history.Add("execution", $"command_execs={commandExecs.Count} tool_execs={toolExecs.Count}");
        history.Add("turn", $"commands={turnResult.MatchedCommands.Count} tools={turnResult.MatchedTools.Count} denials={turnResult.PermissionDenials.Count} stop={turnResult.StopReason}");
        history.Add("session_store", persistedSessionPath);

        return new RuntimeSession(
            prompt,
            context,
            setupReport.Setup,
            setupReport,
            SystemInitBuilder.Build(true),
            history,
            matches,
            turnResult,
            commandExecs,
            toolExecs,
            streamEvents,
            persistedSessionPath);
    }

    public static IReadOnlyList<TurnResult> RunTurnLoop(
        string prompt,
        int limit = 5,
        int maxTurns = 3,
        bool structuredOutput = false)
    {
        var engine = QueryEnginePort.FromWorkspace();
        engine.Config = new QueryEngineConfig(MaxTurns: maxTurns, StructuredOutput: structuredOutput);

        var matches = RoutePrompt(prompt, limit);
        var commandNames = matches.Where(match => match.Kind == "command").Select(match => match.Name).ToList().AsReadOnly();
        var toolNames = matches.Where(match => match.Kind == "tool").Select(match => match.Name).ToList().AsReadOnly();
        var results = new List<TurnResult>();

        for (var turn = 0; turn < maxTurns; turn++)
        {
            var turnPrompt = turn == 0 ? prompt : $"{prompt} [turn {turn + 1}]";
            var result = engine.SubmitMessage(turnPrompt, commandNames, toolNames, Array.Empty<PermissionDenial>());
            results.Add(result);
            if (!string.Equals(result.StopReason, "completed", StringComparison.Ordinal))
            {
                break;
            }
        }

        return results.AsReadOnly();
    }

    private static List<PermissionDenial> InferPermissionDenials(IReadOnlyList<RoutedMatch> matches)
    {
        return matches
            .Where(match => match.Kind == "tool" && match.Name.Contains("bash", StringComparison.OrdinalIgnoreCase))
            .Select(match => new PermissionDenial(match.Name, "destructive shell execution remains gated in the C# port"))
            .ToList();
    }

    private static List<RoutedMatch> CollectMatches(
        IReadOnlySet<string> tokens,
        IEnumerable<PortingModule> modules,
        string kind)
    {
        return modules
            .Select(module => new RoutedMatch(kind, module.Name, module.SourceHint, Score(tokens, module)))
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int Score(IReadOnlySet<string> tokens, PortingModule module)
    {
        var haystacks = new[]
        {
            module.Name.ToLowerInvariant(),
            module.SourceHint.ToLowerInvariant(),
            module.Responsibility.ToLowerInvariant(),
        };

        var score = 0;
        foreach (var token in tokens)
        {
            if (haystacks.Any(haystack => haystack.Contains(token, StringComparison.Ordinal)))
            {
                score += 1;
            }
        }

        return score;
    }
}
