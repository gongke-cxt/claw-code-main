using System.Text.Json;
using System.Text.Json.Serialization;
using ClawCode.Context;

namespace ClawCode.Session;

/// <summary>
/// 持久化后的轻量会话。
/// </summary>
public sealed class StoredSession
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = "";

    [JsonPropertyName("messages")]
    public List<string> Messages { get; init; } = new();

    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; init; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; init; }
}

/// <summary>
/// 读写 .port_sessions 下的 JSON 会话文件。
/// </summary>
public static class SessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string Save(StoredSession session, string? directory = null)
    {
        var targetDir = directory ?? WorkspaceEnvironment.Paths.SessionRoot;
        Directory.CreateDirectory(targetDir);
        var path = Path.Combine(targetDir, $"{session.SessionId}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(session, JsonOptions));
        return path;
    }

    public static StoredSession Load(string sessionId, string? directory = null)
    {
        var targetDir = directory ?? WorkspaceEnvironment.Paths.SessionRoot;
        var path = Path.Combine(targetDir, $"{sessionId}.json");
        return JsonSerializer.Deserialize<StoredSession>(File.ReadAllText(path))
               ?? throw new InvalidDataException($"无法解析会话文件: {path}");
    }
}
