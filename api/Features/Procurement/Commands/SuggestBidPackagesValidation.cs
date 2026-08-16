using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SuggestBidPackagesValidation
{
    public ValidationOutcome Check(SuggestBidPackages command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        // Model is deliberately NOT validated here: AiModelCatalogue.Normalise degrades any
        // unknown/blank key to the cheap tier server-side — a stale client is never an error,
        // and never an accidental upgrade either.
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
