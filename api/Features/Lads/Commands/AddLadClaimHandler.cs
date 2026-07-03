using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Lads.Commands;

public sealed class AddLadClaimHandler : ICommandHandler<AddLadClaim, LadClaim>
{
    private readonly JpmsContext context;
    public AddLadClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<LadClaim> HandleAsync(AddLadClaim command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        var nextNumber = (await context.LadClaims.MaxAsync(l => (int?)l.Number, cancellationToken) ?? 0) + 1;

        var entity = new LadClaimEntity
        {
            LadClaimId = LadsIdentifierFactory.Next(),
            ProjectId = command.ProjectId,
            Number = nextNumber,
            Title = Clamp(command.Title.Trim(), 256),
            Description = Clamp(command.Description?.Trim() ?? "", 2048),
            PeriodFrom = command.PeriodFrom,
            PeriodTo = command.PeriodTo,
            DaysClaimed = Math.Max(0, command.DaysClaimed),
            RatePerWeek = command.RatePerWeek,
            Amount = command.Amount,
            Status = (int)LadStatus.Notified,
            RaisedAt = command.RaisedAt ?? DateTimeOffset.UtcNow,
            CreatedByEmail = command.CreatedByEmail
        };

        context.LadClaims.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
