namespace Prismbar.Core.Settings;

public sealed class AppSettings
{
    public int Version { get; set; } = 1;
    public DockStyle Style { get; set; } = DockStyle.Glass;
    public DockShape Shape { get; set; } = DockShape.Pill;
    public DockEdge Edge { get; set; } = DockEdge.Bottom;
    public bool AutoHide { get; set; }
    public int IconSize { get; set; } = 44;
    public bool Magnification { get; set; } = true;
    public GlassSource GlassSource { get; set; } = GlassSource.Live;
    public int GlassMaxFps { get; set; } = 60;
    public bool ShowOnAllMonitors { get; set; }
    public TaskbarMode WindowsTaskbar { get; set; } = TaskbarMode.Hidden;
    public bool StartWithWindows { get; set; } = true;
    public bool AutoUpdate { get; set; } = true;
    public string Language { get; set; } = "auto";
    public bool DebugLogging { get; set; }
    public List<WidgetPlacement> Widgets { get; set; } = WidgetIds.DefaultLayout();
    public List<PinnedApp> PinnedApps { get; set; } = [];
    public bool PinnedAppsImported { get; set; }
    public List<string> PinnedTrayIcons { get; set; } = [];
    public AutomationSettings Automations { get; set; } = new();
    public QuietHoursSettings QuietHours { get; set; } = new();
    public CalendarSettings Calendar { get; set; } = new();
    public WeatherSettings Weather { get; set; } = new();
    public PomodoroSettings Pomodoro { get; set; } = new();
    public MusicSettings Music { get; set; } = new();
    public GitHubSettings GitHub { get; set; } = new();
}

public sealed class WidgetPlacement
{
    public string Id { get; set; } = "";
    public DockZone Zone { get; set; }
    public int Order { get; set; }
    public bool Enabled { get; set; }
}

public sealed class PinnedApp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PinKind Kind { get; set; }
    public string? LnkPath { get; set; }
    public string? Aumid { get; set; }
    public string DisplayName { get; set; } = "";
    public string MatchKey { get; set; } = "";
}

public sealed class AutomationSettings
{
    public bool PauseMusicOnMeeting { get; set; } = true;
    public bool ResumeMusicAfterMeeting { get; set; } = true;
    public int MeetingReminderMinutes { get; set; } = 2;
    public bool QuietDuringPomodoroFocus { get; set; }
    public bool BatteryLowAlert { get; set; } = true;
    public int BatteryLowPercent { get; set; } = 20;
    public bool MemoryPressureAlert { get; set; } = true;
    public int MemoryPressurePercent { get; set; } = 90;
}

public sealed class QuietHoursSettings
{
    public bool Enabled { get; set; }
    public TimeOnly Start { get; set; } = new(22, 0);
    public TimeOnly End { get; set; } = new(7, 0);
}

public sealed class CalendarSettings
{
    public static readonly string[] Palette = ["#0A84FF", "#30D158", "#FF9F0A", "#FF453A", "#BF5AF2", "#64D2FF", "#FFD60A", "#FF375F"];
    public List<CalendarSource> Sources { get; set; } = [];
}

public sealed class CalendarSource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string ColorHex { get; set; } = "#0A84FF";
    public string UrlProtected { get; set; } = "";
}

public sealed class WeatherSettings
{
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public TemperatureUnit Unit { get; set; } = TemperatureUnit.Celsius;
}

public sealed class PomodoroSettings
{
    public int FocusMinutes { get; set; } = 25;
    public int ShortBreakMinutes { get; set; } = 5;
    public int LongBreakMinutes { get; set; } = 15;
    public int LongBreakEvery { get; set; } = 4;
}

public sealed class MusicSettings
{
    public bool HideWhenIdle { get; set; } = true;
}

public sealed class GitHubSettings
{
    public string? TokenProtected { get; set; }
    public string? Login { get; set; }
}
