using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class SetNextValuationDateHandler
    : ICommandHandler<SetNextValuationDate, Project>
{
    private readonly JpmsContext context;

    public SetNextValuationDateHandler(JpmsContext context) { this.context = context; }

    public async Task<Project> HandleAsync(SetNextValuationDate command, CancellationToken cancellationToken)
    {
        var entity = await context.Projects.FindAsync(new object[] { command.ProjectId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        // Normalise to a pure date at UTC midnight — this is a calendar date, not a moment in time.
        entity.NextExpectedValuationDate = command.NextExpectedValuationDate is { } value
            ? new DateTimeOffset(value.Date, TimeSpan.Zero)
            : null;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
