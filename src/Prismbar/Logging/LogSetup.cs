using System.IO;
using System.Security.Cryptography;
using System.Text;
using ManagedShell.Common.Logging;
using ManagedShell.Common.Logging.Observers;
using Prismbar.Core;
using Prismbar.Core.Security;

namespace Prismbar.Logging
{
    internal static class LogSetup
    {
        private static FileLog? _fileLog;

        public static void Start(bool debug)
        {
            Directory.CreateDirectory(AppPaths.LogsDir);
            foreach (var old in Directory.GetFiles(AppPaths.LogsDir, "*.log"))
            {
                try { if (File.GetLastWriteTime(old) < DateTime.Now.AddDays(-7)) File.Delete(old); }
                catch (IOException) { }
            }
            _fileLog = new FileLog(Path.Combine(AppPaths.LogsDir, $"{DateTime.Now:yyyy-MM-dd_HHmmss}.log"));
            _fileLog.Open();
            SetDebug(debug);
            ShellLogger.Attach(_fileLog, flushOrphanedEvents: true);
            ShellLogger.Info($"Prismbar {typeof(LogSetup).Assembly.GetName().Version} starting on {Environment.OSVersion}");
        }

        public static void SetDebug(bool debug) => ShellLogger.Severity = debug ? LogSeverity.Debug : LogSeverity.Info;

        public static void Stop()
        {
            if (_fileLog is null) return;
            ShellLogger.Detach(_fileLog);
            _fileLog.Dispose();
            _fileLog = null;
        }
    }
}

namespace Prismbar.Services
{
    public sealed class DpapiSecretProtector : ISecretProtector
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Prismbar.v1");

        public string Protect(string plain) =>
            Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), Entropy, DataProtectionScope.CurrentUser));

        public string? Unprotect(string protectedBase64)
        {
            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(protectedBase64), Entropy, DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException) { return null; }
            catch (FormatException) { return null; }
        }
    }
}
