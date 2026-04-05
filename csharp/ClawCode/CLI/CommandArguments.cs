namespace ClawCode.CLI;

/// <summary>
/// 轻量命令行解析器，只覆盖当前工作台需要的参数形态。
/// </summary>
public sealed class CommandArguments
{
    private readonly Dictionary<string, List<string>> _options;

    public string Command { get; }
    public IReadOnlyList<string> Positionals { get; }

    private CommandArguments(string command, IReadOnlyList<string> positionals, Dictionary<string, List<string>> options)
    {
        Command = command;
        Positionals = positionals;
        _options = options;
    }

    public static CommandArguments Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new CommandArguments("", Array.Empty<string>(), new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase));
        }

        var positionals = new List<string>();
        var options = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < args.Length; index++)
        {
            var token = args[index];
            if (token.StartsWith("--", StringComparison.Ordinal))
            {
                var name = token[2..];
                if (!options.TryGetValue(name, out var values))
                {
                    values = new List<string>();
                    options[name] = values;
                }

                var hasValue = index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal);
                values.Add(hasValue ? args[++index] : "");
                continue;
            }

            positionals.Add(token);
        }

        return new CommandArguments(args[0], positionals.AsReadOnly(), options);
    }

    public bool HasFlag(string name)
    {
        return _options.ContainsKey(name);
    }

    public string? GetOption(string name)
    {
        return GetOptions(name).FirstOrDefault();
    }

    public IReadOnlyList<string> GetOptions(string name)
    {
        return _options.TryGetValue(name, out var values)
            ? values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList().AsReadOnly()
            : Array.Empty<string>();
    }

    public int GetIntOption(string name, int defaultValue)
    {
        var raw = GetOption(name);
        return int.TryParse(raw, out var value) ? value : defaultValue;
    }

    public string JoinPositionals(int skip = 0)
    {
        return string.Join(" ", Positionals.Skip(skip));
    }
}
