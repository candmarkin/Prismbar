using System.Globalization;

namespace Prismbar.Core.Search;

/// <summary>Ranks names against a query: prefix 100, word prefix 80, acronym 60, substring 40, subsequence 20.</summary>
public static class SearchMatcher
{
    private static readonly CompareInfo Cmp = CultureInfo.InvariantCulture.CompareInfo;
    private const CompareOptions Opts = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    public static int Score(string name, string query)
    {
        query = query.Trim();
        if (query.Length == 0 || string.IsNullOrEmpty(name)) return 0;
        if (Cmp.IsPrefix(name, query, Opts)) return 100;

        var words = name.Split([' ', '-', '_', '.', '(', ')'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Any(w => Cmp.IsPrefix(w, query, Opts))) return 80;

        var initials = new string(words.Select(w => w[0]).ToArray());
        if (initials.Length >= query.Length && Cmp.IsPrefix(initials, query, Opts)) return 60;

        if (Cmp.IndexOf(name, query, Opts) >= 0) return 40;

        int pos = 0;
        foreach (var ch in query)
        {
            if (ch == ' ') continue;
            bool found = false;
            while (pos < name.Length)
            {
                if (Cmp.Compare(name, pos, 1, ch.ToString(), 0, 1, Opts) == 0) { pos++; found = true; break; }
                pos++;
            }
            if (!found) return 0;
        }
        return 20;
    }

    public static List<T> Rank<T>(IEnumerable<T> items, Func<T, string> name, string query, int max)
    {
        return items
            .Select(i => (item: i, n: name(i), score: Score(name(i), query)))
            .Where(t => t.score > 0)
            .OrderByDescending(t => t.score)
            .ThenBy(t => t.n.Length)
            .ThenBy(t => t.n, StringComparer.CurrentCultureIgnoreCase)
            .Take(max)
            .Select(t => t.item)
            .ToList();
    }
}
