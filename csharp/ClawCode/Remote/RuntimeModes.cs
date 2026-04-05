namespace ClawCode.Remote;

/// <summary>
/// 远程模式报告。
/// </summary>
public sealed record RuntimeModeReport(string Mode, bool Connected, string Detail)
{
    public string AsText()
    {
        return $"mode={Mode}{Environment.NewLine}connected={Connected}{Environment.NewLine}detail={Detail}";
    }
}

/// <summary>
/// 直连类模式报告。
/// </summary>
public sealed record DirectModeReport(string Mode, string Target, bool Active)
{
    public string AsText()
    {
        return $"mode={Mode}{Environment.NewLine}target={Target}{Environment.NewLine}active={Active}";
    }
}

/// <summary>
/// 各种模式的占位执行结果。
/// </summary>
public static class RuntimeModes
{
    public static RuntimeModeReport RunRemoteMode(string target) =>
        new("remote", true, $"Remote control placeholder prepared for {target}");

    public static RuntimeModeReport RunSshMode(string target) =>
        new("ssh", true, $"SSH proxy placeholder prepared for {target}");

    public static RuntimeModeReport RunTeleportMode(string target) =>
        new("teleport", true, $"Teleport resume/create placeholder prepared for {target}");

    public static DirectModeReport RunDirectConnect(string target) =>
        new("direct-connect", target, true);

    public static DirectModeReport RunDeepLink(string target) =>
        new("deep-link", target, true);
}
