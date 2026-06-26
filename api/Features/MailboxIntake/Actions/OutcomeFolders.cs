using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Actions;

/// <summary>
/// Maps a triage outcome to the mailbox folder the email should be moved into, so the Inbox stays
/// the "untriaged" pile. NeedsTriage stays in the Inbox (no move). Failed moves to "Needs
/// attention" only if that folder is configured; otherwise it stays in the Inbox so it can't be
/// missed. A null result means "no move".
/// </summary>
public static class OutcomeFolders
{
    public static string? ResolveFolderId(IntakeStatus status, MailboxFolderIds folders) => status switch
    {
        IntakeStatus.Claimed => folders.InProgress,
        IntakeStatus.Linked => folders.Logged,
        IntakeStatus.Discarded => folders.NotActioned,
        IntakeStatus.Failed => folders.NeedsAttention,
        _ => null
    };
}
