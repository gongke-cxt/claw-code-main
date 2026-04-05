using System.Diagnostics;
using ClawCode.Context;

namespace ClawCode.Tests;

/// <summary>
/// 统一管理测试临时产物和 CLI 子进程调用。
/// </summary>
public sealed class TestEnvironment : IDisposable
{
    private readonly List<string> _createdFiles = new();
    private readonly List<string> _createdDirectories = new();

    public string RepoRoot => WorkspaceEnvironment.Paths.ProjectRoot;

    public string CreateTempDirectory(string name)
    {
        var path = Path.Combine(RepoRoot, name);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            _createdDirectories.Add(path);
        }

        return path;
    }

    public void TrackFile(string path)
    {
        _createdFiles.Add(path);
    }

    public string RunCli(params string[] args)
    {
        var assemblyPath = typeof(ClawCode.Commands.CommandManager).Assembly.Location;
        var dotnetHome = CreateTempDirectory(".dotnet-home-tests");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.Environment["DOTNET_CLI_HOME"] = dotnetHome;
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        startInfo.ArgumentList.Add(assemblyPath);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动 dotnet CLI 进程。");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"CLI 运行失败。args={string.Join(" ", args)} stderr={stderr}");
        }

        return stdout;
    }

    public void Dispose()
    {
        foreach (var file in _createdFiles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        foreach (var directory in _createdDirectories
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderByDescending(path => path.Length))
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
