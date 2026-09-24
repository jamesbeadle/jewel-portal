using System.Globalization;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiSkillHistoryTools
{
    private const string DayFormat = "yyyy-MM-dd";

    private static object Summary(SkillVersion version) => new
    {
        version = version.Version,
        name = version.DisplayName,
        writtenBy = version.WrittenByEmail,
        writtenAt = version.WrittenAt,
        replacedAt = version.ReplacedAt,
        current = version.IsCurrent,
        characters = version.Characters,
        pinned = version.Pinned,
        active = version.IsActive
    };

    /// <summary>A date alone means the end of that day — what stood by close of business.</summary>
    private static DateTimeOffset? MomentOf(string text)
    {
        var trimmed = text.Trim();
        var isADay = DateOnly.TryParseExact(trimmed, DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day);
        if (isADay) return EndOf(day);

        var isAMoment = DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var moment);
        return isAMoment ? moment : null;
    }

    private static DateTimeOffset EndOf(DateOnly day)
    {
        var nextMidnight = new DateTimeOffset(day.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return nextMidnight.AddTicks(-1);
    }
}
