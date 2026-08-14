namespace Jewel.JPMS.Features.Triage.Workspace;

/// <summary>
/// The kinds of content a Control Centre workspace pane can show. Every kind keeps its state while
/// hidden — jumping between kinds always comes back to things exactly as they were left.
/// </summary>
public enum PanelKind
{
    /// <summary>The email lists (Queue / Tagged) and the triage staging + Apply — the main page.</summary>
    Inbox,
    /// <summary>The open email: header, body, attachments, thread and reply.</summary>
    Email,
    /// <summary>A read-only second copy of the open email. Pressing the Email icon while the other
    /// pane already shows the email lands this here instead of stealing the email across, so the
    /// original can be read (and copied from) on one side while the reply is typed on the other.</summary>
    EmailMirror,
    /// <summary>The staged system tags for the open email (was the System Tags modal).</summary>
    SystemTags,
    /// <summary>System actions queued to run when the email's triage completes (placeholder).</summary>
    SystemActions,
    /// <summary>The record explorer: search any system document (RFIs first) and read it here.</summary>
    Records,
    /// <summary>A document opened from a record — a linked drawing, a photo, a PDF.</summary>
    Preview,
    /// <summary>The Xero explorer: search and read Xero transactions.</summary>
    Xero,
    /// <summary>Compose a fresh outbound email from the projects mailbox (was the New email modal).</summary>
    Compose,
    /// <summary>The subcontractor communications browser: every "JPMS/SubComms" thread, read live.</summary>
    SubcontractorComms,
    /// <summary>Replies lined up to send when Apply runs — the open email's reply plus replies to
    /// older emails, each of those also tagged with this triage's records.</summary>
    Outbox
}

/// <summary>Which of the two workspace panes — the divider between them is user-draggable.</summary>
public enum PanelSide { Left, Right }

/// <summary>
/// A document to show in the Preview pane: its display title, the URL that renders it inline, the
/// URL that downloads it, and whether the PDF viewer (rather than a plain frame) should render it.
/// </summary>
public sealed record PreviewRequest(string Title, string FileUrl, string DownloadUrl, bool IsPdf);
