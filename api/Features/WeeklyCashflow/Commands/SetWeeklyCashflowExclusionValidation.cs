using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class SetWeeklyCashflowExclusionValidation
{
    // Exclusions exist for the Xero-fed rows — a bill (or receipt) already covered by a manual
    // item. Manual items have Archive for the same job, so "manual:" keys are refused rather
    // than given a second, confusing off-switch.
    private static readonly string[] KnownPrefixes = { "bill:", "receipt:" };

    public ValidationOutcome Check(SetWeeklyCashflowExclusion command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.PlacementKey))
            errors.Add("A placement key is required.");
        else if (command.PlacementKey.Length > 128)
            errors.Add("The placement key is too long (128 characters at most).");
        else if (!KnownPrefixes.Any(prefix => command.PlacementKey.StartsWith(prefix, StringComparison.Ordinal)))
            errors.Add("Only Xero-fed entries (bills and receipts) can be excluded — archive a manual item instead.");

        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
