namespace Jewel.JPMS.Api.Features.MailboxIntake.Queue;

/// <summary>Storage queue names used by the mailbox-intake feature.</summary>
public static class MailboxQueues
{
    /// <summary>Webhook notifications to fetch + ingest (carries a Graph message id).</summary>
    public const string IntakeNotifications = "mailbox-intake-notifications";
}
