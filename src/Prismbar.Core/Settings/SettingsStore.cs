using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prismbar.Core.Settings;

/// <summary>Loads, normalizes and persists <see cref="AppSettings"/> with debounced atomic writes.</summary>
public sealed class SettingsStore : IDisposable
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Action<string>? _log;
    private readonly object _gate = new();
    private readonly Timer _timer;
    private string? _pendingJson;

    public SettingsStore(Action<string>? log = null)
    {
        _log = log;
        _timer = new Timer(_ => Flush(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public AppSettings Current { get; private set; } = new();

    public event Action<string>? Changed;

    public AppSettings Load()
    {
        var path = AppPaths.SettingsFile;
        AppSettings settings;
        if (!File.Exists(path))
        {
            settings = new AppSettings();
        }
        else
        {
            try
            {
                settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions) ?? new AppSettings();
            }
            catch (JsonException ex)
            {
                var backup = Path.Combine(Path.GetDirectoryName(path)!, $"settings.corrupt-{DateTime.Now:yyyyMMddHHmmss}.json");
                File.Move(path, backup, overwrite: true);
                _log?.Invoke($"settings.json is invalid ({ex.Message}); moved to {backup} and using defaults");
                settings = new AppSettings();
            }
        }
        Normalize(settings);
        Current = settings;
        return settings;
    }

    public static void Normalize(AppSettings s)
    {
        s.Widgets ??= [];
        var seen = new HashSet<string>();
        s.Widgets = s.Widgets.Where(w => w is not null && WidgetIds.All.Contains(w.Id) && seen.Add(w.Id)).ToList();
        foreach (var id in WidgetIds.All.Where(id => !seen.Contains(id)))
            s.Widgets.Add(new WidgetPlacement { Id = id, Zone = DockZone.Right, Order = int.MaxValue, Enabled = false });

        foreach (var w in s.Widgets)
        {
            if (w.Id == WidgetIds.Apps) { w.Zone = DockZone.Center; w.Enabled = true; }
            else if (w.Zone == DockZone.Center) w.Zone = DockZone.Right;
        }

        // Stable renumbering per zone keeps relative order (ties resolved by list position).
        var renumbered = new List<WidgetPlacement>();
        foreach (var zone in Enum.GetValues<DockZone>())
        {
            int i = 0;
            foreach (var w in s.Widgets.Select((w, idx) => (w, idx)).Where(t => t.w.Zone == zone)
                         .OrderBy(t => t.w.Order).ThenBy(t => t.idx).Select(t => t.w))
            {
                w.Order = i++;
                renumbered.Add(w);
            }
        }
        s.Widgets = renumbered;

        int[] sizes = [36, 44, 52];
        s.IconSize = sizes.OrderBy(v => Math.Abs(v - s.IconSize)).ThenByDescending(v => v == 44).First();
        s.GlassMaxFps = s.GlassMaxFps < 45 ? 30 : 60;
        s.Automations ??= new();
        s.Automations.MeetingReminderMinutes = Math.Clamp(s.Automations.MeetingReminderMinutes, 0, 30);
        s.PinnedApps ??= [];
        s.PinnedTrayIcons ??= [];
        s.QuietHours ??= new();
        s.Calendar ??= new();
        s.Calendar.Sources ??= [];
        s.Weather ??= new();
        s.Pomodoro ??= new();
        s.Music ??= new();
        s.GitHub ??= new();
        if (string.IsNullOrWhiteSpace(s.Language)) s.Language = "auto";
    }

    /// <summary>Mutates the current settings, schedules a write in 300 ms and raises <see cref="Changed"/>.</summary>
    public void Update(Action<AppSettings> mutate, string propertyName)
    {
        mutate(Current);
        var json = JsonSerializer.Serialize(Current, JsonOptions);
        lock (_gate)
        {
            _pendingJson = json;
            _timer.Change(300, Timeout.Infinite);
        }
        Changed?.Invoke(propertyName);
    }

    /// <summary>Replaces the whole settings object (e.g. "restore defaults").</summary>
    public void Replace(AppSettings settings)
    {
        Normalize(settings);
        Current = settings;
        Update(_ => { }, "*");
    }

    public void Flush()
    {
        string? json;
        lock (_gate)
        {
            json = _pendingJson;
            _pendingJson = null;
        }
        if (json is null) return;
        try
        {
            var path = AppPaths.SettingsFile;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            _log?.Invoke($"Failed to save settings: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Flush();
        _timer.Dispose();
    }
}
