using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Shape checks for <see cref="SetXeroAllocation"/> ahead of the handler (2026-09-03, for the
/// connector's set_xero_allocation action — the HTTP endpoint only refuses an empty id list and
/// lets the handler's guards speak). Everything that needs the database (project exists, cost
/// centre active, split nets sum to the line) stays in the handler, exactly as over HTTP; this
/// catches the model-side mistakes with a message naming the field instead of a bare guard.
/// </summary>
public sealed class SetXeroAllocationValidation
{
    public ValidationOutcome Check(SetXeroAllocation command)
    {
        var errors = new List<string>();

        if (command.XeroLedgerLineIds is null || command.XeroLedgerLineIds.Count == 0
            || command.XeroLedgerLineIds.Any(string.IsNullOrWhiteSpace))
            errors.Add("xeroLedgerLineIds must list at least one ledger line id (from list_xero_ledger_lines).");

        var hasSplits = command.Splits is { Count: > 0 };
        switch (command.Action)
        {
            case XeroAllocationAction.Allocate:
                if (!hasSplits && string.IsNullOrWhiteSpace(command.ProjectId))
                    errors.Add("Allocate needs a projectId (or splits, each carrying its own project).");
                if (!hasSplits && string.IsNullOrWhiteSpace(command.CostCenterCode))
                    errors.Add("Allocate needs a costCenterCode (or splits, each carrying its own cost centre).");
                if (hasSplits && command.Splits!.Any(split => string.IsNullOrWhiteSpace(split.CostCenterCode)))
                    errors.Add("Every split entry needs a costCenterCode.");
                if (hasSplits && command.XeroLedgerLineIds is { Count: > 1 })
                    errors.Add("A split applies to one line at a time — send one xeroLedgerLineId with splits.");
                break;

            case XeroAllocationAction.AllocateToBucket:
                if (string.IsNullOrWhiteSpace(command.Bucket))
                    errors.Add($"AllocateToBucket needs a bucket: {string.Join(", ", XeroBuckets.All)}.");
                break;

            case XeroAllocationAction.AddDisputeMessage:
                if (string.IsNullOrWhiteSpace(command.Note))
                    errors.Add("AddDisputeMessage needs the message in note.");
                break;

            case XeroAllocationAction.SetProject:
                // A null projectId is a deliberate unset (clears the saved coding); nothing to check.
                break;
        }

        if (hasSplits && command.Action != XeroAllocationAction.Allocate)
            errors.Add("splits only apply to Allocate.");

        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
