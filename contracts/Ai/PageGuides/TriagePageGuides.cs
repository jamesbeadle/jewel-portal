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
            + "user presses Apply); DRAFT A NEW WORK ORDER from the selected email with "
            + "stage_triage_work_order — it fills the System Actions Raise Work Order form (read "
            + "the email first; real cost codes from list_cost_codes), and the user raises it "
            + "with Apply or immediately with the staged chip's Create now button, the email "
            + "tagged to the new order either way (System Actions also creates RFIs, bid "
            + "packages and defects from the email by hand, each with the same Apply-or-Create-"
            + "now choice; cross-pathway filings ask one amber \"File under both anyway\" "
            + "confirm); DRAFT THE REPLY to the selected email with open_modal "
            + "reply_email (it opens the page's own Reply box under the email, envelope prefilled "
            + "reply-all; read the email first, write the body with update_open_modal, and the "
            + "reply sends with the user's Apply); draft any brand-new email with open_modal "
            + "compose_email and write into it with update_open_modal; what is selected and "
            + "already staged — and why anything was refused — rides in "
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
