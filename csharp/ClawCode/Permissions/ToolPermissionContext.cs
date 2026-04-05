namespace ClawCode.Permissions;

/// <summary>
/// 用于在查询时按名称或前缀屏蔽工具。
/// </summary>
public sealed class ToolPermissionContext
{
    private readonly HashSet<string> _denyNames;
    private readonly List<string> _denyPrefixes;

    private ToolPermissionContext(HashSet<string> denyNames, List<string> denyPrefixes)
    {
        _denyNames = denyNames;
        _denyPrefixes = denyPrefixes;
    }

    public static ToolPermissionContext FromCollections(
        IEnumerable<string>? denyNames = null,
        IEnumerable<string>? denyPrefixes = null)
    {
        return new ToolPermissionContext(
            new HashSet<string>(
                (denyNames ?? Array.Empty<string>())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.ToLowerInvariant()),
                StringComparer.Ordinal),
            (denyPrefixes ?? Array.Empty<string>())
                .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
                .Select(prefix => prefix.ToLowerInvariant())
                .ToList());
    }

    public bool Blocks(string toolName)
    {
        var lowered = toolName.ToLowerInvariant();
        return _denyNames.Contains(lowered) || _denyPrefixes.Any(lowered.StartsWith);
    }
}
