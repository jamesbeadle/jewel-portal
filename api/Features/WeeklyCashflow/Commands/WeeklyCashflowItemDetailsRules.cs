using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

/// <summary>The one set of rules for an item's editable face, shared by create and update so the
/// two routes cannot drift apart — same shape as CalendarEventDetailsRules.</summary>
internal static class WeeklyCashflowItemDetailsRules
{
    public static ValidationOutcome Check(WeeklyCashflowItemDetails details)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(details.Name))
            errors.Add("A name is required.");
        if (details.Name?.Length > 200)
            errors.Add("The name is too long (200 characters at most).");
        if (details.Amount <= 0)
            errors.Add("The amount must be more than zero.");
        if (!Enum.IsDefined(details.Category))
            errors.Add("Unknown category.");
        if (!Enum.IsDefined(details.Recurrence))
            errors.Add("Unknown recurrence.");
        if (details.LastDueOn is { } last && last < details.FirstDueOn)
            errors.Add("The last date cannot be before the first.");
        if (details.LastDueOn is not null && details.Recurrence == WeeklyCashflowRecurrence.OneOff)
            errors.Add("A one-off item has no last date — clear it or make the item recurring.");
        if (details.Notes?.Length > 1000)
            errors.Add("The notes are too long (1,000 characters at most).");

        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }

    /// <summary>Writes the editable face onto the entity. Dates are normalised to midnight UTC
    /// (the SiteClock rule) so the maths' date-only keys are stable whatever a client sent.</summary>
    public static void Apply(WeeklyCashflowItemEntity entity, WeeklyCashflowItemDetails details)
    {
        entity.Name = details.Name.Trim();
        entity.Category = (int)details.Category;
        entity.Amount = details.Amount;
        entity.Recurrence = (int)details.Recurrence;
        entity.FirstDueOn = new DateTimeOffset(details.FirstDueOn.UtcDateTime.Date, TimeSpan.Zero);
        entity.LastDueOn = details.LastDueOn is { } last
            ? new DateTimeOffset(last.UtcDateTime.Date, TimeSpan.Zero)
            : null;
        entity.Notes = string.IsNullOrWhiteSpace(details.Notes) ? null : details.Notes.Trim();
    }
}
