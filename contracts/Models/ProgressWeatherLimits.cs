namespace Jewel.JPMS.Models;

/// <summary>
/// The bounds a recorded weather reading must sit within: the one statement the API's validation
/// and the form's inputs both read. Temperatures in °C, wind in mph, precipitation in inches.
/// </summary>
public static class ProgressWeatherLimits
{
    public const int SummaryLength = 256;
    public const int LowestTemperatureC = -50;
    public const int HighestTemperatureC = 60;
    public const int LowestWindMph = 0;
    public const int HighestWindMph = 250;
    public const int LowestHumidityPercent = 0;
    public const int HighestHumidityPercent = 100;
    public const decimal LowestPrecipInches = 0;
    public const decimal HighestPrecipInches = 100;
    public const decimal PrecipStepInches = 0.01m;
}
