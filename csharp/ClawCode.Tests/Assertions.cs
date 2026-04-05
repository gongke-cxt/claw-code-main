namespace ClawCode.Tests;

/// <summary>
/// 极简断言工具，避免引入额外测试框架包。
/// </summary>
public static class Assertions
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void False(bool condition, string message)
    {
        True(!condition, message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} expected={expected} actual={actual}");
        }
    }

    public static void Contains(string expectedSubstring, string actual, string message)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{message} missing='{expectedSubstring}'");
        }
    }

    public static void ContainsIgnoreCase(string expectedSubstring, string actual, string message)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{message} missing='{expectedSubstring}'");
        }
    }

    public static void FileExists(string path, string message)
    {
        True(File.Exists(path), $"{message} path={path}");
    }
}
