namespace Jewel.JPMS.Contracts.Ai;

/// <summary>The intake queues — the Control Centre and Document Triage. Data only; the catalogue
/// and matcher live in <see cref="PageGuideCatalogue"/>.</summary>
public static class TriagePageGuides
{
    public static readonly IReadOnlyList<PageGuide> Guides = new PageGuide[]
    {
        new("/control-centre", "Control Centre",
            "The live mailbox intake queue and router for all correspondence across every project — "
            + "the projects mailbox read in place, where triage only adds or removes category tags and "
            + "nothing ever moves. A person works it in a two-window workspace (panes on an icon rail: "
            + "Inbox with Queue and Tagged tabs, Email plus a read-only mirror, System Tags, System "
            + "Actions, Records, Xero, Subcontractor Comms, Outbox, Compose, Preview) under a triage "
            + "bar holding the selected email's project (auto-matched where possible), two mandatory "
            + "Yes/No decisions — Relevant Event for Programme and Entire thread — an armable Discard, "
            + "and the one Apply button that lands everything staged (tags, new records, to-dos, "
            + "replies and forwards, \"Send to document triage\" ticks) in a single act; unfinished "
            + "work parks per email as a draft. You work on the SELECTED email without navigating "
            + "away — and you SELECT it yourself with select_email (search words: sender, subject "
            + "wording, an amount; never ask the user to click an email for you): "
            + "read_selected_email reads that exact email — full body, envelope, attachment "
            + "ids (they feed read_email_attachment) — and is the ONLY way to read a queue email, "
            + "because an untagged email is in no record's correspondence yet; stage record tags "
            + "with stage_triage_tag and to-dos with stage_triage_todo (nothing lands until the "
            + "user presses Apply); draft any email — including a reply to the selected one, after "
            + "reading it — by opening the New email composer with open_modal compose_email and "
            + "writing into it with update_open_modal; what is selected and already staged rides in "
            + "the current-context block; and find records to stage "
            + "with find_by_reference, list_requests or list_variations. Filing attachments to "
            + "Drawings, Payment Certificates or subcontractor records is not done here — the "
            + "\"Send to document triage\" tick copies them to /document-triage.",
            Aliases: new[] { "/requests/triage" }),

        new("/document-triage", "Document Triage",
            "The attachment triage queue for all projects: each item is a point-in-time copy of one "
            + "email attachment, ticked \"Send to document triage\" in the Control Centre, waiting to "
            + "be filed or discarded. A person works two panes — Queue, Filed and Discarded tabs on "
            + "the left; on the right the open document's preview (PDFs and images inline, everything "
            + "else Download-only), a collapsible Source email, and the filing form. Filing offers "
            + "three destination tabs: Drawings (new drawing or revision, code/revision/title "
            + "prefilled from the file name, landing as an unapproved revision), Payment certificate "
            + "(project, optional valuation claim, number, amount, issued date) and Subcontractor "
            + "document (subcontractor, kind, expiry); Discard is restorable, never a delete. You "
            + "have no page actions or dialogs here — route the user here with navigate_to. Emails "
            + "themselves are triaged in the Control Centre, and a filed drawing's approval follows "
            + "the normal drawings workflow.",
            Aliases: new[] { "/document-control" }),
    };
}
