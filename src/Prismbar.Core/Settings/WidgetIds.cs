namespace Prismbar.Core.Settings;

public static class WidgetIds
{
    public const string Start = "start";
    public const string Search = "search";
    public const string Weather = "weather";
    public const string Apps = "apps";
    public const string Music = "music";
    public const string Calendar = "calendar";
    public const string Cpu = "cpu";
    public const string Ram = "ram";
    public const string Network = "network";
    public const string Volume = "volume";
    public const string Mic = "mic";
    public const string Battery = "battery";
    public const string Tray = "tray";
    public const string Clock = "clock";
    public const string Pomodoro = "pomodoro";
    public const string Timer = "timer";
    public const string Todos = "todos";
    public const string Notes = "notes";
    public const string ScreenTime = "screentime";
    public const string Downloads = "downloads";
    public const string RecycleBin = "recyclebin";
    public const string GitHub = "github";

    public static readonly IReadOnlyList<string> All =
    [
        Start, Search, Weather, Apps, Music, Calendar, Cpu, Ram, Network, Volume, Mic, Battery, Tray, Clock,
        Pomodoro, Timer, Todos, Notes, ScreenTime, Downloads, RecycleBin, GitHub,
    ];

    private static readonly HashSet<string> OffByDefault = [Pomodoro, Timer, Todos, Notes, ScreenTime, Downloads, RecycleBin, GitHub];

    public static List<WidgetPlacement> DefaultLayout()
    {
        var list = new List<WidgetPlacement>
        {
            new() { Id = Start, Zone = DockZone.Left, Order = 0, Enabled = true },
            new() { Id = Search, Zone = DockZone.Left, Order = 1, Enabled = true },
            new() { Id = Weather, Zone = DockZone.Left, Order = 2, Enabled = true },
            new() { Id = Apps, Zone = DockZone.Center, Order = 0, Enabled = true },
        };
        int order = 0;
        foreach (var id in All.Skip(4))
            list.Add(new WidgetPlacement { Id = id, Zone = DockZone.Right, Order = order++, Enabled = !OffByDefault.Contains(id) });
        return list;
    }
}
