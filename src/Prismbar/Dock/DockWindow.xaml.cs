using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ManagedShell;
using ManagedShell.AppBar;
using ManagedShell.Common.Enums;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using ManagedShell.WindowsTray;
using Prismbar.Alerts;
using Prismbar.Core.Settings;
using Prismbar.Glass;
using Prismbar.Localization;
using Prismbar.Popovers;
using Prismbar.Widgets;

namespace Prismbar.Dock;

public sealed record DockContext(
    SettingsStore Store,
    WidgetHost Widgets,
    PopoverService Popovers,
    AlertService Alerts,
    DesktopCaptureService Capture,
    DockManager DockManager,
    ShellManager Shell);

public partial class DockWindow : AppBarWindow
{
    private readonly DockContext _ctx;
    private readonly ShellManager _shell;
    private readonly List<(DockTile Tile, WidgetBase Widget)> _tiles = [];
    private bool _glassPaused;
    private Point _dragStart;
    private bool _dragging;

    public DockWindow(ShellManager shell, AppBarScreen screen, AppSettings s, DockContext ctx)
        : base(shell.AppBarManager, shell.ExplorerHelper, shell.FullScreenHelper, screen,
            s.Edge == DockEdge.Top ? AppBarEdge.Top : AppBarEdge.Bottom, ModeFor(s), DockMetrics.From(s).WindowHeight)
    {
        _ctx = ctx;
        _shell = shell;
        Settings = s;
        Metrics = DockMetrics.From(s);
        StyleParams = GlassStyles.Get(s.Style);
        RequiresScreenEdge = true;
        InitializeComponent();
        if (s.AutoHide) AutoHideElement = DockRoot;

        ApplyLayout();
        PopulateZones();

        Chrome.PreviewMouseLeftButtonDown += Chrome_PreviewMouseLeftButtonDown;
        Chrome.PreviewMouseMove += Chrome_PreviewMouseMove;
        Chrome.PreviewMouseLeftButtonUp += Chrome_PreviewMouseLeftButtonUp;
        DockRoot.ContextMenuOpening += DockRoot_ContextMenuOpening;
        DockRoot.ContextMenu = BuildContextMenu();
    }

    private static AppBarMode ModeFor(AppSettings s) =>
        s.AutoHide ? AppBarMode.AutoHide : s.Shape == DockShape.FullWidth ? AppBarMode.Normal : AppBarMode.None;

    public AppSettings Settings { get; }
    public DockMetrics Metrics { get; }
    public GlassStyleParams StyleParams { get; }
    public DockContext Context => _ctx;

    /// <summary>The element whose bounds are the visible dock surface (pill or full-width bar).</summary>
    public FrameworkElement Surface => DockRoot;

    public Border GlassHostBorder => GlassHost;
    public Border ChromeBorder => Chrome;

