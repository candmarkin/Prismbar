namespace Prismbar.Core.Timers;

public sealed class CountdownTimer
{
    public bool IsRunning { get; private set; }
    public TimeSpan Duration { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }

    public void Start(TimeSpan duration, DateTimeOffset now)
    {
        Duration = duration;
        EndsAt = now + duration;
        IsRunning = true;
    }

    public void Cancel() => IsRunning = false;

    public TimeSpan Remaining(DateTimeOffset now)
    {
        if (!IsRunning) return TimeSpan.Zero;
        var r = EndsAt - now;
        return r < TimeSpan.Zero ? TimeSpan.Zero : r;
    }

    /// <summary>Returns true exactly once, when the countdown reaches zero.</summary>
    public bool Tick(DateTimeOffset now)
    {
        if (!IsRunning || now < EndsAt) return false;
        IsRunning = false;
        return true;
    }
}
