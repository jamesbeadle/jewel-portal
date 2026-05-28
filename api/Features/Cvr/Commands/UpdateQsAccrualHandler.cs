using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class UpdateQsAccrualHandler : ICommandHandler<UpdateQsAccrual, QsAccrual>
{
    private readonly JpmsContext context;
    public UpdateQsAccrualHandler(JpmsContext context) { this.context = context; }

    public async Task<QsAccrual> HandleAsync(UpdateQsAccrual command, CancellationToken cancellationToken)
    {
        var entity = await context.QsAccruals.FindAsync(new object[] { command.QsAccrualId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"QS accrual {command.QsAccrualId} not found.");
        entity.Category = command.Category;
        entity.Description = command.Description;
        entity.AddAmount = command.AddAmount;
        entity.OmitAmount = command.OmitAmount;
        entity.LiabilityAmount = command.LiabilityAmount;
        entity.SignedOffByEmail = command.SignedOffByEmail;
        entity.SignedOffAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
