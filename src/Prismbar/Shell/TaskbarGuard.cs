using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using ManagedShell;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using Prismbar.Core;
using Prismbar.Core.Settings;

namespace Prismbar.Shell;

/// <summary>
/// Hides the Windows taskbar for the session and guarantees it comes back: the original state is
/// persisted in session.json so a watchdog, uninstall hook or the next start can restore it.
/// </summary>
internal static class TaskbarGuard
{
    private sealed record Session(int Pid, int OriginalState, string Mode);

    private static Session? _current;

    public static void BeginSession(ShellManager shell, TaskbarMode mode)
    {
        var original = (int)shell.ExplorerHelper.GetTaskbarState();
        _current = new Session(Environment.ProcessId, original, mode.ToString());
        Directory.CreateDirectory(Path.GetDirectoryName(AppPaths.SessionFile)!);
        File.WriteAllText(AppPaths.SessionFile, JsonSerializer.Serialize(_current));
        if (mode == TaskbarMode.Hidden)
            shell.ExplorerHelper.HideExplorerTaskbar = true;
        else
            shell.ExplorerHelper.SetTaskbarState(NativeMethods.ABState.AutoHide);
        ShellLogger.Info($"TaskbarGuard: session started (mode {mode}, original state {original})");
    }

    public static void EndSession(ShellManager shell)
    {
        if (_current is null) return;
        if (_current.Mode == nameof(TaskbarMode.Hidden))
            shell.ExplorerHelper.HideExplorerTaskbar = false;
        else
            shell.ExplorerHelper.SetTaskbarState((NativeMethods.ABState)_current.OriginalState);
        _current = null;
        TryDelete();
        ShellLogger.Info("TaskbarGuard: session ended, taskbar restored");
    }

    /// <summary>Re-applies the taskbar mode after Explorer restarts.</summary>
    public static void Reapply(ShellManager shell)
    {
        if (_current?.Mode == nameof(TaskbarMode.AutoHide))
            shell.ExplorerHelper.SetTaskbarState(NativeMethods.ABState.AutoHide);
    }

    /// <summary>Restores the taskbar from a stale session file. Uses only raw Win32 (no WPF).</summary>
    public static void RecoverIfDirty(bool force)
    {
        Session? session;
        try
        {
            if (!File.Exists(AppPaths.SessionFile)) return;
            session = JsonSerializer.Deserialize<Session>(File.ReadAllText(AppPaths.SessionFile));
        }
        catch (Exception ex)
        {
            ShellLogger.Warning($"TaskbarGuard: unreadable session file ({ex.Message})");
            session = new Session(0, 0, nameof(TaskbarMode.Hidden));
        }
        if (session is null) return;

        if (!force && session.Pid != Environment.ProcessId && IsPrismbar(session.Pid)) return;

        var tray = Win32.FindWindow("Shell_TrayWnd", null);
        var abd = new Win32.APPBARDATA
        {
            cbSize = Marshal.SizeOf<Win32.APPBARDATA>(),
            hWnd = tray,
            lParam = (IntPtr)session.OriginalState,
        };
        Win32.SHAppBarMessage(Win32.ABM_SETSTATE, ref abd);

        const uint flags = Win32.SWP_SHOWWINDOW | Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE | Win32.SWP_NOZORDER;
        if (tray != IntPtr.Zero) Win32.SetWindowPos(tray, IntPtr.Zero, 0, 0, 0, 0, flags);
        var prev = IntPtr.Zero;
        while ((prev = Win32.FindWindowEx(IntPtr.Zero, prev, "Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
            Win32.SetWindowPos(prev, IntPtr.Zero, 0, 0, 0, 0, flags);

        TryDelete();
        ShellLogger.Info($"TaskbarGuard: recovered taskbar from stale session (pid {session.Pid}, state {session.OriginalState})");
    }

    private static bool IsPrismbar(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            return !p.HasExited && p.ProcessName.Equals("Prismbar", StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    private static void TryDelete()
    {
        try { File.Delete(AppPaths.SessionFile); }
        catch (IOException ex) { ShellLogger.Warning($"TaskbarGuard: could not delete session file ({ex.Message})"); }
    }
}

internal static class Watchdog
{
    /// <summary>Waits for the main process to exit, then restores the taskbar if it left it hidden.</summary>
    public static int Run(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            p.WaitForExit();
        }
        catch (ArgumentException) { }
        TaskbarGuard.RecoverIfDirty(force: true);
        return 0;
    }
}
