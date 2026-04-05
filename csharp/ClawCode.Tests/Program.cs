using System.Text;
using ClawCode.Commands;
using ClawCode.ContextSetup;
using ClawCode.Parity;
using ClawCode.Permissions;
using ClawCode.QueryEngine;
using ClawCode.Remote;
using ClawCode.Runtime;
using ClawCode.Session;
using ClawCode.Subsystems;
using ClawCode.Tools;

namespace ClawCode.Tests;

internal static class Program
{
    private sealed record TestCase(string Name, Action<TestEnvironment> Execute);

    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var tests = new[]
        {
            new TestCase("Manifest_BuildsNontrivialWorkspace", ManifestBuildsNontrivialWorkspace),
            new TestCase("Summary_ContainsCoreSections", SummaryContainsCoreSections),
            new TestCase("Snapshots_AreNontrivial", SnapshotsAreNontrivial),
            new TestCase("CommandAndToolQueries_Work", CommandAndToolQueriesWork),
            new TestCase("ToolPermissionContext_FiltersByPrefix", ToolPermissionContextFiltersByPrefix),
            new TestCase("RoutePrompt_ReturnsCommandAndToolMatches", RoutePromptReturnsCommandAndToolMatches),
            new TestCase("BootstrapSession_RendersAndPersists", BootstrapSessionRendersAndPersists),
            new TestCase("PersistAndLoadSession_RoundTrips", PersistAndLoadSessionRoundTrips),
            new TestCase("TurnLoop_StructuredOutput_Works", TurnLoopStructuredOutputWorks),
            new TestCase("RemoteModes_ExposeExpectedMarkers", RemoteModesExposeExpectedMarkers),
            new TestCase("ParityAudit_Runs", ParityAuditRuns),
            new TestCase("Cli_SmokeSummaryAndCommands", CliSmokeSummaryAndCommands),
            new TestCase("Graphs_AndPools_Render", GraphsAndPoolsRender),
        };

