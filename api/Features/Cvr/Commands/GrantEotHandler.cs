using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class GrantEotHandler : ICommandHandler<GrantEot, Eot>
{
    private readonly JpmsContext context;
    public GrantEotHandler(JpmsContext context) { this.context = context; }

    public async Task<Eot> HandleAsync(GrantEot command, CancellationToken cancellationToken)
    {
        var entity = new EotEntity
        {
            EotId = CvrIdentifierFactory.NextEotId(),
            ProjectId = command.ProjectId,
            Reason = command.Reason,
            DaysGranted = command.DaysGranted,
            CommercialRecovery = command.CommercialRecovery,
            GrantedAt = DateTimeOffset.UtcNow
        };
        context.Eots.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
