using Prismbar.Core.Calendar;
using Prismbar.Core.Pomodoro;

namespace Prismbar.Core.Events;

/// <summary>Synchronous in-process pub/sub. Handler exceptions are isolated and reported.</summary>
public sealed class EventBus(Action<string, Exception>? onError = null)
{
    private readonly object _gate = new();
    private readonly Dictionary<Type, List<Delegate>> _handlers = [];

    public void Publish<T>(T evt)
    {
        Delegate[] handlers;
        lock (_gate)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            handlers = [.. list];
        }
        foreach (var h in handlers)
        {
            try { ((Action<T>)h)(evt); }
            catch (Exception ex) { onError?.Invoke($"Event handler for {typeof(T).Name} failed", ex); }
        }
    }

    public IDisposable Subscribe<T>(Action<T> handler)
    {
        lock (_gate)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) _handlers[typeof(T)] = list = [];
            list.Add(handler);
        }
        return new Subscription(() => { lock (_gate) _handlers[typeof(T)].Remove(handler); });
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}

public sealed record MeetingStarted(CalendarItem Item);
public sealed record MeetingEnded(CalendarItem Item);
public sealed record PomodoroPhaseChanged(PomodoroPhase Previous, PomodoroPhase Current);
public sealed record TimerFinished(TimeSpan Duration);
public sealed record CalendarUpdated(IReadOnlyList<CalendarItem> Items);
public sealed record BatteryStatusChanged(int Percent, bool Charging);
public sealed record MemorySampled(int LoadPercent);
