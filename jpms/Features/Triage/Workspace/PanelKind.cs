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
    /// <summary>The Client pathway pane: how this email is dealt with on the client side —
    /// tagging to client-side records, and the client-side actions (2026-08-27 restructure;
    /// absorbed the System Tags Client tab and the client half of System Actions).</summary>
    Client,
    /// <summary>The Subcontractor pathway pane: subcontractor-side tagging (records + the
    /// SubComms category registers) and actions.</summary>
    Subcontractor,
    /// <summary>The Supplier pathway pane: supplier-side tagging (the SupComms category
    /// registers — Materials first) and actions. New pathway 2026-08-27.</summary>
    Supplier,
    /// <summary>The Internal pathway pane: staff-to-staff tagging (to-dos, calendar events, the
    /// IntComms category registers) and actions.</summary>
    Internal,
    /// <summary>The record explorer: search any system document (RFIs first) and read it here.</summary>
    Records,
    /// <summary>A document opened from a record — a linked drawing, a photo, a PDF.</summary>
    Preview,
    /// <summary>The Xero explorer: search and read Xero transactions.</summary>
    Xero,
    /// <summary>Compose a fresh outbound email from the projects mailbox (was the New email modal).</summary>
    Compose,
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
