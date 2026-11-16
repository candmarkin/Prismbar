using Prismbar.Core.Alerts;
using Prismbar.Core.Calendar;
using Prismbar.Core.Events;
using Prismbar.Core.Pomodoro;
using Prismbar.Core.Settings;
using Prismbar.Core.Time;

namespace Prismbar.Core.Alerts
{
    public enum AlertKind { Info, Meeting, Timer, Warning }

    public sealed record Alert(string Title, string? Body, AlertKind Kind, string? ActionLabel, Action? OnClick, bool PlaySound);

    public interface IAlertSink
    {
        void Show(Alert alert);
    }
}

namespace Prismbar.Core.Automation
{
    public interface IMediaControl
    {
        bool HasSession { get; }
        bool IsPlaying { get; }
        Task PauseAsync();
        Task PlayAsync();
    }

    public sealed class QuietHoursGate(Func<QuietHoursSettings> settings)
    {
        public bool PomodoroFocusQuiet { get; set; }

        public bool IsQuiet(DateTimeOffset now)
        {
            if (PomodoroFocusQuiet) return true;
            var s = settings();
            if (!s.Enabled) return false;
            var t = TimeOnly.FromDateTime(now.DateTime);
            if (s.Start < s.End) return t >= s.Start && t < s.End;
            if (s.Start > s.End) return t >= s.Start || t < s.End;
            return false;
        }
    }

    /// <summary>Cross-widget rules: meetings vs music, reminders, battery/memory and timer alerts.</summary>
    public sealed class AutomationEngine : IDisposable
    {
        private readonly IClock _clock;
        private readonly IMediaControl _media;
        private readonly IAlertSink _alerts;
        private readonly QuietHoursGate _gate;
        private readonly Func<AutomationSettings> _settings;
        private readonly Func<IReadOnlyList<CalendarItem>> _items;
        private readonly Func<string?> _topMemoryProcess;
        private readonly Action<string> _openUrl;
        private readonly Func<string, string> _loc;
        private readonly EventBus _bus;
        private readonly List<IDisposable> _subs = [];

        private readonly Dictionary<string, DateTimeOffset> _reminded = [];
        private readonly Dictionary<string, CalendarItem> _active = [];
        private bool _pausedByUs;
        private bool _batteryAlerted;
        private DateTimeOffset? _memoryHighSince;
        private DateTimeOffset _lastMemoryAlert = DateTimeOffset.MinValue;

        public AutomationEngine(EventBus bus, IClock clock, IMediaControl media, IAlertSink alerts, QuietHoursGate gate,
            Func<AutomationSettings> settings, Func<IReadOnlyList<CalendarItem>> items, Func<string?> topMemoryProcess,
            Action<string> openUrl, Func<string, string> loc)
        {
            (_bus, _clock, _media, _alerts, _gate, _settings, _items, _topMemoryProcess, _openUrl, _loc) =
                (bus, clock, media, alerts, gate, settings, items, topMemoryProcess, openUrl, loc);
            _subs.Add(bus.Subscribe<BatteryStatusChanged>(OnBattery));
            _subs.Add(bus.Subscribe<MemorySampled>(OnMemory));
            _subs.Add(bus.Subscribe<PomodoroPhaseChanged>(OnPomodoro));
            _subs.Add(bus.Subscribe<TimerFinished>(OnTimer));
        }

        private static string Key(CalendarItem i) => i.Uid + "|" + i.Start.UtcTicks;

