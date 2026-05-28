using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class UpdateDefectHandler : ICommandHandler<UpdateDefect, Defect>
{
    private readonly JpmsContext context;
    public UpdateDefectHandler(JpmsContext context) { this.context = context; }

    public async Task<Defect> HandleAsync(UpdateDefect command, CancellationToken cancellationToken)
    {
        var entity = await context.Defects.FindAsync(new object[] { command.DefectId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Defect {command.DefectId} not found.");
        entity.Description = command.Description;
        entity.Location = command.Location;
        entity.AssignedToEmail = command.AssignedToEmail;
        var wasResolved = (DefectStatus)entity.Status == DefectStatus.Resolved || (DefectStatus)entity.Status == DefectStatus.Verified;
        entity.Status = (int)command.Status;
        if (!wasResolved && (command.Status == DefectStatus.Resolved || command.Status == DefectStatus.Verified))
            entity.ResolvedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
