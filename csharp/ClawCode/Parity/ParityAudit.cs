using System.Text.Json;
using ClawCode.Context;
using ClawCode.Models;

namespace ClawCode.Parity;

/// <summary>
/// parity audit 的结果。
/// </summary>
public sealed class ParityAuditResult
{
    public bool ArchivePresent { get; init; }
    public (int Current, int Total) RootFileCoverage { get; init; }
    public (int Current, int Total) DirectoryCoverage { get; init; }
    public (int Current, int Total) TotalFileRatio { get; init; }
    public (int Current, int Total) CommandEntryRatio { get; init; }
    public (int Current, int Total) ToolEntryRatio { get; init; }
    public IReadOnlyList<string> MissingRootTargets { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MissingDirectoryTargets { get; init; } = Array.Empty<string>();

    public string ToMarkdown()
    {
        if (!ArchivePresent)
        {
            return string.Join(Environment.NewLine, new[]
            {
                "# Parity Audit",
                "Local archive unavailable; parity audit cannot compare against the original snapshot.",
            });
        }

        var lines = new List<string>
        {
            "# Parity Audit",
            "",
            $"Root file coverage: **{RootFileCoverage.Current}/{RootFileCoverage.Total}**",
            $"Directory coverage: **{DirectoryCoverage.Current}/{DirectoryCoverage.Total}**",
            $"Total Python files vs archived TS-like files: **{TotalFileRatio.Current}/{TotalFileRatio.Total}**",
            $"Command entry coverage: **{CommandEntryRatio.Current}/{CommandEntryRatio.Total}**",
            $"Tool entry coverage: **{ToolEntryRatio.Current}/{ToolEntryRatio.Total}**",
            "",
            "Missing root targets:",
        };

        lines.AddRange(MissingRootTargets.Count > 0 ? MissingRootTargets.Select(item => $"- {item}") : new[] { "- none" });
        lines.Add("");
        lines.Add("Missing directory targets:");
        lines.AddRange(MissingDirectoryTargets.Count > 0 ? MissingDirectoryTargets.Select(item => $"- {item}") : new[] { "- none" });
        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// 读取 reference_data 和 Python 工作台目录，生成同口径的 parity 报告。
/// </summary>
public static class ParityAudit
{
    private static readonly IReadOnlyDictionary<string, string> ArchiveRootFiles = new Dictionary<string, string>
    {
        ["QueryEngine.ts"] = "QueryEngine.py",
        ["Task.ts"] = "task.py",
        ["Tool.ts"] = "Tool.py",
        ["commands.ts"] = "commands.py",
        ["context.ts"] = "context.py",
        ["cost-tracker.ts"] = "cost_tracker.py",
        ["costHook.ts"] = "costHook.py",
        ["dialogLaunchers.tsx"] = "dialogLaunchers.py",
        ["history.ts"] = "history.py",
        ["ink.ts"] = "ink.py",
        ["interactiveHelpers.tsx"] = "interactiveHelpers.py",
        ["main.tsx"] = "main.py",
        ["projectOnboardingState.ts"] = "projectOnboardingState.py",
        ["query.ts"] = "query.py",
        ["replLauncher.tsx"] = "replLauncher.py",
        ["setup.ts"] = "setup.py",
        ["tasks.ts"] = "tasks.py",
        ["tools.ts"] = "tools.py",
    };

    private static readonly IReadOnlyDictionary<string, string> ArchiveDirectoryMappings = new Dictionary<string, string>
    {
        ["assistant"] = "assistant",
        ["bootstrap"] = "bootstrap",
        ["bridge"] = "bridge",
        ["buddy"] = "buddy",
        ["cli"] = "cli",
        ["commands"] = "commands.py",
        ["components"] = "components",
        ["constants"] = "constants",
        ["context"] = "context.py",
        ["coordinator"] = "coordinator",
        ["entrypoints"] = "entrypoints",
        ["hooks"] = "hooks",
        ["ink"] = "ink.py",
        ["keybindings"] = "keybindings",
        ["memdir"] = "memdir",
        ["migrations"] = "migrations",
        ["moreright"] = "moreright",
        ["native-ts"] = "native_ts",
        ["outputStyles"] = "outputStyles",
        ["plugins"] = "plugins",
        ["query"] = "query.py",
        ["remote"] = "remote",
        ["schemas"] = "schemas",
        ["screens"] = "screens",
        ["server"] = "server",
        ["services"] = "services",
        ["skills"] = "skills",
        ["state"] = "state",
        ["tasks"] = "tasks.py",
        ["tools"] = "tools.py",
        ["types"] = "types",
        ["upstreamproxy"] = "upstreamproxy",
        ["utils"] = "utils",
        ["vim"] = "vim",
        ["voice"] = "voice",
    };

    public static ParityAuditResult Run()
    {
        var paths = WorkspaceEnvironment.Paths;
        var currentEntries = Directory.Exists(paths.SourceRoot)
            ? Directory.EnumerateFileSystemEntries(paths.SourceRoot)
                .Select(Path.GetFileName)
                .OfType<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rootHits = ArchiveRootFiles.Values.Where(currentEntries.Contains).ToList();
        var directoryHits = ArchiveDirectoryMappings.Values.Where(currentEntries.Contains).ToList();
        var missingRootTargets = ArchiveRootFiles.Values.Where(target => !currentEntries.Contains(target)).ToList();
        var missingDirectoryTargets = ArchiveDirectoryMappings.Values.Where(target => !currentEntries.Contains(target)).ToList();
        var currentPythonFiles = Directory.Exists(paths.SourceRoot)
            ? Directory.EnumerateFiles(paths.SourceRoot, "*.py", SearchOption.AllDirectories).Count()
            : 0;

        var referenceSurfacePath = Path.Combine(paths.ReferenceDataRoot, "archive_surface_snapshot.json");
        var reference = JsonSerializer.Deserialize<ArchiveSurfaceSnapshot>(File.ReadAllText(referenceSurfacePath))
                        ?? throw new InvalidDataException($"无法解析快照: {referenceSurfacePath}");

        return new ParityAuditResult
        {
            ArchivePresent = Directory.Exists(paths.ArchiveRoot),
            RootFileCoverage = (rootHits.Count, ArchiveRootFiles.Count),
            DirectoryCoverage = (directoryHits.Count, ArchiveDirectoryMappings.Count),
            TotalFileRatio = (currentPythonFiles, reference.TotalTsLikeFiles),
            CommandEntryRatio = (SnapshotCount(Path.Combine(paths.ReferenceDataRoot, "commands_snapshot.json")), reference.CommandEntryCount),
            ToolEntryRatio = (SnapshotCount(Path.Combine(paths.ReferenceDataRoot, "tools_snapshot.json")), reference.ToolEntryCount),
            MissingRootTargets = missingRootTargets.AsReadOnly(),
            MissingDirectoryTargets = missingDirectoryTargets.AsReadOnly(),
        };
    }

    private static int SnapshotCount(string path)
    {
        var entries = JsonSerializer.Deserialize<List<SnapshotEntry>>(File.ReadAllText(path)) ?? new List<SnapshotEntry>();
        return entries.Count;
    }
}
