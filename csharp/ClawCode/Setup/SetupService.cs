using System.Runtime.InteropServices;
using ClawCode.Context;

namespace ClawCode.Setup;

/// <summary>
/// 预取动作的观测结果。
/// </summary>
public sealed record PrefetchResult(
    string Name,
    bool Started,
    string Detail);

/// <summary>
/// 信任门通过后才开启的延迟初始化项。
/// </summary>
public sealed record DeferredInitResult(
    bool Trusted,
    bool PluginInit,
    bool SkillInit,
    bool McpPrefetch,
    bool SessionHooks)
{
    public IReadOnlyList<string> AsLines()
    {
        return new[]
        {
            $"- plugin_init={PluginInit}",
            $"- skill_init={SkillInit}",
            $"- mcp_prefetch={McpPrefetch}",
            $"- session_hooks={SessionHooks}",
        };
    }
}

/// <summary>
/// 运行当前 C# 工作台所需的基础环境信息。
/// </summary>
public sealed record WorkspaceSetup(
    string DotnetVersion,
    string RuntimeDescription,
    string PlatformName,
    string TestCommand = "dotnet build csharp/ClawCode/ClawCode.csproj")
{
    public IReadOnlyList<string> StartupSteps()
    {
        return new[]
        {
            "start top-level prefetch side effects",
            "build workspace context",
            "load mirrored command snapshot",
            "load mirrored tool snapshot",
            "prepare parity audit hooks",
            "apply trust-gated deferred init",
        };
    }
}

/// <summary>
/// setup-report 命令输出的整体结果。
/// </summary>
public sealed class SetupReport
{
    public WorkspaceSetup Setup { get; }
    public IReadOnlyList<PrefetchResult> Prefetches { get; }
    public DeferredInitResult DeferredInit { get; }
    public bool Trusted { get; }
    public string Cwd { get; }

    public SetupReport(
        WorkspaceSetup setup,
        IReadOnlyList<PrefetchResult> prefetches,
        DeferredInitResult deferredInit,
        bool trusted,
        string cwd)
    {
        Setup = setup;
        Prefetches = prefetches;
        DeferredInit = deferredInit;
        Trusted = trusted;
        Cwd = cwd;
    }

    public string ToMarkdown()
    {
        var lines = new List<string>
        {
            "# Setup Report",
            "",
            $"- .NET: {Setup.DotnetVersion} ({Setup.RuntimeDescription})",
            $"- Platform: {Setup.PlatformName}",
            $"- Trusted mode: {Trusted}",
            $"- CWD: {Cwd}",
            "",
            "Prefetches:",
        };
        lines.AddRange(Prefetches.Select(prefetch => $"- {prefetch.Name}: {prefetch.Detail}"));
        lines.Add("");
        lines.Add("Deferred init:");
        lines.AddRange(DeferredInit.AsLines());
        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// 汇总 C# 工作台需要的 setup 行为。
/// </summary>
public static class SetupService
{
    public static WorkspaceSetup BuildWorkspaceSetup()
    {
        return new WorkspaceSetup(
            DotnetVersion: Environment.Version.ToString(),
            RuntimeDescription: RuntimeInformation.FrameworkDescription,
            PlatformName: RuntimeInformation.OSDescription);
    }

    public static SetupReport Run(string? cwd = null, bool trusted = true)
    {
        var root = cwd ?? WorkspaceEnvironment.Paths.ProjectRoot;
        var prefetches = new[]
        {
            StartMdmRawRead(),
            StartKeychainPrefetch(),
            StartProjectScan(root),
        };

        return new SetupReport(
            setup: BuildWorkspaceSetup(),
            prefetches: prefetches,
            deferredInit: RunDeferredInit(trusted),
            trusted: trusted,
            cwd: root);
    }

    private static PrefetchResult StartMdmRawRead() =>
        new("mdm_raw_read", true, "Simulated MDM raw-read prefetch for workspace bootstrap");

    private static PrefetchResult StartKeychainPrefetch() =>
        new("keychain_prefetch", true, "Simulated keychain prefetch for trusted startup path");

    private static PrefetchResult StartProjectScan(string root) =>
        new("project_scan", true, $"Scanned project root {root}");

    private static DeferredInitResult RunDeferredInit(bool trusted)
    {
        return new DeferredInitResult(
            Trusted: trusted,
            PluginInit: trusted,
            SkillInit: trusted,
            McpPrefetch: trusted,
            SessionHooks: trusted);
    }
}
