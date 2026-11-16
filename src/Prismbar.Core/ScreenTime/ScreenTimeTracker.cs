using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prismbar.Core.ScreenTime;

public sealed class AppUsage
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("path")] public string? Path { get; set; }
    [JsonPropertyName("seconds")] public double Seconds { get; set; }
}

public sealed class DaySnapshot
{
    [JsonPropertyName("apps")] public Dictionary<string, AppUsage> Apps { get; set; } = [];
    [JsonIgnore] public double TotalSeconds => Apps.Values.Sum(a => a.Seconds);
}

/// <summary>Credits foreground time to the active app, split at local midnight, ignoring away periods.</summary>
public sealed class ScreenTimeTracker
{
    private readonly Dictionary<DateOnly, DaySnapshot> _days = [];
    private string? _key, _name, _path;
    private DateTimeOffset _since;
    private bool _away;

    public IEnumerable<DateOnly> LoadedDays => _days.Keys;

    public void Seed(DateOnly day, DaySnapshot snapshot) => _days[day] = snapshot;

    public void Activate(string key, string name, string? path, DateTimeOffset now)
    {
        Credit(now);
        (_key, _name, _path) = (key, name, path);
        _since = now;
    }

    public void SetAway(bool away, DateTimeOffset at)
    {
        if (away == _away) return;
        if (away) Credit(at);
        _away = away;
        _since = at;
    }

    public void Flush(DateTimeOffset now)
    {
        Credit(now);
        _since = now;
    }

    public DaySnapshot Get(DateOnly day) => _days.TryGetValue(day, out var s) ? s : new DaySnapshot();

    private void Credit(DateTimeOffset now)
    {
        if (_key is null || _away || now <= _since) { _since = now; return; }
        var start = _since.ToLocalTime();
        var end = now.ToLocalTime();
        while (start < end)
        {
            var nextMidnight = new DateTimeOffset(start.Date.AddDays(1), start.Offset);
            var segmentEnd = nextMidnight < end ? nextMidnight : end;
            var day = DateOnly.FromDateTime(start.DateTime);
            if (!_days.TryGetValue(day, out var snap)) _days[day] = snap = new DaySnapshot();
            if (!snap.Apps.TryGetValue(_key, out var usage)) snap.Apps[_key] = usage = new AppUsage();
            usage.Name = _name ?? _key;
            usage.Path = _path;
            usage.Seconds += (segmentEnd - start).TotalSeconds;
            start = segmentEnd;
        }
        _since = now;
    }
}

public sealed class ScreenTimeStore(string dir)
{
    private string FileFor(DateOnly day) => Path.Combine(dir, $"{day:yyyy-MM-dd}.json");

    public DaySnapshot Load(DateOnly day)
    {
        try
        {
            var f = FileFor(day);
            return File.Exists(f) ? JsonSerializer.Deserialize<DaySnapshot>(File.ReadAllText(f)) ?? new() : new();
        }
        catch (JsonException) { return new(); }
    }

    public void Save(DateOnly day, DaySnapshot snapshot)
    {
        Directory.CreateDirectory(dir);
        var f = FileFor(day);
        File.WriteAllText(f + ".tmp", JsonSerializer.Serialize(snapshot));
        File.Move(f + ".tmp", f, overwrite: true);
    }

    public void Prune(DateOnly today)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var f in Directory.GetFiles(dir, "*.json"))
            if (DateOnly.TryParseExact(Path.GetFileNameWithoutExtension(f), "yyyy-MM-dd", out var d) && d < today.AddDays(-30))
                File.Delete(f);
    }
}
