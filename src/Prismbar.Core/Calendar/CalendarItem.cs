using System.Text.RegularExpressions;

namespace Prismbar.Core.Calendar;

public sealed record CalendarItem(
    string SourceId,
    string Uid,
    string Title,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool AllDay,
    string? Location,
    string? MeetingUrl,
    string ColorHex);

public static partial class MeetingLinkDetector
{
    [GeneratedRegex(@"https?://(meet\.google\.com/[a-z]{3}-[a-z]{4}-[a-z]{3}|[\w.-]*zoom\.us/j/\d+\S*|teams\.microsoft\.com/l/meetup-join/\S+|teams\.live\.com/meet/\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex MeetingRegex();

    public static string? Find(params string?[] texts)
    {
        foreach (var t in texts)
        {
            if (string.IsNullOrEmpty(t)) continue;
            var m = MeetingRegex().Match(t);
            if (m.Success) return m.Value;
        }
        return null;
    }
}
