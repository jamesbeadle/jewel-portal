using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// Sets a claim's free-text period name ("June 2026"). Allowed at any status — the name
// is bookkeeping, not a financial figure, so locked claims may still be renamed. An
// empty name clears it and the UI falls back to "Claim n".
public sealed class RenameValuationClaimHandler : ICommandHandler<RenameValuationClaim, ValuationClaim>
{
    private readonly JpmsContext context;
    public RenameValuationClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationClaim> HandleAsync(RenameValuationClaim command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation claim {command.ValuationClaimId} was not found.");

        var name = (command.Name ?? "").Trim();
        entity.Name = name.Length <= 128 ? name : name[..128];

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
