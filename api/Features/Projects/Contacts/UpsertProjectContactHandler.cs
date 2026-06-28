using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Projects.Contacts;

public sealed class UpsertProjectContactHandler : ICommandHandler<UpsertProjectContact, ProjectContact>
{
    private readonly JpmsContext context;
    public UpsertProjectContactHandler(JpmsContext context) { this.context = context; }

    public async Task<ProjectContact> HandleAsync(UpsertProjectContact command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        ProjectContactEntity? entity = null;
        if (!string.IsNullOrWhiteSpace(command.ContactId))
        {
            entity = await context.ProjectContacts
                .FirstOrDefaultAsync(c => c.ContactId == command.ContactId && c.ProjectId == command.ProjectId, cancellationToken);
        }

        if (entity is null)
        {
            entity = new ProjectContactEntity
            {
                ContactId = ProjectContactMapping.NextId(),
                ProjectId = command.ProjectId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.ProjectContacts.Add(entity);
        }

        entity.Name = command.Name.Trim();
        entity.Email = command.Email.Trim();
        entity.Organisation = string.IsNullOrWhiteSpace(command.Organisation) ? null : command.Organisation.Trim();
        entity.Role = (int)command.Role;
        entity.ReceivesRequests = command.ReceivesRequests;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
