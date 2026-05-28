using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class UpdateProjectDetailsHandler
    : ICommandHandler<UpdateProjectDetails, Project>
{
    private readonly JpmsContext context;

    public UpdateProjectDetailsHandler(JpmsContext context) { this.context = context; }

    public async Task<Project> HandleAsync(UpdateProjectDetails command, CancellationToken cancellationToken)
    {
        var entity = await context.Projects.FindAsync(new object[] { command.ProjectId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        entity.Reference = command.Reference;
        entity.Name = command.Name;
        entity.ClientName = command.ClientName;
        entity.Organisation = (int)command.Organisation;
        entity.Stage = (int)command.Stage;
        entity.ProjectManagerEmail = command.ProjectManagerEmail;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
