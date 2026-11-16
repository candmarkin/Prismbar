using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Prismbar.Core.Settings;

namespace Prismbar.Localization
{
    /// <summary>String tables live in Languages/*.xaml and are merged at index 0 of the app resources.</summary>
    public static class Loc
    {
        public static string CurrentLanguage { get; private set; } = "en-US";

        public static string Resolve(string language) => language switch
        {
            "pt-BR" => "pt-BR",
            "en-US" => "en-US",
            _ => CultureInfo.CurrentUICulture.Name.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? "pt-BR" : "en-US",
        };

        public static void Apply(string language)
        {
            CurrentLanguage = Resolve(language);
            var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/Languages/{CurrentLanguage}.xaml") };
            var merged = Application.Current.Resources.MergedDictionaries;
            var existing = merged.FirstOrDefault(d => d.Source?.OriginalString.Contains("Languages/") == true);
            if (existing is not null) merged.Remove(existing);
            merged.Insert(0, dict);
        }

        public static string Get(string key) => Application.Current.TryFindResource(key) as string ?? key;

        public static string Format(string key, params object?[] args) => string.Format(Get(key), args);

        /// <summary>Two-letter code for web APIs.</summary>
        public static string ApiLanguage => CurrentLanguage == "pt-BR" ? "pt" : "en";
    }
}

namespace Prismbar.Glass
{
    public sealed record GlassStyleParams(
        bool IsGlass, Color Solid, Color Border, double Refraction, double BezelPx, double FrostPx,
        double Chromatic, Color Tint, double Specular, double EdgeShade, bool DarkForeground);

    public static class GlassStyles
    {
        private static Color Hex(uint argb) => Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

        public static GlassStyleParams Get(DockStyle style) => style switch
        {
            DockStyle.Glass => new(true, Colors.Transparent, Hex(0x1FFFFFFF), 1.0, 16, 0, 0.35, Hex(0x14FFFFFF), 0.55, 0.35, false),
            DockStyle.IosGlass => new(true, Colors.Transparent, Hex(0x1FFFFFFF), 0.6, 12, 10, 0.15, Hex(0x30FFFFFF), 0.35, 0.25, false),
            DockStyle.MidnightGlass => new(true, Colors.Transparent, Hex(0x1FFFFFFF), 0.8, 14, 4, 0.25, Hex(0x8C0B0F1A), 0.35, 0.30, false),
            DockStyle.Midnight => new(false, Hex(0xEB0B0F1A), Hex(0x1FFFFFFF), 0, 0, 0, 0, Colors.Transparent, 0, 0, false),
            DockStyle.White => new(false, Hex(0xF2F7F7F8), Hex(0x14000000), 0, 0, 0, 0, Colors.Transparent, 0, 0, true),
            _ => new(false, Hex(0xEB000000), Hex(0x1FFFFFFF), 0, 0, 0, 0, Colors.Transparent, 0, 0, false),
        };

        /// <summary>Fallback popover/dock fill when glass is unavailable (no shader yet or GPU lacks ps_3_0).</summary>
        public static readonly Color GlassFallback = Hex(0xE61C1C1E);
    }
}

namespace Prismbar.Theming
{
    public static class ThemeManager
    {
        public static void Apply(DockStyle style)
        {
            var dark = Glass.GlassStyles.Get(style).DarkForeground;
            var uri = new Uri($"pack://application:,,,/Resources/Theme.{(dark ? "Dark" : "Light")}.xaml");
            var merged = Application.Current.Resources.MergedDictionaries;
            var existing = merged.FirstOrDefault(d => d.Source?.OriginalString.Contains("Resources/Theme.Light") == true
                                                   || d.Source?.OriginalString.Contains("Resources/Theme.Dark") == true);
            if (existing is not null) merged.Remove(existing);
            merged.Add(new ResourceDictionary { Source = uri });
        }
    }
}

namespace Prismbar.Dock
{
    public sealed record DockMetrics(
        int IconSize, double BarHeight, double EdgeGap, double Headroom, double WindowHeight, double CornerRadius,
        double AppSpacing, bool IsPill, bool Magnify, bool IsTop)
    {
        public static DockMetrics From(AppSettings s)
        {
            int size = s.IconSize;
            bool pill = s.Shape == DockShape.Pill;
            double bar = size + 16;
            double gap = pill ? 10 : 0;
            double headroom = pill && s.Magnification ? Math.Ceiling(size * 0.35) + 4 : 4;
            double window = pill ? bar + gap + headroom : bar;
            double radius = pill ? Math.Round(bar * 0.36) : 0;
            return new DockMetrics(size, bar, gap, headroom, window, radius, Math.Round(size * 0.18), pill,
                pill && s.Magnification, s.Edge == DockEdge.Top);
        }
    }
}
