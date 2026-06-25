using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;

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
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
