using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class AwardBidPackageValidation
{
    private readonly JpmsContext context;

    public AwardBidPackageValidation(JpmsContext context) { this.context = context; }

    public async Task<ValidationOutcome> CheckAsync(AwardBidPackage command, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BidPackageId)) errors.Add("BidPackageId is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (command.Value <= 0) errors.Add("Award value must be positive.");

        var hasExpiredCompliance = await context.ComplianceDocuments
            .Where(document => document.SubcontractorId == command.SubcontractorId)
            .AnyAsync(document => document.ExpiresAt != null && document.ExpiresAt < DateTimeOffset.UtcNow, cancellationToken);
        if (hasExpiredCompliance) errors.Add("Subcontractor has expired compliance documents.");

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
