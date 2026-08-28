using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class PlaceWeeklyCashflowEntryValidation
{
    // The known key prefixes — WeeklyCashflowMaths owns the vocabulary; this list guards the
    // table against arbitrary strings, not against stale-but-well-formed keys (those are
    // harmless: the grid simply never asks for them).
    private static readonly string[] KnownPrefixes = { "bill:", "receipt:", "manual:" };

    public ValidationOutcome Check(PlaceWeeklyCashflowEntry command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.PlacementKey))
            errors.Add("A placement key is required.");
        else if (command.PlacementKey.Length > 128)
            errors.Add("The placement key is too long (128 characters at most).");
        else if (!KnownPrefixes.Any(prefix => command.PlacementKey.StartsWith(prefix, StringComparison.Ordinal)))
            errors.Add("Unknown placement key shape.");

        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
