using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>
/// Sanity checks for manually entered weather conditions, shared by the create/update
/// progress-update validations. Weather is always optional — the rules only bound what was given.
/// </summary>
internal static class ProgressWeatherRules
{
    public static void Check(ProgressWeather? weather, List<string> errors)
    {
        if (weather is null) return;

        if ((weather.Summary?.Length ?? 0) > 256)
            errors.Add("Weather summary must be 256 characters or fewer.");
        if (weather.TempHighC is < -50 or > 60)
            errors.Add("Weather high temperature must be between -50°C and 60°C.");
        if (weather.TempLowC is < -50 or > 60)
            errors.Add("Weather low temperature must be between -50°C and 60°C.");
        if (weather is { TempHighC: { } high, TempLowC: { } low } && low > high)
            errors.Add("Weather low temperature cannot exceed the high temperature.");
        if (weather.WindMph is < 0 or > 250)
            errors.Add("Weather wind speed must be between 0 and 250 mph.");
        if (weather.HumidityPercent is < 0 or > 100)
            errors.Add("Weather humidity must be between 0% and 100%.");
        if (weather.PrecipInches is < 0 or > 100)
            errors.Add("Weather precipitation must be between 0\" and 100\".");
    }
}
