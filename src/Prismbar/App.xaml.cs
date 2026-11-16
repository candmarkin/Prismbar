using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using ManagedShell;
using ManagedShell.Common.Logging;
using Prismbar.Alerts;
using Prismbar.Automation;
using Prismbar.Core;
using Prismbar.Core.Automation;
using Prismbar.Core.Events;
using Prismbar.Core.Settings;
using Prismbar.Core.Time;
using Prismbar.Dock;
using Prismbar.Glass;
using Prismbar.Localization;
using Prismbar.Logging;
using Prismbar.Popovers;
using Prismbar.Services;
using Prismbar.Shell;
using Prismbar.Theming;
using Prismbar.Widgets;

namespace Prismbar;

public partial class App : Application
{
    private const int UCEERR_RENDERTHREADFAILURE = unchecked((int)0x88980406);

    private bool _exiting;
    private ShellManager? _shell;
    private ExplorerMonitor? _explorerMonitor;
    private InstanceMessenger? _messenger;
    private readonly List<IDisposable> _services = [];

    public static App Instance => (App)Current;

    public SettingsStore Store { get; } = new(msg => ShellLogger.Warning(msg));
    public EventBus Bus { get; } = new((msg, ex) => ShellLogger.Error(msg, ex));
    public IClock Clock { get; } = new SystemClock();
    public HttpClient Http { get; private set; } = null!;
    public ShellManager Shell => _shell!;
    public WidgetHost Widgets { get; private set; } = null!;
    public DockManager Docks { get; private set; } = null!;
    public PopoverService Popovers { get; private set; } = null!;
    public AlertService Alerts { get; private set; } = null!;
    public UpdateService Updates { get; private set; } = null!;
    public QuietHoursGate QuietGate { get; private set; } = null!;
    public PinStore Pins { get; private set; } = null!;
    public DpapiSecretProtector Secrets { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ShellLogger.Warning($"Unobserved task exception: {args.Exception.GetBaseException().Message}");
            args.SetObserved();
        };
        SessionEnding += (_, _) => ExitGracefully();

        // 1. Data, settings, log
        AppPaths.EnsureCreated();
        var settings = Store.Load();
        LogSetup.Start(settings.DebugLogging || e.Args.Contains("--debug"));

        // 2. Language and theme
        Loc.Apply(settings.Language);
        ThemeManager.Apply(settings.Style);

        // 3. Restore the taskbar if a previous session crashed — before ManagedShell captures its state.
        TaskbarGuard.RecoverIfDirty(force: false);

        // 4-5. Shell + taskbar session + watchdog
        _shell = ShellHost.Create(settings);
        TaskbarGuard.BeginSession(_shell, settings.WindowsTaskbar);
        StartWatchdog();

        // 6. Services
        Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        Http.DefaultRequestHeaders.UserAgent.ParseAdd($"Prismbar/{Version}");
        var dispatcher = Dispatcher;
        var capture = Track(new DesktopCaptureService());
        var media = Track(new MediaService(dispatcher));
        var audio = Track(new AudioService(dispatcher));
        var calendar = Track(new CalendarService(Store, Http, Secrets, Bus, Clock, dispatcher));
        var screenTime = Track(new ScreenTimeService(_shell, Clock, dispatcher));
        QuietGate = new QuietHoursGate(() => Store.Current.QuietHours);
        Popovers = new PopoverService(Store);
        Alerts = Track(new AlertService(Store, QuietGate, Clock, () => Docks?.Primary));
        Track(new AutomationHost(Bus, Clock, media, Alerts, QuietGate, Store, calendar));
        Updates = Track(new UpdateService(Store, Alerts));
        Pins = new PinStore(Store);

        var ctx = new WidgetContext(Store, Bus, Clock, _shell, Popovers, Alerts, Http, Secrets, dispatcher,
            media, audio, calendar, screenTime);

        media.Start();
        audio.Start();
        calendar.Start();
        screenTime.Start();

