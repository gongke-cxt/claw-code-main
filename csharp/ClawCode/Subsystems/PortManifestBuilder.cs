using ClawCode.Context;
using ClawCode.Models;

namespace ClawCode.Subsystems;

/// <summary>
/// C# 实现读取同一份 Python 工作台源码树后生成的清单。
/// </summary>
public sealed class PortManifest
{
    public string SrcRoot { get; }
    public int TotalPythonFiles { get; }
    public IReadOnlyList<Subsystem> TopLevelModules { get; }

    public PortManifest(string srcRoot, int totalPythonFiles, IReadOnlyList<Subsystem> topLevelModules)
    {
        SrcRoot = srcRoot;
        TotalPythonFiles = totalPythonFiles;
        TopLevelModules = topLevelModules;
    }

    public string ToMarkdown()
    {
        var lines = new List<string>
        {
            $"Port root: `{SrcRoot}`",
            $"Total Python files: **{TotalPythonFiles}**",
            "",
            "Top-level Python modules:",
        };

        lines.AddRange(TopLevelModules.Select(module =>
            $"- `{module.Name}` ({module.FileCount} files) - {module.Notes}"));

        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// 统计仓库 src/ 下的 Python 文件和顶层模块分布。
/// </summary>
public static class PortManifestBuilder
{
    private static readonly IReadOnlyDictionary<string, string> Notes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["__init__.py"] = "package export surface",
        ["main.py"] = "CLI entrypoint",
        ["port_manifest.py"] = "workspace manifest generation",
        ["query_engine.py"] = "port orchestration summary layer",
        ["commands.py"] = "command backlog metadata",
        ["tools.py"] = "tool backlog metadata",
        ["models.py"] = "shared dataclasses",
        ["task.py"] = "task-level planning structures",
    };

    public static PortManifest Build(WorkspacePaths? paths = null)
    {
        var resolved = paths ?? WorkspaceEnvironment.Paths;
        var files = Directory.Exists(resolved.SourceRoot)
            ? Directory.EnumerateFiles(resolved.SourceRoot, "*.py", SearchOption.AllDirectories).ToList()
            : new List<string>();

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(resolved.SourceRoot, file).Replace('\\', '/');
            var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            var key = parts[0];
            counts[key] = counts.TryGetValue(key, out var current) ? current + 1 : 1;
        }

        var modules = counts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new Subsystem(
                Name: pair.Key,
                Path: $"src/{pair.Key}",
                FileCount: pair.Value,
                Notes: Notes.TryGetValue(pair.Key, out var note) ? note : "Python port support module"))
            .ToList()
            .AsReadOnly();

        return new PortManifest(resolved.SourceRoot, files.Count, modules);
    }
}
