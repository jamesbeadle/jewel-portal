using System.Globalization;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>The optional manually entered weather fields of the multipart progress form
/// (<c>weatherSummary</c>, <c>weatherObservedAt</c> (ISO 8601), <c>weatherTempHighC</c>,
/// <c>weatherTempLowC</c>, <c>weatherWindMph</c>, <c>weatherHumidityPercent</c>,
/// <c>weatherPrecipInches</c>); null when none were sent.</summary>
internal static class ProgressWeatherForm
{
    public static ProgressWeather? Read(IFormCollection form)
    {
        var summary = form["weatherSummary"].ToString().Trim();
        DateTimeOffset? observedAt = DateTimeOffset.TryParse(form["weatherObservedAt"], out var parsedObservedAt) ? parsedObservedAt : null;
        var tempHighC = ReadInt(form, "weatherTempHighC");
        var tempLowC = ReadInt(form, "weatherTempLowC");
        var windMph = ReadInt(form, "weatherWindMph");
        var humidityPercent = ReadInt(form, "weatherHumidityPercent");
        // Invariant culture: the store formats the value, not the user's locale.
        decimal? precipInches = decimal.TryParse(
            form["weatherPrecipInches"], NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedPrecip) ? parsedPrecip : null;

        var isEmpty = string.IsNullOrWhiteSpace(summary) && observedAt is null
            && tempHighC is null && tempLowC is null && windMph is null
            && humidityPercent is null && precipInches is null;
        return isEmpty
            ? null
            : new ProgressWeather(summary, observedAt, tempHighC, tempLowC, windMph, humidityPercent, precipInches);
    }

    private static int? ReadInt(IFormCollection form, string field) =>
        int.TryParse(form[field], out var value) ? value : null;
}
