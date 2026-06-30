using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class InviteSubcontractorsToBidPackageValidation
{
    public ValidationOutcome Check(InviteSubcontractorsToBidPackage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BidPackageId)) errors.Add("BidPackageId is required.");
        if (command.SubcontractorIds is null || command.SubcontractorIds.Count == 0)
            errors.Add("At least one subcontractor is required.");
        else if (command.SubcontractorIds.Any(string.IsNullOrWhiteSpace))
            errors.Add("Subcontractor ids must not be blank.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
