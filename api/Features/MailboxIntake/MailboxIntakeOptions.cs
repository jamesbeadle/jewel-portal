using Microsoft.Extensions.Configuration;

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
    /// Top-level mailbox folder that holds one subfolder per request (named REQ-0001 etc.) plus the
    /// "not relevant" folder. Created on demand by the worker; no folder ids need configuring.
    /// </summary>
    public string RequestsParentFolder { get; set; } = "Requests";

    /// <summary>Subfolder of <see cref="RequestsParentFolder"/> that discarded ("not relevant") emails move into.</summary>
    public string NotRelevantFolder { get; set; } = "Not relevant";

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

    /// <summary>
    /// Binds options from the "MailboxIntake" configuration section. Shared by the SWA API
    /// (producer/webhook side) and the worker Function App (consumer side) so both read the
    /// exact same values. Manual binding (no Configuration.Binder dependency).
    /// </summary>
    public static MailboxIntakeOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var options = new MailboxIntakeOptions
        {
            TenantId = section["TenantId"],
            ClientId = section["ClientId"],
            ClientSecret = section["ClientSecret"],
            NotificationUrl = section["NotificationUrl"],
            ClientState = section["ClientState"],
            Enabled = ParseBool(section["Enabled"], true),
            EnableDeltaSweep = ParseBool(section["EnableDeltaSweep"], true),
            EnableWebhook = ParseBool(section["EnableWebhook"], false),
            EnableFolderMoves = ParseBool(section["EnableFolderMoves"], true),
            EnableOutboundSend = ParseBool(section["EnableOutboundSend"], false)
        };

        var mailbox = section["Mailbox"];
        if (!string.IsNullOrWhiteSpace(mailbox))
            options.Mailbox = mailbox;

        var requestsParent = section["Folders:RequestsParent"];
        if (!string.IsNullOrWhiteSpace(requestsParent))
            options.RequestsParentFolder = requestsParent;

        var notRelevant = section["Folders:NotRelevant"];
        if (!string.IsNullOrWhiteSpace(notRelevant))
            options.NotRelevantFolder = notRelevant;

        options.Folders.InProgress = section["Folders:InProgress"];
        options.Folders.Logged = section["Folders:Logged"];
        options.Folders.NotActioned = section["Folders:NotActioned"];
        options.Folders.NeedsAttention = section["Folders:NeedsAttention"];

        return options;
    }

    private static bool ParseBool(string? value, bool fallback) =>
        bool.TryParse(value, out var parsed) ? parsed : fallback;
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