        // 7. Widgets, docks, Explorer monitor, single-instance messenger, autostart
        Pins.ImportOnce();
        Widgets = new WidgetHost(ctx);
        Widgets.Start();
        Docks = new DockManager(_shell, Store, Widgets, Popovers, Alerts, capture);
        Docks.Open();
        _explorerMonitor = new ExplorerMonitor();
        _explorerMonitor.Start(OnExplorerRestarted);
        _messenger = new InstanceMessenger(ShowSettings);
        _messenger.Listen();
        Autostart.Set(settings.StartWithWindows);
        Store.Changed += OnSettingsChanged;

        // 8. Updates
        Updates.Start();
    }

    public static string Version =>
        typeof(App).Assembly.GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "0.0.0";

    private T Track<T>(T service) where T : IDisposable
    {
        _services.Add(service);
        return service;
    }

    private static void StartWatchdog()
    {
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, $"--watchdog {Environment.ProcessId}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            });
        }
        catch (Exception ex)
        {
            ShellLogger.Warning($"Failed to start watchdog: {ex.Message}");
        }
    }

    private void OnSettingsChanged(string property)
    {
        var s = Store.Current;
        switch (property)
        {
            case nameof(AppSettings.WindowsTaskbar):
                TaskbarGuard.EndSession(Shell);
                TaskbarGuard.BeginSession(Shell, s.WindowsTaskbar);
                break;
            case nameof(AppSettings.StartWithWindows):
                Autostart.Set(s.StartWithWindows);
                break;
            case nameof(AppSettings.DebugLogging):
                LogSetup.SetDebug(s.DebugLogging);
                break;
            case "*":
                TaskbarGuard.EndSession(Shell);
                TaskbarGuard.BeginSession(Shell, s.WindowsTaskbar);
                Autostart.Set(s.StartWithWindows);
                LogSetup.SetDebug(s.DebugLogging);
                break;
        }
    }

    private void OnExplorerRestarted()
    {
        Docks.Reopen();
        Shell.Tasks.Dispose();
        Shell.Tasks.Initialize(true);
        TaskbarGuard.Reapply(Shell);
    }

    public void ShowSettings() => Prismbar.Settings.SettingsWindow.ShowSingleton();

    public void RestartApp()
    {
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "--restarted") { UseShellExecute = false });
        }
        catch (Exception ex)
        {
            ShellLogger.Error("Failed to relaunch Prismbar", ex);
        }
        ExitGracefully();
    }

    /// <summary>Releases the shell and restores the taskbar without shutting the WPF app down (used before applying updates).</summary>
    public void PrepareForUpdate() => TearDown();

    public void ExitGracefully()
    {
        if (!TearDown()) return;
        Current.Shutdown();
    }

    private bool TearDown()
    {
        if (_exiting) return false;
        _exiting = true;
        ShellLogger.Info("Prismbar exiting");
        try
        {
            Store.Changed -= OnSettingsChanged;
            _shell?.AppBarManager.SignalGracefulShutdown();
            Docks?.Dispose();
            Widgets?.Dispose();
            _explorerMonitor?.Dispose();
            _messenger?.Dispose();
            foreach (var s in Enumerable.Reverse(_services))
            {
                try { s.Dispose(); }
                catch (Exception ex) { ShellLogger.Warning($"Dispose {s.GetType().Name} failed: {ex.Message}"); }
            }
            if (_shell is not null) TaskbarGuard.EndSession(_shell);
            Store.Flush();
            _shell?.Dispose();
        }
        catch (Exception ex)
        {
            ShellLogger.Error("Error during shutdown", ex);
            TaskbarGuard.RecoverIfDirty(force: true);
        }
        LogSetup.Stop();
        return true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TearDown();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        ShellLogger.Error("Unhandled dispatcher exception", e.Exception);
        if (e.Exception is COMException { ErrorCode: UCEERR_RENDERTHREADFAILURE })
        {
            RestartApp();
            return;
        }
        if (_exiting) return;
        var answer = MessageBox.Show(Loc.Get("App.ErrorRestart") + "\n\n" + e.Exception.Message, "Prismbar",
            MessageBoxButton.YesNo, MessageBoxImage.Error);
        if (answer == MessageBoxResult.Yes) RestartApp();
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ShellLogger.Fatal("Unhandled exception", e.ExceptionObject as Exception);
        TaskbarGuard.RecoverIfDirty(force: true);
    }
}
