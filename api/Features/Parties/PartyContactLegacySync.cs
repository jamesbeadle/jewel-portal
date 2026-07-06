using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Parties;

/// <summary>
/// Keeps a party's contact book in step when the legacy single-contact fields are written through
/// the older client/architect commands: the primary PartyContact is updated (or created) to match,
/// so both read paths agree whichever surface did the editing. Callers save; this only stages
/// changes on the context.
/// </summary>
public static class PartyContactLegacySync
{
    public static async Task SyncPrimaryAsync(
        JpmsContext context, PartyKind kind, string partyId, string? contactName, string? contactEmail,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(contactEmail)) return;

        var primary = await context.PartyContacts
            .Where(c => c.PartyKind == (int)kind && c.PartyId == partyId && c.IsPrimary)
            .FirstOrDefaultAsync(ct);

        if (primary is null)
        {
            context.PartyContacts.Add(new PartyContactEntity
            {
                PartyContactId = PartyContactMapping.NextId(),
                PartyKind = (int)kind,
                PartyId = partyId,
                Name = string.IsNullOrWhiteSpace(contactName) ? contactEmail.Trim() : contactName.Trim(),
                Email = contactEmail.Trim(),
                DefaultRouting = (int)CorrespondenceRouting.To,
                IsPrimary = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
            return;
        }

        primary.Name = string.IsNullOrWhiteSpace(contactName) ? contactEmail.Trim() : contactName.Trim();
        primary.Email = contactEmail.Trim();
        primary.DefaultRouting = (int)CorrespondenceRouting.To;
    }
}
