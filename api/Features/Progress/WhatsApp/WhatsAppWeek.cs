namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>The reporting week: Friday to the following Thursday, named by the Thursday it ends
/// on — the period the Contractor's Report covers.</summary>
public sealed record WhatsAppWeek(DateOnly Start, DateOnly End)
{
    private const int DaysInWeek = 7;

    public static WhatsAppWeek EndingOn(DateOnly thursday)
    {
        if (thursday.DayOfWeek != DayOfWeek.Thursday)
            throw new ArgumentException($"{thursday:dddd d MMMM yyyy} is not a Thursday — the week ends on one.", nameof(thursday));
        return new WhatsAppWeek(thursday.AddDays(1 - DaysInWeek), thursday);
    }

    public bool Contains(DateOnly day) => day >= Start && day <= End;

    public IEnumerable<DateOnly> Days()
    {
        for (var day = Start; day <= End; day = day.AddDays(1)) yield return day;
    }
}
