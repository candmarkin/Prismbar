using ManagedShell;
using ManagedShell.Common.Enums;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using Microsoft.Win32;
using Prismbar.Core.Settings;

namespace Prismbar.Shell;

internal static class ShellHost
{
    public static ShellManager Create(AppSettings settings)
    {
        EnvironmentHelper.IsAppRunningAsShell = NativeMethods.GetShellWindow() == IntPtr.Zero;
        var c = ShellManager.DefaultShellConfig;
        c.TaskIconSize = IconSize.ExtraLarge;
        c.PinnedNotifyIcons = settings.PinnedTrayIcons.ToArray();
        return new ShellManager(c);
    }
}

/// <summary>Listens for the TaskbarCreated broadcast (Explorer restarted).</summary>
internal sealed class ExplorerMonitor : IDisposable
{
    private MonitorWindow? _window;

    public void Start(Action onExplorerRestarted)
    {
        _window ??= new MonitorWindow(onExplorerRestarted);
    }

    public void Dispose()
    {
        _window?.DestroyHandle();
        _window = null;
    }

    private sealed class MonitorWindow : System.Windows.Forms.NativeWindow
    {
        private readonly int _msg = RegisterWindowMessage("TaskbarCreated");
        private readonly Action _callback;

        public MonitorWindow(Action callback)
        {
            _callback = callback;
            CreateHandle(new System.Windows.Forms.CreateParams());
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == _msg)
            {
                ShellLogger.Info("ExplorerMonitor: Explorer restarted");
                try { _callback(); }
                catch (Exception ex) { ShellLogger.Error("ExplorerMonitor: restart handling failed", ex); }
            }
            base.WndProc(ref m);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int RegisterWindowMessage(string msg);
    }
}

/// <summary>Message-only window that lets a second instance ask the running one to show Settings.</summary>
internal sealed class InstanceMessenger : System.Windows.Forms.NativeWindow, IDisposable
{
    private const string Caption = "Prismbar.MessageWindow";
    private const int WM_SHOW_SETTINGS = Win32.WM_APP + 1;
    private readonly Action _showSettings;

    public InstanceMessenger(Action showSettings) => _showSettings = showSettings;

    public void Listen()
    {
        if (Handle != IntPtr.Zero) return;
        CreateHandle(new System.Windows.Forms.CreateParams { Caption = Caption, Parent = new IntPtr(Win32.HWND_MESSAGE) });
    }

    public static void SignalShowSettings()
    {
        var hwnd = Win32.FindWindowEx(new IntPtr(Win32.HWND_MESSAGE), IntPtr.Zero, null, Caption);
        if (hwnd != IntPtr.Zero) Win32.PostMessage(hwnd, WM_SHOW_SETTINGS, IntPtr.Zero, IntPtr.Zero);
    }

    protected override void WndProc(ref System.Windows.Forms.Message m)
    {
        if (m.Msg == WM_SHOW_SETTINGS)
        {
            ShellLogger.Info("InstanceMessenger: second instance requested Settings");
            _showSettings();
            return;
        }
        base.WndProc(ref m);
    }

    public void Dispose() => DestroyHandle();
}

internal static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled) key.SetValue("Prismbar", $"\"{Environment.ProcessPath}\"");
            else if (key.GetValue("Prismbar") is not null) key.DeleteValue("Prismbar");
        }
        catch (Exception ex)
        {
            ShellLogger.Warning($"Autostart: failed to update Run key ({ex.Message})");
        }
    }
}
