using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

/// <summary>Every widget the dock knows, in <see cref="WidgetIds.All"/> order.</summary>
public static class WidgetRegistry
{
    public static readonly IReadOnlyList<WidgetDescriptor> All =
    [
        new(WidgetIds.Start, c => new StartWidget(c), () => new StartTile(), null),
        new(WidgetIds.Search, c => new SearchWidget(c), () => new SearchTile(), () => new SearchPopover()),
        new(WidgetIds.Weather, c => new WeatherWidget(c), () => new WeatherTile(), () => new WeatherPopover()),
        new(WidgetIds.Apps, c => new AppsWidget(c), () => new AppsTile(), null, IsBareTile: true),
        new(WidgetIds.Music, c => new MusicWidget(c), () => new MusicTile(), () => new MusicPopover()),
        new(WidgetIds.Calendar, c => new CalendarWidget(c), () => new CalendarTile(), () => new CalendarPopover()),
        new(WidgetIds.Cpu, c => new CpuWidget(c), () => new CpuTile(), () => new CpuPopover()),
        new(WidgetIds.Ram, c => new RamWidget(c), () => new RamTile(), () => new RamPopover()),
        new(WidgetIds.Network, c => new NetworkWidget(c), () => new NetworkTile(), () => new NetworkPopover()),
        new(WidgetIds.Volume, c => new VolumeWidget(c), () => new VolumeTile(), () => new VolumePopover()),
        new(WidgetIds.Mic, c => new MicWidget(c), () => new MicTile(), () => new MicPopover()),
        new(WidgetIds.Battery, c => new BatteryWidget(c), () => new BatteryTile(), () => new BatteryPopover()),
        new(WidgetIds.Tray, c => new TrayWidget(c), () => new TrayTile(), () => new TrayPopover()),
        new(WidgetIds.Clock, c => new ClockWidget(c), () => new ClockTile(), () => new ClockPopover()),
        new(WidgetIds.Pomodoro, c => new PomodoroWidget(c), () => new PomodoroTile(), () => new PomodoroPopover()),
        new(WidgetIds.Timer, c => new TimerWidget(c), () => new TimerTile(), () => new TimerPopover()),
        new(WidgetIds.Todos, c => new TodosWidget(c), () => new TodosTile(), () => new TodosPopover()),
        new(WidgetIds.Notes, c => new NotesWidget(c), () => new NotesTile(), () => new NotesPopover()),
        new(WidgetIds.ScreenTime, c => new ScreenTimeWidget(c), () => new ScreenTimeTile(), () => new ScreenTimePopover()),
        new(WidgetIds.Downloads, c => new DownloadsWidget(c), () => new DownloadsTile(), () => new DownloadsPopover()),
        new(WidgetIds.RecycleBin, c => new RecycleBinWidget(c), () => new RecycleBinTile(), () => new RecycleBinPopover()),
        new(WidgetIds.GitHub, c => new GitHubWidget(c), () => new GitHubTile(), () => new GitHubPopover()),
    ];
}
