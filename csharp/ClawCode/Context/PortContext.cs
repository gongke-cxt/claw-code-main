namespace ClawCode.Context;

/// <summary>
/// 对当前仓库的基础观测结果。
/// </summary>
public sealed record PortContext(
    string SourceRoot,
    string TestsRoot,
    string AssetsRoot,
    string ArchiveRoot,
    int PythonFileCount,
    int TestFileCount,
    int AssetFileCount,
    bool ArchiveAvailable);

/// <summary>
/// 构建和渲染仓库上下文。
/// </summary>
public static class PortContextBuilder
{
    public static PortContext Build(WorkspacePaths? paths = null)
    {
        var resolved = paths ?? WorkspaceEnvironment.Paths;
        return new PortContext(
            SourceRoot: resolved.SourceRoot,
            TestsRoot: resolved.TestsRoot,
            AssetsRoot: resolved.AssetsRoot,
            ArchiveRoot: resolved.ArchiveRoot,
            PythonFileCount: CountFiles(resolved.SourceRoot, "*.py"),
            TestFileCount: CountFiles(resolved.TestsRoot, "*.py"),
            AssetFileCount: CountFiles(resolved.AssetsRoot, "*"),
            ArchiveAvailable: Directory.Exists(resolved.ArchiveRoot));
    }

    public static string Render(PortContext context)
    {
        return string.Join(Environment.NewLine, new[]
        {
            $"Source root: {context.SourceRoot}",
            $"Test root: {context.TestsRoot}",
            $"Assets root: {context.AssetsRoot}",
            $"Archive root: {context.ArchiveRoot}",
            $"Python files: {context.PythonFileCount}",
            $"Test files: {context.TestFileCount}",
            $"Assets: {context.AssetFileCount}",
            $"Archive available: {context.ArchiveAvailable}",
        });
    }

    private static int CountFiles(string root, string pattern)
    {
        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories).Count()
            : 0;
    }
}
