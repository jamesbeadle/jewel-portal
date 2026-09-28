using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Progress.Updates;

/// <summary>
/// The recorded conditions as one compact line, only the parts recorded:
/// "Partly cloudy · 17°C / 9°C · Wind 6 mph · observed Mon 14 Sep, 08:00".
/// </summary>
public static class ProgressWeatherLine
{
    public static string Of(ProgressWeather weather)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(weather.Summary)) parts.Add(weather.Summary);
        var temperatures = TemperaturesOf(weather);
        if (temperatures is not null) parts.Add(temperatures);
        if (weather.WindMph is { } wind) parts.Add($"Wind {wind} mph");
        if (weather.HumidityPercent is { } humidity) parts.Add($"Humidity {humidity}%");
        if (weather.PrecipInches is { } precip) parts.Add($"Precip {precip:0.##}\"");
        if (weather.ObservedAt is { } observedAt) parts.Add($"observed {observedAt:ddd dd MMM, HH:mm}");
        return string.Join(" · ", parts);
    }

    private static string? TemperaturesOf(ProgressWeather weather) => (weather.TempHighC, weather.TempLowC) switch
    {
        ({ } high, { } low) => $"{high}°C / {low}°C",
        ({ } high, null) => $"{high}°C",
        (null, { } low) => $"{low}°C",
        _ => null
    };
}
