using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class StartValuationClaimHandler : ICommandHandler<StartValuationClaim, ValuationClaim>
{
    private readonly JpmsContext context;
    public StartValuationClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationClaim> HandleAsync(StartValuationClaim command, CancellationToken cancellationToken)
    {
        var entity = new ValuationClaimEntity
        {
            ValuationClaimId = CommercialIdentifierFactory.NextValuationClaimId(),
            ProjectId = command.ProjectId,
            ClaimNumber = command.ClaimNumber,
            ClaimDate = command.ClaimDate,
            Status = (int)ValuationClaimStatus.Draft,
            RetentionPercent = command.RetentionPercent,
            RetentionReleasePercent = command.RetentionReleasePercent,
            PreapprovedAt = null,
            ConfirmedAt = null
            // Summary totals stay zero until the claim is preapproved / confirmed.
        };
        context.ValuationClaims.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
