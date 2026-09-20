namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// Who may send an email out of the shared projects mailbox — that is, who may write as the
/// business to a client, an architect or a supplier.
///
/// Every internal role could, from 2026-08-10, so that a to-do's assignee could reply to the item's
/// linked mail from its own page. Nigel narrowed it to the directors on 2026-09-19: free-form mail
/// leaving the business over Jewel's own address is theirs. Reading correspondence is untouched and
/// stays with the whole internal team, as does Control Centre triage; issuing a request's own
/// workflow mail stays with them and the project manager (SendRequestEmailAuthorisation).
///
/// The cost is deliberate and known: a site manager, accounts, office admin, the compliance
/// coordinator, the QS, a foreman and sales can no longer reply to a to-do's mail from the to-do.
/// </summary>
internal static class MailboxComposeRoles
{
    public static readonly RoleSet AllowedToSend =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector);
}
