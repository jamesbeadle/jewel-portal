namespace Jewel.JPMS.Contracts.Labour;

/// <summary>The day in chunks, as a site worker thinks of it — off, half a day, a full day — and
/// the hours one tap records. The half-hour steps stay for anything in between.</summary>
public static class WorkingDayChunks
{
    public const decimal Off = 0m;
    public static readonly decimal FullDay = ForecastRules.StandardHoursPerDay;
    public static readonly decimal HalfDay = FullDay / 2;
}
