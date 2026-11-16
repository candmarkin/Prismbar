using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManagedShell.Common.Helpers;
using Prismbar.Core.Calendar;
using Prismbar.Core.Events;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

public sealed partial class ClockWidget : WidgetBase
{
    private readonly DispatcherTimer _timer;
    private IDisposable? _calendarSub;
    private DateTime _shownMonth;

    public ClockWidget(WidgetContext ctx) : base(ctx)
    {
        _timer = new DispatcherTimer(DispatcherPriority.Normal, ctx.Dispatcher);
        _timer.Tick += (_, _) => Tick();
    }

    public override string Id => WidgetIds.Clock;

    public sealed record DayCell(int Day, bool InMonth, bool IsToday);

    [ObservableProperty] private string timeText = "";
    [ObservableProperty] private string dateText = "";
    [ObservableProperty] private string monthTitle = "";

    public ObservableCollection<string> WeekdayNames { get; } = [];
    public ObservableCollection<DayCell> MonthDays { get; } = [];
    public ObservableCollection<CalendarItem> UpcomingEvents { get; } = [];
    public bool HasEvents => UpcomingEvents.Count > 0;

    public override void OnStart()
    {
        Tick();
        _calendarSub = Ctx.Bus.Subscribe<CalendarUpdated>(_ => Ctx.Dispatcher.InvokeAsync(RefreshEvents));
    }

    public override void OnStop()
    {
        _timer.Stop();
        _calendarSub?.Dispose();
        _calendarSub = null;
    }

    private void Tick()
    {
        var now = Ctx.Clock.Now.LocalDateTime;
        TimeText = now.ToString("t");
        DateText = now.ToString("ddd d");
        ToolTipText = now.ToString("D");
        // Fire just after the next minute boundary; re-aligning every tick avoids drift.
        _timer.Interval = TimeSpan.FromSeconds(60 - now.Second) - TimeSpan.FromMilliseconds(now.Millisecond) + TimeSpan.FromMilliseconds(50);
        _timer.Start();
    }

    public override void OnPopoverOpened()
    {
        BuildMonth(Ctx.Clock.Now.LocalDateTime.Date);
        RefreshEvents();
    }

    private void RefreshEvents()
    {
        UpcomingEvents.Clear();
        foreach (var e in Ctx.Calendar.Upcoming(3)) UpcomingEvents.Add(e);
        OnPropertyChanged(nameof(HasEvents));
    }

    [RelayCommand]
    private void PreviousMonth() => BuildMonth(_shownMonth.AddMonths(-1));

    [RelayCommand]
    private void NextMonth() => BuildMonth(_shownMonth.AddMonths(1));

    private void BuildMonth(DateTime anyDay)
    {
        var culture = CultureInfo.CurrentCulture;
        var first = new DateTime(anyDay.Year, anyDay.Month, 1);
        _shownMonth = first;
        var title = first.ToString("Y", culture);
        MonthTitle = char.ToUpper(title[0], culture) + title[1..];

        var firstDow = culture.DateTimeFormat.FirstDayOfWeek;
        WeekdayNames.Clear();
        for (int i = 0; i < 7; i++)
            WeekdayNames.Add(culture.DateTimeFormat.GetShortestDayName((DayOfWeek)(((int)firstDow + i) % 7)));

        MonthDays.Clear();
        int offset = ((int)first.DayOfWeek - (int)firstDow + 7) % 7;
        var start = first.AddDays(-offset);
        var today = Ctx.Clock.Now.LocalDateTime.Date;
        for (int i = 0; i < 42; i++)
        {
            var d = start.AddDays(i);
            MonthDays.Add(new DayCell(d.Day, d.Month == first.Month, d == today));
        }
    }

    [RelayCommand]
    private void OpenDateTimeSettings()
    {
        Ctx.Popovers.Close();
        ShellHelper.StartProcess("ms-settings:dateandtime");
    }

    public override void Dispose() => OnStop();
}
