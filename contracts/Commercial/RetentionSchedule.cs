using Jewel.JPMS.Models;

namespace Jewel.JPMS.Commercial;

// One release milestone as the schedule presents it: the amount (forecast until confirmed,
// then the frozen actual), when it falls due, and whether it has happened.
public sealed record RetentionScheduleLine(
    RetentionMilestone Milestone,
    decimal Amount,
    DateTimeOffset? DueOn,        // null until a practical-completion date is set
    bool IsConfirmed,
    DateTimeOffset? ConfirmedAt);

// The project's retention position calculated from its terms and the valuation figures —
// pure maths, mirroring the By France workbook: held = retention % x works complete;
// each release forecast = its % of the revised contract sum (works complete equals the
// revised sum once the project completes, so the completion release is half the 5% held).
public sealed record RetentionSchedule(
    decimal HeldToDate,           // retention % x total works complete — earned but withheld
    decimal ReleasedToDate,       // confirmed releases only — money actually freed
    decimal Outstanding,          // held less released — still locked up
    RetentionScheduleLine CompletionRelease,
    RetentionScheduleLine FinalRelease)
{
    public static RetentionSchedule For(
        ProjectRetention retention,
        decimal totalWorksComplete,
        decimal revisedContractSum)
    {
        var heldToDate = ValuationCalculations.RetentionHeld(totalWorksComplete, retention.RetentionPercent);

        var completionConfirmed = retention.CompletionReleaseConfirmedAt is not null;
        var completionAmount = completionConfirmed
            ? retention.CompletionReleaseAmount
            : ValuationCalculations.RetentionReleased(revisedContractSum, retention.CompletionReleasePercent);

        // The final release is whatever the completion release leaves behind of the full
        // retention pot (at completion the pot is retention % of the revised contract sum).
        var finalConfirmed = retention.FinalReleaseConfirmedAt is not null;
        var finalAmount = finalConfirmed
            ? retention.FinalReleaseAmount
            : ValuationCalculations.RetentionHeld(revisedContractSum, retention.RetentionPercent) - completionAmount;

        var completionDue = retention.PracticalCompletionAt;
        var finalDue = retention.PracticalCompletionAt?.AddMonths(retention.DefectsPeriodMonths);

        var releasedToDate =
            (completionConfirmed ? retention.CompletionReleaseAmount : 0m) +
            (finalConfirmed ? retention.FinalReleaseAmount : 0m);

        return new(
            HeldToDate: heldToDate,
            ReleasedToDate: releasedToDate,
            Outstanding: heldToDate - releasedToDate,
            CompletionRelease: new(
                RetentionMilestone.Completion, completionAmount, completionDue,
                completionConfirmed, retention.CompletionReleaseConfirmedAt),
            FinalRelease: new(
                RetentionMilestone.DefectsPeriodEnd, finalAmount, finalDue,
                finalConfirmed, retention.FinalReleaseConfirmedAt));
    }
}
