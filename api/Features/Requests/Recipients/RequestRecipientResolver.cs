using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Recipients;

/// <summary>
/// The single source of truth for who a request's outbound document goes to. Every path that
/// emails a request document — the worker send, the Outlook draft, and the recipients preview —
/// resolves through here, so To/CC/BCC can never drift between paths.
///
/// Resolution:
///  - The corresponding party is the request's own party link, falling back to the project's.
///  - To: the party's contacts whose effective routing is To (the party's primary correspondent,
///    unless the project overrides routing per person), falling back to the party's legacy single
///    contact-email field, then to the project profile rows with Routing = To (the same fallback
///    the pre-profile send path used via ReceivesRequests).
///  - Cc/Bcc: the party's contacts whose effective routing is Cc/Bcc plus the project's ad-hoc
///    rows (e.g. internal Jewel staff) with that routing.
///  - Effective routing = the party contact's default, overridden by a project profile row linked
///    via PartyContactId (None removes the person for that project).
///  - De-duplication: an address keeps its highest visibility only (To beats Cc beats Bcc).
///
/// Lives in the api project and is compiled into the worker via a linked include (see
/// Jewel.JPMS.Worker.csproj), exactly like the entities and the document builder.
/// </summary>
public static class RequestRecipientResolver
{
    public static async Task<RequestRecipientSet> ResolveAsync(
        JpmsContext context, RequestEntity request, CancellationToken ct)
    {
        // The corresponding party: the request's own link wins; otherwise the project's.
        var partyKind = request.PartyKind;
        var partyId = request.PartyId;
        if (string.IsNullOrWhiteSpace(partyId))
        {
            var project = await context.Projects
                .Where(p => p.ProjectId == request.ProjectId)
                .Select(p => new { p.PartyKind, p.PartyId })
                .FirstOrDefaultAsync(ct);
            partyKind = project?.PartyKind ?? (int)PartyKind.Client;
            partyId = project?.PartyId;
        }

        var profileRows = await context.ProjectContacts
            .Where(c => c.ProjectId == request.ProjectId)
            .OrderBy(c => c.Role).ThenBy(c => c.Name)
            .ToListAsync(ct);

        var to = new List<CorrespondenceRecipient>();
        var cc = new List<CorrespondenceRecipient>();
        var bcc = new List<CorrespondenceRecipient>();

        if (!string.IsNullOrWhiteSpace(partyId))
        {
            var partyLabel = ((PartyKind)partyKind).DisplayName();
            var partyName = await ResolvePartyNameAsync(context, partyKind, partyId!, ct);

            var partyContacts = await context.PartyContacts
                .Where(c => c.PartyKind == partyKind && c.PartyId == partyId)
                .OrderByDescending(c => c.IsPrimary).ThenBy(c => c.Name)
                .ToListAsync(ct);

            // Per-project overrides, keyed by the party contact they re-route.
            var overrides = profileRows
                .Where(row => row.PartyContactId is not null)
                .ToDictionary(row => row.PartyContactId!, row => (CorrespondenceRouting)row.Routing);

            foreach (var contact in partyContacts)
            {
                var routing = overrides.TryGetValue(contact.PartyContactId, out var overridden)
                    ? overridden
                    : (CorrespondenceRouting)contact.DefaultRouting;
                if (string.IsNullOrWhiteSpace(contact.Email)) continue;

                var recipient = new CorrespondenceRecipient(
                    contact.Name, contact.Email.Trim(), routing, partyLabel, partyName);
                switch (routing)
                {
                    case CorrespondenceRouting.To: to.Add(recipient); break;
                    case CorrespondenceRouting.Cc: cc.Add(recipient); break;
                    case CorrespondenceRouting.Bcc: bcc.Add(recipient); break;
                }
            }

            // Legacy fallback: a party with no To contact on its book but a single contact-email
            // field still set (pre-seed data, or the seed was removed) keeps resolving as before.
            if (to.Count == 0)
            {
                var legacy = await ResolveLegacyPartyContactAsync(context, partyKind, partyId!, ct);
                if (legacy is not null)
                    to.Add(new CorrespondenceRecipient(
                        legacy.Value.Name, legacy.Value.Email, CorrespondenceRouting.To, partyLabel, partyName));
            }
        }

        // Ad-hoc profile rows (no party link). Linked rows resolve through the party pass above;
        // stale links — rows pointing at a contact no longer on the current party's book, e.g.
        // after the project's party changed — are ignored rather than resurrected with stale data.
        var partyProvidedTo = to.Count > 0;
        foreach (var row in profileRows)
        {
            if (row.PartyContactId is not null) continue;
            if (string.IsNullOrWhiteSpace(row.Email)) continue;

            var routing = (CorrespondenceRouting)row.Routing;
            var recipient = new CorrespondenceRecipient(
                row.Name, row.Email.Trim(), routing,
                ((ProjectContactRole)row.Role).DisplayName(), row.Organisation);
            switch (routing)
            {
                case CorrespondenceRouting.Cc: cc.Add(recipient); break;
                case CorrespondenceRouting.Bcc: bcc.Add(recipient); break;
                case CorrespondenceRouting.To:
                    // Profile To rows are the fallback correspondent — used only when the party
                    // resolves none (the same precedence the pre-profile send path had).
                    if (!partyProvidedTo) to.Add(recipient);
                    break;
            }
        }

        return Deduplicate(to, cc, bcc);
    }

    private static async Task<string?> ResolvePartyNameAsync(
        JpmsContext context, int partyKind, string partyId, CancellationToken ct)
    {
        if (partyKind == (int)PartyKind.Architect)
            return await context.Architects
                .Where(a => a.ArchitectId == partyId).Select(a => a.Name).FirstOrDefaultAsync(ct);
        return await context.Clients
            .Where(c => c.ClientId == partyId).Select(c => c.Name).FirstOrDefaultAsync(ct);
    }

    private static async Task<(string Name, string Email)?> ResolveLegacyPartyContactAsync(
        JpmsContext context, int partyKind, string partyId, CancellationToken ct)
    {
        if (partyKind == (int)PartyKind.Architect)
        {
            var architect = await context.Architects.FindAsync(new object[] { partyId }, ct);
            return string.IsNullOrWhiteSpace(architect?.ContactEmail)
                ? null
                : (architect!.ContactName ?? architect.Name, architect.ContactEmail.Trim());
        }

        var client = await context.Clients.FindAsync(new object[] { partyId }, ct);
        return string.IsNullOrWhiteSpace(client?.PrimaryContactEmail)
            ? null
            : (client!.PrimaryContactName ?? client.Name, client.PrimaryContactEmail.Trim());
    }

    /// <summary>Each address keeps its highest visibility only: To beats Cc beats Bcc.</summary>
    private static RequestRecipientSet Deduplicate(
        List<CorrespondenceRecipient> to, List<CorrespondenceRecipient> cc, List<CorrespondenceRecipient> bcc)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<CorrespondenceRecipient> Keep(List<CorrespondenceRecipient> recipients) =>
            recipients.Where(r => seen.Add(r.Email.Trim())).ToList();

        return new RequestRecipientSet(Keep(to), Keep(cc), Keep(bcc));
    }
}
