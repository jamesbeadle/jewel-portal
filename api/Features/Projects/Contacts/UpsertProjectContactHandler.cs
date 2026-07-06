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

        // Routing wins when supplied; older callers still sending only the legacy flag get the
        // equivalent routing. The flag itself is kept derived so nothing reading it drifts.
        var routing = command.Routing ?? (command.ReceivesRequests ? CorrespondenceRouting.To : CorrespondenceRouting.None);

        // A linked row is a per-project routing override for a person on a party's contact book:
        // its name/email snapshot comes from the party contact (read paths read through anyway).
        PartyContactEntity? source = null;
        if (!string.IsNullOrWhiteSpace(command.PartyContactId))
        {
            source = await context.PartyContacts.FirstOrDefaultAsync(
                p => p.PartyContactId == command.PartyContactId, cancellationToken);
            if (source is null)
                throw new InvalidOperationException($"Party contact '{command.PartyContactId}' not found.");
        }

        entity.Name = source?.Name ?? command.Name.Trim();
        entity.Email = source?.Email ?? command.Email.Trim();
        entity.Organisation = string.IsNullOrWhiteSpace(command.Organisation) ? null : command.Organisation.Trim();
        entity.Role = (int)command.Role;
        entity.Routing = (int)routing;
        entity.ReceivesRequests = routing == CorrespondenceRouting.To;
        entity.PartyContactId = source?.PartyContactId;

        await context.SaveChangesAsync(cancellationToken);
        return source is null ? entity.ToModel() : entity.ToModel(source);
    }
}
