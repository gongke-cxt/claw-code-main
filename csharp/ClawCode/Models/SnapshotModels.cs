using System.Text.Json.Serialization;

namespace ClawCode.Models;

/// <summary>
/// 命令和工具快照的 JSON 反序列化模型。
/// </summary>
public sealed class SnapshotEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("source_hint")] public string SourceHint { get; set; } = "";
    [JsonPropertyName("responsibility")] public string Responsibility { get; set; } = "";
}

/// <summary>
/// 归档表面快照，用于 parity audit。
/// </summary>
public sealed class ArchiveSurfaceSnapshot
{
    [JsonPropertyName("archive_root")] public string ArchiveRoot { get; set; } = "";
    [JsonPropertyName("root_files")] public List<string> RootFiles { get; set; } = new();
    [JsonPropertyName("root_dirs")] public List<string> RootDirs { get; set; } = new();
    [JsonPropertyName("total_ts_like_files")] public int TotalTsLikeFiles { get; set; }
    [JsonPropertyName("command_entry_count")] public int CommandEntryCount { get; set; }
    [JsonPropertyName("tool_entry_count")] public int ToolEntryCount { get; set; }
}

/// <summary>
/// 子系统快照模型，目前主要用于保留后续扩展入口。
/// </summary>
public sealed class SubsystemSnapshot
{
    [JsonPropertyName("archive_name")] public string ArchiveName { get; set; } = "";
    [JsonPropertyName("package_name")] public string PackageName { get; set; } = "";
    [JsonPropertyName("module_count")] public int ModuleCount { get; set; }
    [JsonPropertyName("sample_files")] public List<string> SampleFiles { get; set; } = new();
}
