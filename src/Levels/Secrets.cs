namespace Levels;

/// <summary>
/// Keys in <c>~/.config/levels/secrets.env</c> (spec §12): <c>KEY=VALUE</c> lines, <c>#</c> comments. Values are never printed.
/// </summary>
internal static class Secrets
{
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "levels", "secrets.env");

    /// <summary>The file's keys; empty if it does not exist.</summary>
    public static IReadOnlyDictionary<string, string> Load(string path) =>
        File.Exists(path) ? Parse(File.ReadAllLines(path)) : new Dictionary<string, string>();

    internal static Dictionary<string, string> Parse(IEnumerable<string> lines)
    {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (line.StartsWith("export "))
                line = line["export ".Length..].TrimStart();
            int eq = line.IndexOf('=');
            if (eq <= 0)
                continue;
            var value = line[(eq + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' && value[^1] == '"' || value[0] == '\'' && value[^1] == '\''))
                value = value[1..^1];
            keys[line[..eq].Trim()] = value;
        }
        return keys;
    }

    /// <summary>A warning if group or others can read the file (it should be mode 600), else null.</summary>
    public static string? ModeWarning(string path)
    {
        if (!File.Exists(path) || OperatingSystem.IsWindows())
            return null;
        var mode = File.GetUnixFileMode(path);
        const UnixFileMode open = UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead | UnixFileMode.OtherWrite;
        return (mode & open) != 0 ? $"{path} can be read by other users; run: chmod 600 {path}" : null;
    }
}
