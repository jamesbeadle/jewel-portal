using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class CreateProjectShellHandler
    : ICommandHandler<CreateProjectShell, Project>
{
    private readonly JpmsContext context;

    public CreateProjectShellHandler(JpmsContext context) { this.context = context; }

    public async Task<Project> HandleAsync(CreateProjectShell command, CancellationToken cancellationToken)
    {
        var entity = new ProjectEntity
        {
            ProjectId = ProjectIdentifierFactory.Next(),
            Reference = command.Reference,
            Name = command.Name,
            ClientName = command.ClientName,
            Organisation = (int)command.Organisation,
            Stage = (int)command.Stage,
            ProjectManagerEmail = command.ProjectManagerEmail,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Projects.Add(entity);
        // Every project starts with the standard drawing-folder set; one SaveChanges covers both.
        await StandardDrawingFolders.AddMissingAsync(context, entity.ProjectId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