        public async Task TickAsync()
        {
            var s = _settings();
            var now = _clock.Now;
            var meetings = _items().Where(i => i.MeetingUrl != null && !i.AllDay).ToList();

            if (s.MeetingReminderMinutes > 0)
            {
                foreach (var m in meetings)
                {
                    var key = Key(m);
                    if (now >= m.Start - TimeSpan.FromMinutes(s.MeetingReminderMinutes) && now < m.Start && !_reminded.ContainsKey(key))
                    {
                        _reminded[key] = now;
                        var mins = Math.Max(1, (int)Math.Ceiling((m.Start - now).TotalMinutes));
                        var url = m.MeetingUrl!;
                        _alerts.Show(new Alert(string.Format(_loc("Alert.MeetingSoon"), mins, m.Title), null, AlertKind.Meeting,
                            _loc("Alert.Join"), () => _openUrl(url), false));
                    }
                }
            }

            var nowActive = meetings.Where(m => m.Start <= now && now < m.End).ToDictionary(Key);
            foreach (var (key, item) in nowActive)
            {
                if (_active.ContainsKey(key)) continue;
                _active[key] = item;
                _bus.Publish(new MeetingStarted(item));
                if (s.PauseMusicOnMeeting && _media.IsPlaying && !_pausedByUs)
                {
                    _pausedByUs = true;
                    await _media.PauseAsync();
                }
            }
            foreach (var key in _active.Keys.Where(k => !nowActive.ContainsKey(k)).ToList())
            {
                var item = _active[key];
                _active.Remove(key);
                _bus.Publish(new MeetingEnded(item));
            }
            if (_active.Count == 0 && _pausedByUs)
            {
                _pausedByUs = false;
                if (s.ResumeMusicAfterMeeting && _media.HasSession && !_media.IsPlaying)
                    await _media.PlayAsync();
            }

            foreach (var k in _reminded.Where(kv => now - kv.Value > TimeSpan.FromDays(1)).Select(kv => kv.Key).ToList())
                _reminded.Remove(k);
        }

        private void OnBattery(BatteryStatusChanged e)
        {
            var s = _settings();
            if (e.Charging) { _batteryAlerted = false; return; }
            if (s.BatteryLowAlert && e.Percent <= s.BatteryLowPercent && !_batteryAlerted)
            {
                _batteryAlerted = true;
                _alerts.Show(new Alert(string.Format(_loc("Alert.BatteryLow"), e.Percent), null, AlertKind.Warning, null, null, false));
            }
        }

        private void OnMemory(MemorySampled e)
        {
            var s = _settings();
            var now = _clock.Now;
            if (!s.MemoryPressureAlert || e.LoadPercent < s.MemoryPressurePercent) { _memoryHighSince = null; return; }
            _memoryHighSince ??= now;
            if (now - _memoryHighSince >= TimeSpan.FromSeconds(60) && now - _lastMemoryAlert >= TimeSpan.FromMinutes(30))
            {
                _lastMemoryAlert = now;
                _alerts.Show(new Alert(string.Format(_loc("Alert.MemoryHigh"), e.LoadPercent, _topMemoryProcess() ?? "?"), null,
                    AlertKind.Warning, null, null, false));
            }
        }

        private void OnPomodoro(PomodoroPhaseChanged e)
        {
            _gate.PomodoroFocusQuiet = _settings().QuietDuringPomodoroFocus && e.Current == PomodoroPhase.Focus;
            if (e.Previous == PomodoroPhase.Focus && e.Current is PomodoroPhase.ShortBreak or PomodoroPhase.LongBreak)
                _alerts.Show(new Alert(_loc("Alert.FocusDone"), null, AlertKind.Timer, null, null, true));
            else if (e.Previous is PomodoroPhase.ShortBreak or PomodoroPhase.LongBreak && e.Current == PomodoroPhase.Idle)
                _alerts.Show(new Alert(_loc("Alert.BreakDone"), null, AlertKind.Timer, null, null, true));
        }

        private void OnTimer(TimerFinished e)
        {
            var label = e.Duration.TotalMinutes >= 1 ? $"{(int)e.Duration.TotalMinutes} min" : $"{(int)e.Duration.TotalSeconds} s";
            _alerts.Show(new Alert(string.Format(_loc("Alert.TimerDone"), label), null, AlertKind.Timer, null, null, true));
        }

        public void Dispose()
        {
            foreach (var s in _subs) s.Dispose();
        }
    }
}
