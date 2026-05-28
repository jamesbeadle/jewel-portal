using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class RecordQsAccrualHandler : ICommandHandler<RecordQsAccrual, QsAccrual>
{
    private readonly JpmsContext context;
    public RecordQsAccrualHandler(JpmsContext context) { this.context = context; }

    public async Task<QsAccrual> HandleAsync(RecordQsAccrual command, CancellationToken cancellationToken)
    {
        var entity = new QsAccrualEntity
        {
            QsAccrualId = CvrIdentifierFactory.NextQsAccrualId(),
            ProjectId = command.ProjectId,
            Category = command.Category,
            Description = command.Description,
            AddAmount = command.AddAmount,
            OmitAmount = command.OmitAmount,
            LiabilityAmount = command.LiabilityAmount,
            SignedOffByEmail = command.SignedOffByEmail,
            SignedOffAt = DateTimeOffset.UtcNow
        };
        context.QsAccruals.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