        var failures = 0;
        using var environment = new TestEnvironment();
        foreach (var test in tests)
        {
            try
            {
                test.Execute(environment);
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failures += 1;
                Console.WriteLine($"FAIL {test.Name}");
                Console.WriteLine(ex.Message);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {tests.Length}, Failed: {failures}");
        return failures == 0 ? 0 : 1;
    }

    private static void ManifestBuildsNontrivialWorkspace(TestEnvironment _)
    {
        var manifest = PortManifestBuilder.Build();
        Assertions.True(manifest.TotalPythonFiles >= 20, "Python 文件数应大于等于 20。");
        Assertions.True(manifest.TopLevelModules.Count > 0, "顶层模块列表不能为空。");
        Assertions.Contains("Top-level Python modules:", manifest.ToMarkdown(), "manifest 应包含模块章节。");
    }

    private static void SummaryContainsCoreSections(TestEnvironment _)
    {
        var summary = QueryEnginePort.FromWorkspace().RenderSummary();
        Assertions.Contains("# C# Porting Workspace Summary", summary, "summary 标题缺失。");
        Assertions.Contains("Command surface:", summary, "summary 应包含命令面。");
        Assertions.Contains("Tool surface:", summary, "summary 应包含工具面。");
    }

    private static void SnapshotsAreNontrivial(TestEnvironment _)
    {
        Assertions.True(CommandManager.PortedCommands.Count >= 150, "命令快照不应过小。");
        Assertions.True(ToolManager.PortedTools.Count >= 100, "工具快照不应过小。");
    }

    private static void CommandAndToolQueriesWork(TestEnvironment _)
    {
        var commandOutput = CommandManager.RenderCommandIndex(limit: 5, query: "review");
        var toolOutput = ToolManager.RenderToolIndex(limit: 5, query: "MCP");

        Assertions.Contains("命令条目数", commandOutput, "命令查询输出标题缺失。");
        Assertions.Contains("过滤关键字: review", commandOutput, "命令查询过滤条件缺失。");
        Assertions.Contains("工具条目数", toolOutput, "工具查询输出标题缺失。");
        Assertions.ContainsIgnoreCase("mcp", toolOutput, "工具查询应包含 mcp 相关结果。");
    }

    private static void ToolPermissionContextFiltersByPrefix(TestEnvironment _)
    {
        var filtered = ToolManager.GetTools(permissionContext: ToolPermissionContext.FromCollections(
            denyPrefixes: new[] { "mcp" }));

        Assertions.False(
            filtered.Any(module => module.Name.StartsWith("mcp", StringComparison.OrdinalIgnoreCase)),
            "deny-prefix=mcp 后仍然出现以 mcp 开头的工具。");
        Assertions.False(
            filtered.Any(module => module.Name.Equals("MCPTool", StringComparison.OrdinalIgnoreCase)),
            "deny-prefix=mcp 后仍然出现 MCPTool。");
    }

    private static void RoutePromptReturnsCommandAndToolMatches(TestEnvironment _)
    {
        var matches = PortRuntime.RoutePrompt("review MCP tool", 5);
        Assertions.True(matches.Count > 0, "路由结果不能为空。");
        Assertions.True(matches.Any(match => match.Kind == "command"), "路由结果应包含至少一个命令。");
        Assertions.True(matches.Any(match => match.Kind == "tool"), "路由结果应包含至少一个工具。");
    }

    private static void BootstrapSessionRendersAndPersists(TestEnvironment environment)
    {
        var session = PortRuntime.BootstrapSession("review MCP tool", 5);
        environment.TrackFile(session.PersistedSessionPath);

        var markdown = session.ToMarkdown();
        Assertions.Contains("# Runtime Session", markdown, "bootstrap 输出缺少标题。");
        Assertions.Contains("## Startup Steps", markdown, "bootstrap 输出缺少 startup steps。");
        Assertions.FileExists(session.PersistedSessionPath, "bootstrap 应持久化 session 文件。");
    }

    private static void PersistAndLoadSessionRoundTrips(TestEnvironment environment)
    {
        var engine = QueryEnginePort.FromWorkspace();
        engine.SubmitMessage("review MCP tool", new[] { "review" }, new[] { "MCPTool" }, Array.Empty<ClawCode.Models.PermissionDenial>());
        var path = engine.PersistSession();
        environment.TrackFile(path);

        var loaded = SessionStore.Load(Path.GetFileNameWithoutExtension(path));
        Assertions.Equal(engine.SessionId, loaded.SessionId, "session id 不一致。");
        Assertions.Equal(1, loaded.Messages.Count, "持久化消息数不正确。");
    }

    private static void TurnLoopStructuredOutputWorks(TestEnvironment _)
    {
        var results = PortRuntime.RunTurnLoop("review MCP tool", maxTurns: 2, structuredOutput: true);
        Assertions.True(results.Count >= 1, "turn loop 至少应返回一轮。");
        Assertions.Contains("\"summary\"", results[0].Output, "结构化输出应包含 summary。");
        Assertions.Contains("\"session_id\"", results[0].Output, "结构化输出应包含 session_id。");
    }

    private static void RemoteModesExposeExpectedMarkers(TestEnvironment _)
    {
        Assertions.Contains("mode=remote", RuntimeModes.RunRemoteMode("workspace").AsText(), "remote-mode 标记缺失。");
        Assertions.Contains("mode=ssh", RuntimeModes.RunSshMode("workspace").AsText(), "ssh-mode 标记缺失。");
        Assertions.Contains("mode=teleport", RuntimeModes.RunTeleportMode("workspace").AsText(), "teleport-mode 标记缺失。");
        Assertions.Contains("mode=direct-connect", RuntimeModes.RunDirectConnect("workspace").AsText(), "direct-connect-mode 标记缺失。");
        Assertions.Contains("mode=deep-link", RuntimeModes.RunDeepLink("workspace").AsText(), "deep-link-mode 标记缺失。");
    }

    private static void ParityAuditRuns(TestEnvironment _)
    {
        var result = ParityAudit.Run();
        Assertions.True(result.CommandEntryRatio.Current >= 150, "parity audit 命令条目数过低。");
        Assertions.True(result.ToolEntryRatio.Current >= 100, "parity audit 工具条目数过低。");
        Assertions.Contains("# Parity Audit", result.ToMarkdown(), "parity audit 标题缺失。");
    }

    private static void CliSmokeSummaryAndCommands(TestEnvironment environment)
    {
        var summary = environment.RunCli("summary");
        var commands = environment.RunCli("commands", "--limit", "3", "--query", "review");

        Assertions.Contains("# C# Porting Workspace Summary", summary, "CLI summary 输出异常。");
        Assertions.Contains("命令条目数", commands, "CLI commands 输出异常。");
        Assertions.Contains("review", commands, "CLI commands 查询结果异常。");
    }

    private static void GraphsAndPoolsRender(TestEnvironment _)
    {
        var commandGraph = CommandGraphBuilder.Build().ToMarkdown();
        var toolPool = ToolPoolBuilder.Assemble(simpleMode: true, includeMcp: false).ToMarkdown();
        var bootstrapGraph = BootstrapGraphBuilder.Build().ToMarkdown();

        Assertions.Contains("# Command Graph", commandGraph, "command graph 标题缺失。");
        Assertions.Contains("# Tool Pool", toolPool, "tool pool 标题缺失。");
        Assertions.Contains("# Bootstrap Graph", bootstrapGraph, "bootstrap graph 标题缺失。");
    }
}
