using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManagedShell;
using ManagedShell.Common.Logging;
using Prismbar.Alerts;
using Prismbar.Core.Events;
using Prismbar.Core.Security;
using Prismbar.Core.Settings;
using Prismbar.Core.Time;
using Prismbar.Localization;
using Prismbar.Popovers;
using Prismbar.Services;

namespace Prismbar.Widgets;

/// <summary>Everything a widget may depend on. Created once by <see cref="App"/>.</summary>
public sealed record WidgetContext(
    SettingsStore Store,
    EventBus Bus,
    IClock Clock,
    ShellManager Shell,
    PopoverService Popovers,
    AlertService Alerts,
    HttpClient Http,
    ISecretProtector Secrets,
    Dispatcher Dispatcher,
    MediaService Media,
    AudioService Audio,
    CalendarService Calendar,
    ScreenTimeService ScreenTime)
{
    public AppSettings Settings => Store.Current;
}

/// <summary>
/// Base view-model for a dock widget. The tile and popover views bind to it as DataContext.
/// Lifecycle: constructed once → OnStart (when enabled) → RefreshAsync every PollInterval → OnStop/Dispose.
/// </summary>
public abstract partial class WidgetBase : ObservableObject, IDisposable
{
    protected WidgetBase(WidgetContext ctx)
    {
        Ctx = ctx;
        ShowSettingsCommand = HasSettings ? new RelayCommand(() => IsSettingsMode = !IsSettingsMode) : null;
    }

    protected WidgetContext Ctx { get; }

    public abstract string Id { get; }

    /// <summary>Localized display name (Widget.&lt;id&gt;.Name).</summary>
    public string DisplayName => Loc.Get($"Widget.{Id}.Name");

    [ObservableProperty] private bool isAvailable = true;
    [ObservableProperty] private bool isSettingsMode;
    [ObservableProperty] private string? toolTipText;

    /// <summary>Non-null when the popover has a settings view (gear in <c>PopoverHeader</c>).</summary>
    public IRelayCommand? ShowSettingsCommand { get; }

    protected virtual bool HasSettings => false;

    public virtual TimeSpan? PollInterval => null;
    public virtual bool HasPopover => true;
    public bool IsRunning { get; internal set; }
    public bool IsPopoverOpen { get; private set; }

    public virtual void OnStart() { }
    public virtual void OnStop() { }
    public virtual Task RefreshAsync(CancellationToken ct) => Task.CompletedTask;

    public virtual void OnPopoverOpened() { }
    public virtual void OnPopoverClosed() { }

    internal void NotifyPopoverOpened()
    {
        IsPopoverOpen = true;
        OnPopoverOpened();
    }

    internal void NotifyPopoverClosed()
    {
        IsPopoverOpen = false;
        IsSettingsMode = false;
        OnPopoverClosed();
    }

    public virtual void OnTileClick(FrameworkElement tile)
    {
        if (HasPopover) Ctx.Popovers.Toggle(this, tile);
    }

    public virtual void OnTileRightClick(FrameworkElement tile) { }

    /// <summary>Runs an async action, logging instead of crashing on failure.</summary>
    protected async void Fire(Func<Task> action, string what)
    {
        try { await action(); }
        catch (Exception ex) { ShellLogger.Warning($"{Id}: {what} failed: {ex.Message}"); }
    }

    public virtual void Dispose() { }
}

public sealed record WidgetDescriptor(
    string Id,
    Func<WidgetContext, WidgetBase> Create,
    Func<UserControl> CreateTile,
    Func<UserControl>? CreatePopover,
    bool IsBareTile = false);

/// <summary>Owns one instance of every widget, starts/stops them as the layout changes.</summary>
public sealed class WidgetHost : IDisposable
{
    private readonly WidgetContext _ctx;
    private readonly Dictionary<string, WidgetBase> _widgets = [];
    private readonly Dictionary<string, DispatcherTimer> _timers = [];
    private readonly Dictionary<string, CancellationTokenSource> _cts = [];

    public WidgetHost(WidgetContext ctx)
    {
        _ctx = ctx;
        foreach (var d in WidgetRegistry.All) _widgets[d.Id] = d.Create(ctx);
    }

    public IReadOnlyDictionary<string, WidgetBase> Widgets => _widgets;

    public WidgetBase Get(string id) => _widgets[id];

    public void Start()
    {
        Sync();
        _ctx.Store.Changed += OnSettingsChanged;
    }

    private void OnSettingsChanged(string property)
    {
        if (property is nameof(AppSettings.Widgets) or "*") Sync();
    }

    private void Sync()
    {
        var enabled = _ctx.Settings.Widgets.Where(w => w.Enabled).Select(w => w.Id).ToHashSet();
        foreach (var (id, w) in _widgets)
        {
            if (enabled.Contains(id) && !w.IsRunning) StartWidget(w);
            else if (!enabled.Contains(id) && w.IsRunning) StopWidget(w);
        }
    }

    private void StartWidget(WidgetBase w)
    {
        w.IsRunning = true;
        try { w.OnStart(); }
        catch (Exception ex) { ShellLogger.Error($"Widget {w.Id}: OnStart failed", ex); }

        if (w.PollInterval is { } interval)
        {
            var cts = new CancellationTokenSource();
            _cts[w.Id] = cts;
            var timer = new DispatcherTimer(DispatcherPriority.Background, _ctx.Dispatcher) { Interval = interval };
            timer.Tick += (_, _) => Refresh(w, cts.Token);
            _timers[w.Id] = timer;
            timer.Start();
            Refresh(w, cts.Token);
        }
    }

    private static async void Refresh(WidgetBase w, CancellationToken ct)
    {
        try { await w.RefreshAsync(ct); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ShellLogger.Warning($"Widget {w.Id}: refresh failed: {ex.Message}"); }
    }

    private void StopWidget(WidgetBase w)
    {
        w.IsRunning = false;
        if (_timers.Remove(w.Id, out var t)) t.Stop();
        if (_cts.Remove(w.Id, out var c)) { c.Cancel(); c.Dispose(); }
        try { w.OnStop(); }
        catch (Exception ex) { ShellLogger.Error($"Widget {w.Id}: OnStop failed", ex); }
    }

    public void Dispose()
    {
        _ctx.Store.Changed -= OnSettingsChanged;
        foreach (var w in _widgets.Values)
        {
            if (w.IsRunning) StopWidget(w);
            try { w.Dispose(); }
            catch (Exception ex) { ShellLogger.Error($"Widget {w.Id}: Dispose failed", ex); }
        }
    }
}
