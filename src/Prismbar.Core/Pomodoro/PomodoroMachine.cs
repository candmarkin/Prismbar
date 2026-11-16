using Prismbar.Core.Events;
using Prismbar.Core.Settings;

namespace Prismbar.Core.Pomodoro;

public enum PomodoroPhase { Idle, Focus, ShortBreak, LongBreak }

/// <summary>Deadline-based pomodoro state machine (robust to timer jitter and sleep).</summary>
public sealed class PomodoroMachine(Func<PomodoroSettings> settings)
{
    public PomodoroPhase Phase { get; private set; } = PomodoroPhase.Idle;
    public bool IsPaused { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public TimeSpan RemainingWhenPaused { get; private set; }
    public int CompletedFocusCount { get; private set; }

    public void StartFocus(DateTimeOffset now) => Enter(PomodoroPhase.Focus, now);

    public void Pause(DateTimeOffset now)
    {
        if (Phase == PomodoroPhase.Idle || IsPaused) return;
        RemainingWhenPaused = Remaining(now);
        IsPaused = true;
    }

    public void Resume(DateTimeOffset now)
    {
        if (!IsPaused) return;
        IsPaused = false;
        EndsAt = now + RemainingWhenPaused;
    }

    public PomodoroPhaseChanged? Skip(DateTimeOffset now) => Advance(now, countFocus: false);

    public void Reset()
    {
        Phase = PomodoroPhase.Idle;
        IsPaused = false;
        CompletedFocusCount = 0;
        RemainingWhenPaused = TimeSpan.Zero;
    }

    public TimeSpan Remaining(DateTimeOffset now)
    {
        if (Phase == PomodoroPhase.Idle) return TimeSpan.Zero;
        if (IsPaused) return RemainingWhenPaused;
        var r = EndsAt - now;
        return r < TimeSpan.Zero ? TimeSpan.Zero : r;
    }

    public PomodoroPhaseChanged? Tick(DateTimeOffset now)
    {
        if (Phase == PomodoroPhase.Idle || IsPaused || now < EndsAt) return null;
        return Advance(now, countFocus: true);
    }

    private PomodoroPhaseChanged? Advance(DateTimeOffset now, bool countFocus)
    {
        var previous = Phase;
        switch (Phase)
        {
            case PomodoroPhase.Idle:
                return null;
            case PomodoroPhase.Focus:
                if (countFocus) CompletedFocusCount++;
                var every = Math.Max(1, settings().LongBreakEvery);
                Enter(countFocus && CompletedFocusCount % every == 0 ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak, now);
                break;
            default:
                Phase = PomodoroPhase.Idle;
                IsPaused = false;
                break;
        }
        return new PomodoroPhaseChanged(previous, Phase);
    }

    private void Enter(PomodoroPhase phase, DateTimeOffset now)
    {
        var s = settings();
        int minutes = phase switch
        {
            PomodoroPhase.Focus => s.FocusMinutes,
            PomodoroPhase.ShortBreak => s.ShortBreakMinutes,
            _ => s.LongBreakMinutes,
        };
        Phase = phase;
        IsPaused = false;
        EndsAt = now + TimeSpan.FromMinutes(Math.Max(1, minutes));
    }
}
