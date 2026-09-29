using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class RaiseDefectHandler : ICommandHandler<RaiseDefect, Defect>
{
    private readonly JpmsContext context;
    public RaiseDefectHandler(JpmsContext context) { this.context = context; }

    public async Task<Defect> HandleAsync(RaiseDefect command, CancellationToken cancellationToken)
    {
        var nextNumber = await DefectNumbers.NextAsync(context, cancellationToken);

        // The picked directory record, or the legacy address promoted to one when it matches.
        var subcontractorId = await DefectSupplierLookup.ResolveAsync(
            context, command.SubcontractorId, command.AssignedToEmail ?? "", cancellationToken);

        var entity = new DefectEntity
        {
            DefectId = CloseoutIdentifierFactory.NextDefectId(),
            ProjectId = command.ProjectId,
            Number = nextNumber,
            Description = command.Description,
            Location = command.Location,
            AssignedToEmail = command.AssignedToEmail ?? "",
            SubcontractorId = subcontractorId,
            Status = (int)DefectStatus.Open,
            RaisedAt = DateTimeOffset.UtcNow,
            ResolvedAt = null
        };
        context.Defects.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(await DefectSupplierLookup.OneAsync(context, entity.SubcontractorId, cancellationToken));
    }
}
