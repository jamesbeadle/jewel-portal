using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Parties;

/// <summary>
/// Adds or updates a person on a party's contact book. Exactly one contact may be primary — the
/// party's To correspondent — so marking one demotes any other (its routing drops to Cc so the
/// person keeps receiving correspondence rather than silently vanishing). The party's legacy
/// single contact-email field is kept in step with the primary until that field is retired.
/// </summary>
public sealed class UpsertPartyContactHandler : ICommandHandler<UpsertPartyContact, PartyContact>
{
    private readonly JpmsContext context;
    public UpsertPartyContactHandler(JpmsContext context) { this.context = context; }

    public async Task<PartyContact> HandleAsync(UpsertPartyContact command, CancellationToken cancellationToken)
    {
        await EnsurePartyExistsAsync(command.PartyKind, command.PartyId, cancellationToken);

        PartyContactEntity? entity = null;
        if (!string.IsNullOrWhiteSpace(command.PartyContactId))
        {
            entity = await context.PartyContacts.FirstOrDefaultAsync(
                c => c.PartyContactId == command.PartyContactId
                    && c.PartyKind == (int)command.PartyKind && c.PartyId == command.PartyId,
                cancellationToken);
        }

        if (entity is null)
        {
            entity = new PartyContactEntity
            {
                PartyContactId = PartyContactMapping.NextId(),
                PartyKind = (int)command.PartyKind,
                PartyId = command.PartyId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.PartyContacts.Add(entity);
        }

        entity.Name = command.Name.Trim();
        entity.Email = command.Email.Trim();
        entity.JobTitle = string.IsNullOrWhiteSpace(command.JobTitle) ? null : command.JobTitle.Trim();
        // A primary contact is by definition the To correspondent.
        entity.DefaultRouting = command.IsPrimary ? (int)CorrespondenceRouting.To : (int)command.DefaultRouting;
        entity.IsPrimary = command.IsPrimary;

        if (command.IsPrimary)
        {
            var previousPrimaries = await context.PartyContacts
                .Where(c => c.PartyKind == (int)command.PartyKind && c.PartyId == command.PartyId
                    && c.IsPrimary && c.PartyContactId != entity.PartyContactId)
                .ToListAsync(cancellationToken);
            foreach (var previous in previousPrimaries)
            {
                previous.IsPrimary = false;
                if (previous.DefaultRouting == (int)CorrespondenceRouting.To)
                    previous.DefaultRouting = (int)CorrespondenceRouting.Cc;
            }

            await SyncLegacyContactFieldsAsync(command.PartyKind, command.PartyId, entity, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private async Task EnsurePartyExistsAsync(PartyKind kind, string partyId, CancellationToken ct)
    {
        var exists = kind == PartyKind.Architect
            ? await context.Architects.AnyAsync(a => a.ArchitectId == partyId, ct)
            : await context.Clients.AnyAsync(c => c.ClientId == partyId, ct);
        if (!exists) throw new InvalidOperationException($"{kind.DisplayName()} '{partyId}' not found.");
    }

    // Older read paths still resolve through ClientEntity.PrimaryContactEmail /
    // ArchitectEntity.ContactEmail, so the primary contact keeps those fields current.
    private async Task SyncLegacyContactFieldsAsync(
        PartyKind kind, string partyId, PartyContactEntity primary, CancellationToken ct)
    {
        if (kind == PartyKind.Architect)
        {
            var architect = await context.Architects.FindAsync(new object[] { partyId }, ct);
            if (architect is null) return;
            architect.ContactName = primary.Name;
            architect.ContactEmail = primary.Email;
        }
        else
        {
            var client = await context.Clients.FindAsync(new object[] { partyId }, ct);
            if (client is null) return;
            client.PrimaryContactName = primary.Name;
            client.PrimaryContactEmail = primary.Email;
        }
    }
}
