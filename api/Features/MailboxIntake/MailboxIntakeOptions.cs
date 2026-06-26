namespace Jewel.JPMS.Api.Features.MailboxIntake;

/// <summary>
/// Configuration for the projects@jewelbb.co.uk mailbox integration (Microsoft Graph).
///
/// These values are read from application configuration (app settings / Key Vault) at runtime.
/// The client secret is a password to the mailbox: it MUST live in app settings or Key Vault and
/// must NEVER be written into source control or committed config. Local development uses
/// local.settings.json (which is git-ignored).
///
/// When <see cref="IsConfigured"/> is false (no secret present), the feature registers no-op
/// fallbacks so the app still runs without Graph access — mirroring the existing invite-notifier pattern.
/// </summary>
public sealed class MailboxIntakeOptions
{
    public const string SectionName = "MailboxIntake";

    /// <summary>Entra tenant id.</summary>
    public string? TenantId { get; set; }

    /// <summary>App registration (client) id.</summary>
    public string? ClientId { get; set; }

    /// <summary>App registration client secret. Sourced from app settings / Key Vault only.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Target shared mailbox, e.g. projects@jewelbb.co.uk.</summary>
    public string Mailbox { get; set; } = "projects@jewelbb.co.uk";

    /// <summary>
    /// Public HTTPS URL of the webhook Function that Graph posts change notifications to.
    /// Required only when <see cref="EnableWebhook"/> is true.
    /// </summary>
    public string? NotificationUrl { get; set; }

    /// <summary>
    /// Opaque secret echoed back by Graph in every notification so we can verify it is genuine.
    /// Generate any sufficiently random string and keep it in app settings.
    /// </summary>
    public string? ClientState { get; set; }

    /// <summary>
    /// Outcome-folder ids in the mailbox, keyed by intake status. Used by the move worker.
    /// NeedsTriage maps to the Inbox (no move). Missing entries simply skip the move for that status.
    /// </summary>
    public MailboxFolderIds Folders { get; set; } = new();

    // --- Feature flags so each moving part can be enabled independently as it is proven out ---

    /// <summary>Master switch. When false the whole feature uses no-op fallbacks.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Run the delta sweep timer (the completeness safety net + backlog import).</summary>
    public bool EnableDeltaSweep { get; set; } = true;

    /// <summary>Create/renew the Graph change-notification subscription (near-real-time speed).</summary>
    public bool EnableWebhook { get; set; }

    /// <summary>Move emails into outcome folders as triage progresses.</summary>
    public bool EnableFolderMoves { get; set; } = true;

    /// <summary>Send outbound (Shared) replies via Graph. Off by default — enable deliberately.</summary>
    public bool EnableOutboundSend { get; set; }

    /// <summary>True when the minimum Graph credentials are present.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(Mailbox);
}

/// <summary>Graph folder ids for each triage outcome. NeedsTriage stays in the Inbox.</summary>
public sealed class MailboxFolderIds
{
    /// <summary>Folder for Claimed items ("In progress").</summary>
    public string? InProgress { get; set; }

    /// <summary>Folder for Linked / created-a-request items ("Logged in JPMS").</summary>
    public string? Logged { get; set; }

    /// <summary>Folder for Discarded items ("Not actioned").</summary>
    public string? NotActioned { get; set; }

    /// <summary>Optional folder for Failed items ("Needs attention"). If null, failures stay in the Inbox.</summary>
    public string? NeedsAttention { get; set; }
}
