using static Jewel.JPMS.Models.ProgressWeatherLimits;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>
/// Sanity checks for manually entered weather conditions, shared by the create/update
/// progress-update validations. Weather is always optional — the rules only bound what was given,
/// against the same limits the form shows.
/// </summary>
internal static class ProgressWeatherRules
{
    public static void Check(ProgressWeather? weather, List<string> errors)
    {
        if (weather is null) return;

        if ((weather.Summary?.Length ?? 0) > SummaryLength)
            errors.Add($"Weather summary must be {SummaryLength} characters or fewer.");
        if (weather.TempHighC is < LowestTemperatureC or > HighestTemperatureC)
            errors.Add($"Weather high temperature must be between {LowestTemperatureC}°C and {HighestTemperatureC}°C.");
        if (weather.TempLowC is < LowestTemperatureC or > HighestTemperatureC)
            errors.Add($"Weather low temperature must be between {LowestTemperatureC}°C and {HighestTemperatureC}°C.");
        if (weather is { TempHighC: { } high, TempLowC: { } low } && low > high)
            errors.Add("Weather low temperature cannot exceed the high temperature.");
        if (weather.WindMph is < LowestWindMph or > HighestWindMph)
            errors.Add($"Weather wind speed must be between {LowestWindMph} and {HighestWindMph} mph.");
        if (weather.HumidityPercent is < LowestHumidityPercent or > HighestHumidityPercent)
            errors.Add($"Weather humidity must be between {LowestHumidityPercent}% and {HighestHumidityPercent}%.");
        if (weather.PrecipInches is < LowestPrecipInches or > HighestPrecipInches)
            errors.Add($"Weather precipitation must be between {LowestPrecipInches}\" and {HighestPrecipInches}\".");
    }
}
