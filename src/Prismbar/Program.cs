using Prismbar.Shell;
using Velopack;

namespace Prismbar;

internal static class Program
{
    private static Mutex? _singleInstance;

    [STAThread]
    private static int Main(string[] args)
    {
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ =>
            {
                TaskbarGuard.RecoverIfDirty(force: true);
                Autostart.Set(false);
            })
            .Run();

        if (Environment.OSVersion.Version.Build < 19041)
        {
            System.Windows.MessageBox.Show(
                "Prismbar requires Windows 10 version 2004 or later.\nO Prismbar requer o Windows 10 versão 2004 ou superior.",
                "Prismbar");
            return 1;
        }

        if (args.Length >= 2 && args[0] == "--watchdog" && int.TryParse(args[1], out var pid))
            return Watchdog.Run(pid);

        if (!AcquireSingleInstance(args.Contains("--restarted")))
        {
            InstanceMessenger.SignalShowSettings();
            return 0;
        }

        System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);
        var app = new App();
        app.InitializeComponent();
        var code = app.Run();
        _singleInstance?.ReleaseMutex();
        return code;
    }

    private static bool AcquireSingleInstance(bool restarted)
    {
        int attempts = restarted ? 10 : 1;
        for (int i = 0; i < attempts; i++)
        {
            _singleInstance = new Mutex(true, @"Local\Prismbar.SingleInstance", out bool created);
            if (created) return true;
            _singleInstance.Dispose();
            _singleInstance = null;
            if (i < attempts - 1) Thread.Sleep(500);
        }
        return false;
    }
}
