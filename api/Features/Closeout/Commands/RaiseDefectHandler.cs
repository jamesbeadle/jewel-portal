using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class RaiseDefectHandler : ICommandHandler<RaiseDefect, Defect>
{
    private readonly JpmsContext context;
    public RaiseDefectHandler(JpmsContext context) { this.context = context; }

    public async Task<Defect> HandleAsync(RaiseDefect command, CancellationToken cancellationToken)
    {
        var entity = new DefectEntity
        {
            DefectId = CloseoutIdentifierFactory.NextDefectId(),
            ProjectId = command.ProjectId,
            Description = command.Description,
            Location = command.Location,
            AssignedToEmail = command.AssignedToEmail,
            Status = (int)DefectStatus.Open,
            RaisedAt = DateTimeOffset.UtcNow,
            ResolvedAt = null
        };
        context.Defects.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
