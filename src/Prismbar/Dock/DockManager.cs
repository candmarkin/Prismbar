using System.Windows.Threading;
using ManagedShell;
using ManagedShell.AppBar;
using ManagedShell.Common.Logging;
using Prismbar.Alerts;
using Prismbar.Core.Settings;
using Prismbar.Glass;
using Prismbar.Popovers;
using Prismbar.Theming;
using Prismbar.Widgets;

namespace Prismbar.Dock;

/// <summary>Owns one DockWindow per screen and rebuilds them when layout-affecting settings change.</summary>
public sealed class DockManager : IDisposable
{
    private static readonly HashSet<string> ReopenProperties =
    [
        nameof(AppSettings.Style), nameof(AppSettings.Shape), nameof(AppSettings.Edge), nameof(AppSettings.AutoHide),
        nameof(AppSettings.IconSize), nameof(AppSettings.Magnification), nameof(AppSettings.ShowOnAllMonitors),
        nameof(AppSettings.Widgets), nameof(AppSettings.GlassSource), nameof(AppSettings.GlassMaxFps), "*",
    ];

    private readonly ShellManager _shell;
    private readonly SettingsStore _store;
    private readonly DockContext _ctx;
    private readonly List<DockWindow> _docks = [];
    private readonly DispatcherTimer _reopenTimer;
    private List<string> _screenSignature = [];

    public DockManager(ShellManager shell, SettingsStore store, WidgetHost widgets, PopoverService popovers,
        AlertService alerts, DesktopCaptureService capture)
    {
        _shell = shell;
        _store = store;
        _ctx = new DockContext(store, widgets, popovers, alerts, capture, this, shell);
        _reopenTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _reopenTimer.Tick += (_, _) =>
        {
            _reopenTimer.Stop();
            CloseAll();
            Open();
        };
        _store.Changed += OnSettingsChanged;
    }

    public IReadOnlyList<DockWindow> Docks => _docks;

    /// <summary>The dock on the primary monitor (alerts anchor to it).</summary>
    public DockWindow? Primary => _docks.FirstOrDefault(d => d.Screen.Primary) ?? _docks.FirstOrDefault();

    public event EventHandler? DocksChanged;

    public void Open()
    {
        var settings = _store.Current;
        var screens = settings.ShowOnAllMonitors ? AppBarScreen.FromAllScreens() : [AppBarScreen.FromPrimaryScreen()];
        _screenSignature = Signature();
        foreach (var screen in screens)
        {
            try
            {
                var dock = new DockWindow(_shell, screen, settings, _ctx);
                _docks.Add(dock);
                dock.Show();
            }
            catch (Exception ex)
            {
                ShellLogger.Error($"DockManager: failed to open dock on {screen.DeviceName}", ex);
            }
        }
        DocksChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CloseAll()
    {
        _ctx.Popovers.Close();
        foreach (var d in _docks)
        {
            d.AllowClose = true;
            d.Close();
        }
        _docks.Clear();
    }

    public void Reopen()
    {
        _reopenTimer.Stop();
        _reopenTimer.Start();
    }

    private void OnSettingsChanged(string property)
    {
        if (property is nameof(AppSettings.Style) or "*") ThemeManager.Apply(_store.Current.Style);
        if (ReopenProperties.Contains(property)) Reopen();
    }

    /// <summary>Called when a dock sees a display change; reopens only if the set of screens really changed.</summary>
    public void NotifyDisplayChange()
    {
        var now = Signature();
        if (now.SequenceEqual(_screenSignature)) return;
        ShellLogger.Info("DockManager: display configuration changed, reopening docks");
        Reopen();
    }

    private static List<string> Signature() =>
        System.Windows.Forms.Screen.AllScreens
            .Select(s => $"{s.DeviceName}|{s.Bounds}|{s.Primary}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

    public void Dispose()
    {
        _store.Changed -= OnSettingsChanged;
        _reopenTimer.Stop();
        CloseAll();
    }
}
