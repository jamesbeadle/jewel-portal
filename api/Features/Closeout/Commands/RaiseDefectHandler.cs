using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class RaiseDefectHandler : ICommandHandler<RaiseDefect, Defect>
{
    private readonly JpmsContext context;
    public RaiseDefectHandler(JpmsContext context) { this.context = context; }

    public async Task<Defect> HandleAsync(RaiseDefect command, CancellationToken cancellationToken)
    {
        // Global sequence (like to-do numbers): max + 1, never a row count — deleted rows must
        // not re-issue a number, because the number is the mailbox tag stem ("JPMS/DEF-0001").
        var nextNumber = (await context.Defects.MaxAsync(d => (int?)d.Number, cancellationToken) ?? 0) + 1;

        var entity = new DefectEntity
        {
            DefectId = CloseoutIdentifierFactory.NextDefectId(),
            ProjectId = command.ProjectId,
            Number = nextNumber,
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
