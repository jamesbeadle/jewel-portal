using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Resolves a "Raised to" contact picked from the project's contact list (Setup tab) into the
/// denormalised display string stored on the request. Shared by RaiseRequest and
/// UpdateRequestDetails so the two paths can never derive different strings for the same contact.
/// On raise the id must belong to the request's project — a mismatch is a user-visible error,
/// not a silent null (the UI only ever offers the project's own contacts). On update the id is
/// resolved leniently (<paramref name="required"/> false): a request outlives its contacts —
/// removing a contact from the Setup tab must never make an unrelated edit or status change fail
/// months later, so a stale link just drops and the RaisedTo string travelling on the command is
/// kept. Linked rows (per-project overrides of a party contact) read the person's current name
/// through from the party contact, matching every other read path.
/// </summary>
internal static class RaisedToContactResolver
{
    public sealed record ResolvedRaisedTo(string ContactId, string Display);

    public static async Task<ResolvedRaisedTo?> ResolveAsync(
        JpmsContext context, string projectId, string? raisedToContactId, CancellationToken cancellationToken,
        bool required = true)
    {
        if (string.IsNullOrWhiteSpace(raisedToContactId)) return null;

        var contact = await context.ProjectContacts.FirstOrDefaultAsync(
            c => c.ContactId == raisedToContactId && c.ProjectId == projectId, cancellationToken);
        if (contact is null)
        {
            if (required)
                throw new InvalidOperationException("That contact is not on this project's contact list. Add them on the project's Setup tab first.");
            return null;
        }

        var name = contact.Name;
        if (contact.PartyContactId is not null)
        {
            var source = await context.PartyContacts.FirstOrDefaultAsync(
                p => p.PartyContactId == contact.PartyContactId, cancellationToken);
            if (source is not null) name = source.Name;
        }

        var display = string.IsNullOrWhiteSpace(contact.Organisation)
            ? name
            : $"{name} ({contact.Organisation})";
        return new ResolvedRaisedTo(contact.ContactId, display);
    }
}
