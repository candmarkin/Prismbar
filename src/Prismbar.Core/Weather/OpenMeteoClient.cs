using System.Globalization;
using System.Text.Json;
using Prismbar.Core.Settings;

namespace Prismbar.Core.Weather;

public static class WeatherCodes
{
    public static (string Icon, string LabelKey) Map(int code, bool isDay) => code switch
    {
        0 => (isDay ? "sun" : "moon", "Weather.Clear"),
        1 or 2 => (isDay ? "cloud-sun" : "moon", "Weather.PartlyCloudy"),
        3 => ("cloud", "Weather.Cloudy"),
        45 or 48 => ("cloud-fog", "Weather.Fog"),
        51 or 53 or 55 or 56 or 57 => ("cloud-drizzle", "Weather.Drizzle"),
        61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => ("cloud-rain", "Weather.Rain"),
        71 or 73 or 75 or 77 or 85 or 86 => ("cloud-snow", "Weather.Snow"),
        95 or 96 or 99 => ("cloud-lightning", "Weather.Thunderstorm"),
        _ => ("cloud", "Weather.Cloudy"),
    };
}

public sealed record CityResult(string Name, string? Admin1, string? Country, double Latitude, double Longitude)
{
    public string Display => string.Join(", ", new[] { Name, Admin1, Country }.Where(s => !string.IsNullOrEmpty(s)));
}

public sealed record DailyForecast(DateOnly Date, int Code, double Max, double Min);

public sealed record Forecast(double Temperature, int Code, bool IsDay, IReadOnlyList<DailyForecast> Daily);

public sealed class OpenMeteoClient(HttpClient http)
{
    public async Task<IReadOnlyList<CityResult>> SearchCityAsync(string name, string lang, CancellationToken ct = default)
    {
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(name)}&count=5&language={lang}&format=json";
        using var doc = JsonDocument.Parse(await http.GetStringAsync(url, ct));
        if (!doc.RootElement.TryGetProperty("results", out var results)) return [];
        return results.EnumerateArray().Select(r => new CityResult(
            r.GetProperty("name").GetString() ?? "",
            r.TryGetProperty("admin1", out var a) ? a.GetString() : null,
            r.TryGetProperty("country", out var c) ? c.GetString() : null,
            r.GetProperty("latitude").GetDouble(),
            r.GetProperty("longitude").GetDouble())).ToList();
    }

    public async Task<Forecast> GetForecastAsync(double lat, double lon, TemperatureUnit unit, CancellationToken ct = default)
    {
        var inv = CultureInfo.InvariantCulture;
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(inv)}&longitude={lon.ToString(inv)}" +
                  "&current=temperature_2m,weather_code,is_day&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=5" +
                  (unit == TemperatureUnit.Fahrenheit ? "&temperature_unit=fahrenheit" : "");
        using var doc = JsonDocument.Parse(await http.GetStringAsync(url, ct));
        var cur = doc.RootElement.GetProperty("current");
        var daily = doc.RootElement.GetProperty("daily");
        var times = daily.GetProperty("time").EnumerateArray().Select(t => DateOnly.Parse(t.GetString()!, inv)).ToList();
        var codes = daily.GetProperty("weather_code").EnumerateArray().Select(e => e.GetInt32()).ToList();
        var max = daily.GetProperty("temperature_2m_max").EnumerateArray().Select(e => e.GetDouble()).ToList();
        var min = daily.GetProperty("temperature_2m_min").EnumerateArray().Select(e => e.GetDouble()).ToList();
        var days = times.Select((d, i) => new DailyForecast(d, codes[i], max[i], min[i])).ToList();
        return new Forecast(cur.GetProperty("temperature_2m").GetDouble(), cur.GetProperty("weather_code").GetInt32(),
            cur.GetProperty("is_day").GetInt32() == 1, days);
    }
}
