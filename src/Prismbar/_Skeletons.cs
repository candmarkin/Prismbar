// TEMPORARY SCAFFOLDING — each type here is replaced by its owning module's real implementation
// (the owner deletes its type from this file). This file must be gone before release.
using System.Net.Http;
using System.Windows.Threading;
using ManagedShell;
using Prismbar.Core.Alerts;
using Prismbar.Core.Automation;
using Prismbar.Core.Calendar;
using Prismbar.Core.Events;
using Prismbar.Core.Security;
using Prismbar.Core.Settings;
using Prismbar.Core.Time;
using Prismbar.Dock;

namespace Prismbar.Glass
{
    public sealed class DesktopCaptureService : IDisposable
    {
        public void Dispose() { }
    }
}

namespace Prismbar.Services
{
    public sealed class MediaService(Dispatcher dispatcher) : IMediaControl, IDisposable
    {
        public bool HasSession => false;
        public bool IsPlaying => false;
        public Task PauseAsync() => Task.CompletedTask;
        public Task PlayAsync() => Task.CompletedTask;
        public void Start() { }
        public void Dispose() { }
    }

    public sealed class AudioService(Dispatcher dispatcher) : IDisposable
    {
        public void Start() { }
        public void Dispose() { }
    }

    public sealed class CalendarService(SettingsStore store, HttpClient http, ISecretProtector secrets, EventBus bus, IClock clock, Dispatcher dispatcher) : IDisposable
    {
        public IReadOnlyList<CalendarItem> Items => [];
        public IReadOnlyList<CalendarItem> Upcoming(int n) => [];
        public void Start() { }
        public void Dispose() { }
    }

    public sealed class ScreenTimeService(ShellManager shell, IClock clock, Dispatcher dispatcher) : IDisposable
    {
        public void Start() { }
        public void Dispose() { }
    }

    public sealed class UpdateService(SettingsStore store, Alerts.AlertService alerts) : IDisposable
    {
        public void Start() { }
        public void Dispose() { }
    }
}

namespace Prismbar.Alerts
{
    public sealed class AlertService(SettingsStore store, QuietHoursGate gate, IClock clock, Func<DockWindow?> primaryDock) : IAlertSink, IDisposable
    {
        public bool AnyVisible => false;
        public void Show(Alert alert) { }
        public void Dispose() { }
    }
}

namespace Prismbar.Automation
{
    public sealed class AutomationHost(EventBus bus, IClock clock, Services.MediaService media, Alerts.AlertService alerts,
        QuietHoursGate gate, SettingsStore store, Services.CalendarService calendar) : IDisposable
    {
        public void Dispose() { }
    }
}

namespace Prismbar.Shell
{
    public sealed class PinStore(SettingsStore store)
    {
        public void ImportOnce() { }
    }
}

namespace Prismbar.Settings
{
    public static class SettingsWindow
    {
        public static void ShowSingleton() { }
    }
}
