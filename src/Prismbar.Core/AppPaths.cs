namespace Prismbar.Core;

/// <summary>Locations of all per-user Prismbar data (kept apart from the Velopack install dir).</summary>
public static class AppPaths
{
    public static string Root { get; private set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Prismbar");

    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string SessionFile => Path.Combine(Root, "session.json");
    public static string LogsDir => Path.Combine(Root, "logs");
    public static string CacheDir => Path.Combine(Root, "cache");
    public static string ScreenTimeDir => Path.Combine(Root, "screentime");
    public static string PinsDir => Path.Combine(Root, "pins");
    public static string TodosFile => Path.Combine(Root, "todos.json");
    public static string NotesFile => Path.Combine(Root, "notes.txt");

    public static void EnsureCreated()
    {
        foreach (var dir in new[] { Root, LogsDir, CacheDir, ScreenTimeDir, PinsDir })
            Directory.CreateDirectory(dir);
    }

    /// <summary>Redirects all paths (tests).</summary>
    public static void UseRoot(string root) => Root = root;
}
