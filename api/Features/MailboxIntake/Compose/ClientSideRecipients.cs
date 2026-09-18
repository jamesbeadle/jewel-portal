using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// Who the client side of a project is, for any outbound email addressed to it: the Client and
/// Architect rows of the correspondence profile that carry an email address, deduped in case the
/// same person sits on the profile twice. The valuation statement and the variation order both go
/// to exactly this set, so they read it here rather than each keeping a copy of the rule.
///
/// An empty answer is returned rather than thrown: the sentence a person reads when a project has
/// nobody to send to names the document they were trying to send, so it belongs at the door.
/// </summary>
internal static class ClientSideRecipients
{
    private static readonly int[] ClientSideRoles =
        { (int)ProjectContactRole.Client, (int)ProjectContactRole.Architect };

    public static async Task<List<MailboxDraftRecipient>> ForProjectAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var contacts = await context.ProjectContacts.AsNoTracking()
            .Where(contact => contact.ProjectId == projectId && ClientSideRoles.Contains(contact.Role) && contact.Email != "")
            .OrderBy(contact => contact.Role).ThenBy(contact => contact.Name)
            .ToListAsync(cancellationToken);

        return contacts
            .GroupBy(contact => contact.Email.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new MailboxDraftRecipient(group.Key, group.First().Name))
            .ToList();
    }
}
