using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;


public static partial class ModalCatalog
{
    /// <summary>
    /// The Control Centre's "New email" composer, registered as a dialog so the assistant drafts
    /// email THROUGH the portal UI — never straight into Outlook (decision 2026-08-14, retiring the
    /// draft_outlook_email tool). The assistant opens the composer, writes the draft into it, and
    /// the user reviews the envelope and body on their own screen and presses Send (or Save as
    /// draft) themselves — the same human-sends rule every other dialog carries. Record-less and
    /// project-less: the route has no <c>{project}</c> or <c>{record}</c>, because the composer is a
    /// whole-company page and a brand-new email needs no originating record.
    /// </summary>
    public static readonly ModalDescriptor ComposeEmail = new(
        "compose_email",
        "New email",
        "It drafts a brand-new email from the projects mailbox in the Control Centre's composer. The "
        + "user reviews every field — recipients, subject, message — and presses Send themselves; "
        + "nothing is sent, and no draft exists anywhere, until they do.",
        "/control-centre",
        // Exactly the people who can open the Control Centre (the API's TriageRoles) — whoever the
        // compose endpoint will accept SendMailboxEmail from, and nobody else. The QS has the
        // assistant but not this page: offering the dialog would route them to an access refusal.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector,
            Role.ProjectManager
        },
        new ModalField[]
        {
            new("to", "string",
                "The To recipients as email addresses separated by semicolons. Only addresses that "
                + "actually appear in the conversation, a tool result or the correspondence — NEVER "
                + "guess or construct an address, a plausible wrong one sends a real email to a "
                + "stranger. If you have a name but no address, leave this out and say so.",
                Required: true),

            new("cc", "string",
                "Cc recipients, semicolon-separated. Same rule: only addresses you have actually read."),

            new("subject", "string",
                "A concise subject in the house style — the record reference first where one applies: "
                + "\"V72 — Revised Coving: quotation attached\". Not a sentence.",
                Required: true),

            new("body", "string",
                "The message as PLAIN TEXT — blank lines between paragraphs, no HTML, no markdown. "
                + "Plain UK English, direct, commercial position first. Sign off as the sender would "
                + "(their name is in the conversation context); never invent commitments, figures or "
                + "dates that are not in what you have read.",
                Required: true)
        });

    /// <summary>
    /// The Reply box under the Control Centre's selected email, registered so "draft a reply to
    /// this" is a first-class task rather than a degrade to an unthreaded compose_email
    /// (2026-08-22). Two ways in: the user presses Reply (the page attaches the task silently —
    /// no kick-off, no billed turn) or the assistant's open_modal reply_email (the server refuses
    /// it while no email is selected). The reply is STAGED page state — it sends from the
    /// projects mailbox when the user presses Apply, with the rest of the triage.
    /// </summary>
    public static readonly ModalDescriptor ReplyEmail = new(
        "reply_email",
        "Reply",
        "It drafts the reply to the email SELECTED in the Control Centre, in the Reply box under "
        + "it. The envelope prefills reply-all from the email itself. The reply is lined up and "
        + "sends from the projects mailbox when the user presses Apply — nothing sends before "
        + "that. Read the selected email (read_selected_email) BEFORE drafting, so the reply is "
        + "grounded in what was actually written.",
        "/control-centre",
        // Exactly the people who can open the Control Centre (the API's TriageRoles) — same set
        // as compose_email.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector,
            Role.ProjectManager
        },
        new ModalField[]
        {
            new("to", "string",
                "Prefilled reply-all from the email — leave it out unless it is blank, and only "
                + "ever use addresses you have actually read."),

            new("cc", "string",
                "Cc recipients, semicolon-separated. Same rule: only addresses you have actually read."),

            new("subject", "string",
                "Prefilled as \"RE: …\" from the email — leave it out unless it is blank."),

            new("body", "string",
                "The reply as PLAIN TEXT — blank lines between paragraphs, no HTML, no markdown. "
                + "Plain UK English, direct, commercial position first, grounded in what the email "
                + "actually says. Never invent figures, dates or commitments.",
                Required: true)
        });

    /// <summary>
    /// The Reply composer on a bid package's Emails tab, anchored to one tender email — the
    /// "Draft supplier reply" leg of tender extraction: when an extraction finds gaps, the page
    /// opens this composer on the tender email and the assistant writes the reply asking for what
    /// is missing. Deliberately NOT openable via open_modal: the composer only exists anchored to
    /// a specific email, which the page supplies when it starts the task — the assistant can only
    /// update_open_modal it. The user reviews and presses Send themselves; the reply sends from
    /// the projects mailbox and files itself back under the package by the thread's tags.
    /// </summary>
    public static readonly ModalDescriptor TenderReply = new(
        "tender_reply",
        "Reply",
        "It drafts the reply to a subcontractor's tender submission asking for the information their "
        + "tender is missing. The composer is already open beside the chat, anchored to their email "
        + "with the recipients and subject prefilled — update the body (and the rest only if blank); "
        + "the user reviews everything and presses Send themselves.",
        "/projects/{project}/bid-package-invites/{record}",
        // The composer sends through SendMailboxEmail, so exactly the API's triage roles.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector,
            Role.ProjectManager
        },
        new ModalField[]
        {
            new("to", "string",
                "The To recipients, semicolon-separated. Prefilled from the reply envelope — leave "
                + "it out unless it is blank, and only ever use addresses you have actually read."),

            new("cc", "string",
                "Cc recipients, semicolon-separated. Same rule: only addresses you have actually read."),

            new("subject", "string",
                "Prefilled as \"RE: …\" from the tender email — leave it out unless it is blank."),

            new("body", "string",
                "The reply as PLAIN TEXT — blank lines between paragraphs, no HTML, no markdown. "
                + "Plain UK English: thank them for their tender, list exactly what is missing or "
                + "unclear (one line per gap), and ask them to return the completed pricing schedule. "
                + "Never invent figures, dates or commitments that are not in what you have read.",
                Required: true)
        });

}