    /// <summary>True while hidden (auto-hide) or a full-screen app covers the monitor: glass stops capturing.</summary>
    public bool GlassPaused
    {
        get => _glassPaused;
        private set
        {
            if (_glassPaused == value) return;
            _glassPaused = value;
            GlassPausedChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? GlassPausedChanged;

    /// <summary>Raised after construction so optional modules (glass) can attach to the dock.</summary>
    public static event Action<DockWindow>? Created;

    private void ApplyLayout()
    {
        var m = Metrics;
        bool top = m.IsTop;
        var verticalEdge = top ? VerticalAlignment.Top : VerticalAlignment.Bottom;

        DockRoot.Height = m.BarHeight;
        DockRoot.VerticalAlignment = verticalEdge;
        DockRoot.Margin = top ? new Thickness(0, m.EdgeGap, 0, 0) : new Thickness(0, 0, 0, m.EdgeGap);
        DockRoot.HorizontalAlignment = m.IsPill ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
        if (m.IsPill) DockRoot.Effect = new DropShadowEffect { BlurRadius = 18, Opacity = 0.35, ShadowDepth = 2, Direction = 270 };
        CenterColumn.Width = m.IsPill ? GridLength.Auto : new GridLength(1, GridUnitType.Star);

        EdgeHitStrip.VerticalAlignment = verticalEdge;
        EdgeHitStrip.Visibility = Settings.AutoHide ? Visibility.Visible : Visibility.Collapsed;

        LeftSeparator.Height = RightSeparator.Height = m.BarHeight * 0.5;

        var p = StyleParams;
        var radius = new CornerRadius(m.CornerRadius);
        Chrome.CornerRadius = radius;
        GlassHost.CornerRadius = radius;
        if (p.IsGlass)
        {
            // Glass module paints the backdrop into GlassHost; until then show the opaque fallback.
            Chrome.Background = new SolidColorBrush(GlassStyles.GlassFallback);
            Chrome.BorderBrush = new SolidColorBrush(p.Border);
        }
        else
        {
            Chrome.Background = new SolidColorBrush(p.Solid);
            Chrome.BorderBrush = new SolidColorBrush(p.Border);
        }
        Chrome.BorderThickness = m.IsPill ? new Thickness(1) : top ? new Thickness(0, 0, 0, 1) : new Thickness(0, 1, 0, 0);
    }

    private void PopulateZones()
    {
        var placements = Settings.Widgets.Where(w => w.Enabled).OrderBy(w => w.Zone).ThenBy(w => w.Order);
        foreach (var p in placements)
        {
            var desc = WidgetRegistry.All.FirstOrDefault(d => d.Id == p.Id);
            if (desc is null) continue;
            var widget = _ctx.Widgets.Get(p.Id);
            FrameworkElement element;
            try
            {
                element = CreateTileElement(desc, widget);
            }
            catch (Exception ex)
            {
                ShellLogger.Error($"Dock: failed to create tile for {p.Id}", ex);
                continue;
            }
            switch (p.Zone)
            {
                case DockZone.Left: LeftZone.Children.Add(element); break;
                case DockZone.Center: CenterZone.Content = element; break;
                default: RightZone.Children.Add(element); break;
            }
            element.IsVisibleChanged += (_, _) => UpdateSeparators();
        }
        UpdateSeparators();
    }

    private FrameworkElement CreateTileElement(WidgetDescriptor desc, WidgetBase widget)
    {
        var content = desc.CreateTile();
        content.DataContext = widget;
        FrameworkElement element;
        if (desc.IsBareTile)
        {
            element = content;
        }
        else
        {
            var tile = new DockTile
            {
                Content = content,
                DataContext = widget,
                MinWidth = Metrics.IconSize + 12,
                Height = Metrics.BarHeight - 8,
            };
            tile.TileClick += (_, _) => widget.OnTileClick(tile);
            tile.TileRightClick += (_, _) => widget.OnTileRightClick(tile);
            tile.SetBinding(ToolTipProperty, new Binding(nameof(WidgetBase.ToolTipText))
            {
                Source = widget,
                TargetNullValue = widget.DisplayName,
            });
            ToolTipService.SetInitialShowDelay(tile, 500);
            ToolTipService.SetPlacement(tile, Metrics.IsTop ? System.Windows.Controls.Primitives.PlacementMode.Bottom : System.Windows.Controls.Primitives.PlacementMode.Top);
            _tiles.Add((tile, widget));
            element = tile;
        }
        element.SetBinding(VisibilityProperty, new Binding(nameof(WidgetBase.IsAvailable))
        {
            Source = widget,
            Converter = new BooleanToVisibilityConverter(),
        });
        return element;
    }

    private static bool AnyVisible(Panel p) => p.Children.Cast<UIElement>().Any(c => c.Visibility == Visibility.Visible);

    private void UpdateSeparators()
    {
        bool left = AnyVisible(LeftZone), right = AnyVisible(RightZone);
        bool center = CenterZone.Content is UIElement { Visibility: Visibility.Visible };
        LeftSeparator.Visibility = left && (center || right) ? Visibility.Visible : Visibility.Collapsed;
        RightSeparator.Visibility = right && center ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void OnSourceInitialized(object? sender, EventArgs e)
    {
        base.OnSourceInitialized(sender, e);
        WindowHelper.HideWindowFromTasks(Handle);
        Created?.Invoke(this);
    }

    protected override bool ShouldAllowAutoHide() =>
        base.ShouldAllowAutoHide() && !_ctx.Popovers.IsOpenOn(this) && !_ctx.Alerts.AnyVisible;

    public void RefreshAutoHide() => OnPropertyChanged(nameof(AllowAutoHide));

    protected override void OnFullScreenEnter(FullScreenApp app)
    {
        base.OnFullScreenEnter(app);
        GlassPaused = true;
    }

    protected override void OnFullScreenLeave()
    {
        base.OnFullScreenLeave();
        GlassPaused = false;
    }

    protected override void OnAutoHideAnimationBegin(bool isHiding)
    {
        base.OnAutoHideAnimationBegin(isHiding);
        if (!isHiding) GlassPaused = false;
    }

    protected override void OnAutoHideAnimationComplete(bool isHiding)
    {
        base.OnAutoHideAnimationComplete(isHiding);
        if (isHiding) GlassPaused = true;
    }

    protected override void SetScreenProperties(ScreenSetupReason reason)
    {
        if (reason == ScreenSetupReason.DpiChange)
        {
            UpdatePosition();
            return;
        }
        if (Settings.ShowOnAllMonitors)
        {
            _ctx.DockManager.NotifyDisplayChange();
            return;
        }
        base.SetScreenProperties(reason);
    }

    public override bool UpdatePosition()
    {
        var result = base.UpdatePosition();
        try
        {
            if (Screen.Primary && _shell.NotificationArea is { } tray && DockRoot.IsLoaded && PresentationSource.FromVisual(DockRoot) is not null)
            {
                var tl = DockRoot.PointToScreen(new Point(0, 0));
                var br = DockRoot.PointToScreen(new Point(DockRoot.ActualWidth, DockRoot.ActualHeight));
                tray.SetTrayHostSizeData(new TrayHostSizeData
                {
                    edge = (NativeMethods.ABEdge)(int)AppBarEdge,
                    rc = new NativeMethods.Rect { Left = (int)tl.X, Top = (int)tl.Y, Right = (int)br.X, Bottom = (int)br.Y },
                });
            }
        }
        catch (Exception ex)
        {
            ShellLogger.Debug($"Dock: SetTrayHostSizeData failed: {ex.Message}");
        }
        return result;
    }

    protected override void CustomClosing()
    {
        if (!AllowClose) return;
        foreach (var (tile, _) in _tiles) tile.Content = null;
        _tiles.Clear();
        LeftZone.Children.Clear();
        RightZone.Children.Clear();
        CenterZone.Content = null;
        GlassHost.Child = null;
    }

    // ---- context menu -------------------------------------------------------------------------

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();
        MenuItem Item(string key, Action action)
        {
            var mi = new MenuItem();
            mi.SetResourceReference(HeaderedItemsControl.HeaderProperty, key);
            mi.Click += (_, _) => action();
            return mi;
        }
        menu.Items.Add(Item("Dock.Menu.Settings", () => App.Instance.ShowSettings()));
        menu.Items.Add(Item("Dock.Menu.TaskManager", ShellHelper.StartTaskManager));
        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Dock.Menu.Restart", () => App.Instance.RestartApp()));
        menu.Items.Add(Item("Dock.Menu.Exit", () => App.Instance.ExitGracefully()));
        return menu;
    }

    private void DockRoot_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        // Only the dock background opens the dock menu; tiles have their own behavior.
        if (e.OriginalSource is DependencyObject d && FindAncestor<DockTile>(d) is not null) e.Handled = true;
        if (e.OriginalSource is DependencyObject d2 && FindAncestor<FrameworkElement>(d2, fe => fe.Tag as string == "NoDockMenu") is not null) e.Handled = true;
    }

    private static T? FindAncestor<T>(DependencyObject d, Func<T, bool>? predicate = null) where T : DependencyObject
    {
        while (d is not null)
        {
            if (d is T t && (predicate is null || predicate(t))) return t;
            d = d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
        }
        return null;
    }

    // ---- drag to switch edge ------------------------------------------------------------------

    private void Chrome_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _dragging = false;
        Chrome.CaptureMouse();
    }

    private void Chrome_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!Chrome.IsMouseCaptured) return;
        if (!_dragging && Math.Abs(e.GetPosition(this).Y - _dragStart.Y) > 40)
        {
            _dragging = true;
            Opacity = 0.7;
        }
    }

    private void Chrome_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!Chrome.IsMouseCaptured) return;
        Chrome.ReleaseMouseCapture();
        Opacity = 1;
        if (!_dragging) return;
        _dragging = false;
        var cursor = System.Windows.Forms.Cursor.Position;
        var bounds = Screen.Bounds;
        var edge = cursor.Y < bounds.Top + bounds.Height / 2 ? DockEdge.Top : DockEdge.Bottom;
        if (edge != Settings.Edge)
            _ctx.Store.Update(st => st.Edge = edge, nameof(AppSettings.Edge));
    }
}
