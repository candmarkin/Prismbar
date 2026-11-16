using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ManagedShell.Common.Helpers;
using Prismbar.Core.Settings;
using Prismbar.Dock;
using Prismbar.Glass;
using Prismbar.Shell;
using Prismbar.Widgets;

namespace Prismbar.Popovers;

public partial class PopoverWindow : Window
{
    private readonly FrameworkElement _anchor;
    private readonly bool _openUpwards;
    private bool _closing;

    internal PopoverWindow(object content, object? dataContext, FrameworkElement anchor, bool openUpwards, AppSettings settings)
    {
        InitializeComponent();
        _anchor = anchor;
        _openUpwards = openUpwards;
        Presenter.Content = content;
        DataContext = dataContext;
        if (content is FrameworkElement fe) fe.DataContext = dataContext;
        DockStyle = settings.Style;
        GlassSource = settings.GlassSource;
        ApplyChrome();
        Slide.Y = openUpwards ? 8 : -8;
    }

    public DockStyle DockStyle { get; }
    public GlassSource GlassSource { get; }
    public GlassStyleParams StyleParams => GlassStyles.Get(DockStyle);
    public Grid GlassLayerHost => GlassLayer;
    public Border SurfaceBorder => Surface;
    public IntPtr Handle { get; private set; }

    /// <summary>Raised from SourceInitialized; the glass module hooks this to apply display affinity/glass.</summary>
    public static event Action<PopoverWindow>? Created;

    private void ApplyChrome()
    {
        var p = StyleParams;
        Surface.BorderBrush = new SolidColorBrush(p.Border);
        Surface.Background = new SolidColorBrush(p.IsGlass ? GlassStyles.GlassFallback : p.Solid);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Handle = new WindowInteropHelper(this).Handle;
        WindowHelper.HideWindowFromTasks(Handle);
        Created?.Invoke(this);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        Reposition();
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)) { EasingFunction = ease });
        Slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(150)) { EasingFunction = ease });
        Activate();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        if (IsLoaded) Reposition();
    }

    /// <summary>Places the popover centered over the anchor, clamped to the anchor's monitor, in device pixels.</summary>
    private void Reposition()
    {
        if (Handle == IntPtr.Zero || !_anchor.IsVisible || PresentationSource.FromVisual(_anchor) is null) return;
        var dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var tl = _anchor.PointToScreen(new Point(0, 0));
        var br = _anchor.PointToScreen(new Point(_anchor.ActualWidth, _anchor.ActualHeight));
        var center = new System.Drawing.Point((int)((tl.X + br.X) / 2), (int)((tl.Y + br.Y) / 2));
        var mon = System.Windows.Forms.Screen.FromPoint(center).Bounds;

        double margin = Root.Margin.Left * dpi; // shadow margin around the visual surface
        double w = ActualWidth * dpi, h = ActualHeight * dpi;
        double x = center.X - w / 2;
        x = Math.Clamp(x, mon.Left + 8 - margin, Math.Max(mon.Left + 8 - margin, mon.Right - 8 + margin - w));
        double y = _openUpwards ? tl.Y - 10 * dpi - h + margin : br.Y + 10 * dpi - margin;
        Win32.SetWindowPos(Handle, IntPtr.Zero, (int)Math.Round(x), (int)Math.Round(y), 0, 0,
            Win32.SWP_NOSIZE | Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        SafeClose();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Escape && !e.Handled)
        {
            e.Handled = true;
            SafeClose();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _closing = true;
        base.OnClosing(e);
    }

    public void SafeClose()
    {
        if (_closing) return;
        _closing = true;
        Dispatcher.BeginInvoke(Close);
    }
}

/// <summary>Opens at most one popover at a time, anchored to a dock tile.</summary>
public sealed class PopoverService
{
    private readonly SettingsStore _store;
    private PopoverWindow? _current;
    private string? _currentKey;
    private DockWindow? _currentDock;
    private string? _lastClosedKey;
    private DateTime _lastClosedAt;

    public PopoverService(SettingsStore store) => _store = store;

    public bool IsOpen => _current is not null;

    public bool IsOpenOn(DockWindow dock) => _current is not null && ReferenceEquals(_currentDock, dock);

    public void Toggle(WidgetBase widget, FrameworkElement anchor)
    {
        var key = "widget:" + widget.Id;
        if (_currentKey == key) { Close(); return; }
        if (_lastClosedKey == key && DateTime.UtcNow - _lastClosedAt < TimeSpan.FromMilliseconds(250)) return;
        var desc = WidgetRegistry.All.First(d => d.Id == widget.Id);
        if (desc.CreatePopover is null) return;
        Open(desc.CreatePopover(), widget, anchor, key, widget);
    }

    /// <summary>Generic popover (window lists, tray overflow...). Same toggle semantics keyed by <paramref name="key"/>.</summary>
    public void Show(object content, object? dataContext, FrameworkElement anchor, string key)
    {
        if (_currentKey == key) { Close(); return; }
        if (_lastClosedKey == key && DateTime.UtcNow - _lastClosedAt < TimeSpan.FromMilliseconds(250)) return;
        Open(content, dataContext, anchor, key, null);
    }

    public void Close() => _current?.SafeClose();

    private void Open(object content, object? dataContext, FrameworkElement anchor, string key, WidgetBase? widget)
    {
        _current?.Close();
        var dock = Window.GetWindow(anchor) as DockWindow;
        bool up = dock is null || dock.AppBarEdge != ManagedShell.AppBar.AppBarEdge.Top;
        var win = new PopoverWindow(content, dataContext, anchor, up, _store.Current);
        _current = win;
        _currentKey = key;
        _currentDock = dock;
        win.Closed += (_, _) =>
        {
            if (ReferenceEquals(_current, win))
            {
                _current = null;
                _currentKey = null;
                _currentDock = null;
            }
            _lastClosedKey = key;
            _lastClosedAt = DateTime.UtcNow;
            widget?.NotifyPopoverClosed();
            dock?.RefreshAutoHide();
        };
        widget?.NotifyPopoverOpened();
        win.Show();
        dock?.RefreshAutoHide();
    }
}
