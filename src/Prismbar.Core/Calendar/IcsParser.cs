using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Prismbar.Core.Settings;
using IcalCalendar = Ical.Net.Calendar;

namespace Prismbar.Core.Calendar;

public static class IcsParser
{
    public static List<CalendarItem> Parse(string ics, CalendarSource meta, DateTimeOffset from, DateTimeOffset to, string untitledText)
    {
        var cal = IcalCalendar.Load(ics) ?? throw new FormatException("Not an iCalendar document");
        var end = new CalDateTime(to.UtcDateTime, "UTC");
        var items = new List<CalendarItem>();
        foreach (var occ in cal.GetOccurrences<CalendarEvent>(new CalDateTime(from.UtcDateTime, "UTC")).TakeWhileBefore(end))
        {
            if (occ.Source is not CalendarEvent evt) continue;
            DateTimeOffset start, finish;
            if (evt.IsAllDay)
            {
                var s = occ.Period.StartTime;
                var e = occ.Period.EffectiveEndTime ?? s.AddDays(1);
                start = new DateTimeOffset(new DateTime(s.Year, s.Month, s.Day, 0, 0, 0, DateTimeKind.Local));
                finish = new DateTimeOffset(new DateTime(e.Year, e.Month, e.Day, 0, 0, 0, DateTimeKind.Local));
            }
            else
            {
                start = new DateTimeOffset(occ.Period.StartTime.AsUtc, TimeSpan.Zero);
                finish = occ.Period.EffectiveEndTime is { } eet
                    ? new DateTimeOffset(eet.AsUtc, TimeSpan.Zero)
                    : start + evt.EffectiveDuration.ToTimeSpan(occ.Period.StartTime);
            }
            if (finish <= from) continue;
            items.Add(new CalendarItem(
                meta.Id.ToString(),
                evt.Uid ?? "",
                string.IsNullOrWhiteSpace(evt.Summary) ? untitledText : evt.Summary,
                start, finish, evt.IsAllDay, evt.Location,
                MeetingLinkDetector.Find(evt.Summary, evt.Location, evt.Description),
                meta.ColorHex));
        }
        return items.OrderBy(i => i.Start).ToList();
    }
}
