using System.Reflection;

namespace ClawCode.Context;

/// <summary>
/// 统一管理仓库根目录和运行时需要用到的关键路径。
/// </summary>
public sealed record WorkspacePaths(
    string ProjectRoot,
    string SourceRoot,
    string TestsRoot,
    string AssetsRoot,
    string ArchiveRoot,
    string ReferenceDataRoot,
    string SessionRoot);

/// <summary>
/// 从当前工作目录和程序集目录反推仓库根路径。
/// </summary>
public static class WorkspaceEnvironment
{
    private static readonly Lazy<WorkspacePaths> LazyPaths = new(Locate);

    public static WorkspacePaths Paths => LazyPaths.Value;

    private static WorkspacePaths Locate()
    {
        var candidates = new List<string>();
        candidates.AddRange(EnumerateCandidateRoots(Environment.CurrentDirectory));
        candidates.AddRange(EnumerateCandidateRoots(AppContext.BaseDirectory));

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrWhiteSpace(assemblyLocation))
        {
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrWhiteSpace(assemblyDirectory))
            {
                candidates.AddRange(EnumerateCandidateRoots(assemblyDirectory));
            }
        }

        var projectRoot = candidates
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(LooksLikeProjectRoot);

        if (projectRoot is null)
        {
            throw new DirectoryNotFoundException("无法定位 claw-code-main 仓库根目录。");
        }

        var referenceDataFromOutput = Path.Combine(AppContext.BaseDirectory, "ReferenceData");
        var referenceDataRoot = Directory.Exists(referenceDataFromOutput)
            ? referenceDataFromOutput
            : Path.Combine(projectRoot, "src", "reference_data");

        return new WorkspacePaths(
            ProjectRoot: projectRoot,
            SourceRoot: Path.Combine(projectRoot, "src"),
            TestsRoot: Path.Combine(projectRoot, "tests"),
            AssetsRoot: Path.Combine(projectRoot, "assets"),
            ArchiveRoot: Path.Combine(projectRoot, "archive", "claw_code_ts_snapshot", "src"),
            ReferenceDataRoot: referenceDataRoot,
            SessionRoot: Path.Combine(projectRoot, ".port_sessions"));
    }

    private static IEnumerable<string> EnumerateCandidateRoots(string startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
        {
            yield break;
        }

        var current = new DirectoryInfo(Path.GetFullPath(startPath));
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private static bool LooksLikeProjectRoot(string root)
    {
        return Directory.Exists(Path.Combine(root, "src")) &&
               Directory.Exists(Path.Combine(root, "csharp")) &&
               File.Exists(Path.Combine(root, "README.md"));
    }
}
