using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Lads.Commands;

public sealed class UpdateLadClaimHandler : ICommandHandler<UpdateLadClaim, LadClaim>
{
    private readonly JpmsContext context;
    public UpdateLadClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<LadClaim> HandleAsync(UpdateLadClaim command, CancellationToken cancellationToken)
    {
        var entity = await context.LadClaims.FirstOrDefaultAsync(l => l.LadClaimId == command.LadClaimId, cancellationToken)
            ?? throw new InvalidOperationException($"LADs claim '{command.LadClaimId}' not found.");

        entity.Title = Clamp(command.Title.Trim(), 256);
        entity.Description = Clamp(command.Description?.Trim() ?? "", 2048);
        entity.PeriodFrom = command.PeriodFrom;
        entity.PeriodTo = command.PeriodTo;
        entity.DaysClaimed = Math.Max(0, command.DaysClaimed);
        entity.RatePerWeek = command.RatePerWeek;
        entity.Amount = command.Amount;
        entity.Status = (int)command.Status;
        if (command.RaisedAt is not null) entity.RaisedAt = command.RaisedAt.Value;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
