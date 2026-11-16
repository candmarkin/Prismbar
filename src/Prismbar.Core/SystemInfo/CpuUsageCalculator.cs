namespace Prismbar.Core.SystemInfo;

/// <summary>Turns cumulative GetSystemTimes counters into a usage percentage. Kernel time includes idle.</summary>
public sealed class CpuUsageCalculator
{
    private ulong _idle, _kernel, _user;
    private bool _hasPrevious;

    public double? Sample(ulong idle, ulong kernel, ulong user)
    {
        double? result = null;
        if (_hasPrevious)
        {
            double dIdle = idle - _idle;
            double dTotal = (kernel - _kernel) + (user - _user);
            result = dTotal <= 0 ? 0 : Math.Clamp((1 - dIdle / dTotal) * 100, 0, 100);
        }
        (_idle, _kernel, _user, _hasPrevious) = (idle, kernel, user, true);
        return result;
    }
}
