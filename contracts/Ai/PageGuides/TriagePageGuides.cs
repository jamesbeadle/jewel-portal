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
            + "work parks per email as a draft. Over the connector you do not drive that page's "
            + "staging — you work the same mailbox directly, and each act lands at once rather than "
            + "waiting for Apply. READ: list_triage_queue is the Inbox pane's views (queue, "
            + "discarded, tagged); search_mailbox finds an email by sender, subject wording or an "
            + "amount — never ask the user to click an email for you; get_mailbox_message reads one "
            + "in full — body, envelope, current tags, attachment ids (they feed "
            + "read_email_attachment) and the replyAll envelope a reply starts from — and is the "
            + "ONLY way to read a queue email, because an untagged email is in no record's "
            + "correspondence yet; list_mailbox_conversation reads the thread around it. DO (each "
            + "one a perform_action): file_email_to_record is the System Tags pane — tagging the "
            + "email to a record (a cross-pathway filing is the user's decision, never yours); "
            + "create_request_from_message, create_work_order_from_message, "
            + "create_bid_package_from_message, create_defect_from_message, "
            + "create_todo_items_from_message, create_calendar_event_from_message, "
            + "create_inventory_item_from_message and import_architect_instruction_from_message "
            + "are the System Actions pane's create-from-email, the new record tagged onto the "
            + "email (read the email first; real cost codes from list_cost_codes); "
            + "send_mailbox_email is the Reply box and the Compose pane — a reply-all in the "
            + "email's thread, a forward, or a brand-new email, SENT from the projects mailbox, "
            + "and a sent reply tags the thread JPMS/Replied so it leaves the queue: it is "
            + "confirm-first, so show the user the envelope and body and get their yes, and "
            + "saveAsDraftOnly stages it in Outlook's Drafts instead; discard_mailbox_message and "
            + "restore_mailbox_message are Discard and its undo; remove_mailbox_message_tag "
            + "un-files; send_attachments_to_document_control is the \"Send to document triage\" "
            + "tick. Page-only: the triage bar's Relevant Event and Entire thread decisions and "
            + "uploading a file from the user's computer onto a reply. Find records to file to "
            + "with find_by_reference, list_requests or list_variations — and, for the month's "
            + "valuation, get_valuation_context (a claim's ValuationClaimId files as type "
            + "ValuationClaim: the live period's own correspondence, shown on the Valuation "
            + "Report's Correspondence section and on every snapshot frozen from the claim) or "
            + "list_valuation_snapshots (type ValuationReportSnapshot, the frozen statement the "
            + "client was sent). Filing attachments to "
            + "the project Documents register, Payment Certificates or subcontractor records is "
            + "not done here — Document Triage (/document-triage, list_document_triage) is where "
            + "a copied attachment gets filed.",
            Aliases: new[] { "/requests/triage" }),

        new("/document-triage", "Document Triage",
            "The attachment triage queue for all projects: each item is a point-in-time copy of one "
            + "email attachment, ticked \"Send to document triage\" in the Control Centre, waiting to "
            + "be filed or discarded. A person works two panes — Queue, Filed and Discarded tabs on "
            + "the left; on the right the open document's preview (PDFs and images inline, everything "
            + "else Download-only), a collapsible Source email, and the filing form. Filing offers "
            + "three destination tabs: Project documents (new document or revision in the "
            + "project's Documents register — drawings and anything else; code/revision/title "
            + "prefilled from the file name when it carries a \"Rev X\", all optional; a Folder "
            + "picker files a new document into any folder or sub-folder, or creates one inline; "
            + "landing as an unapproved revision), Payment certificate "
            + "(project, optional valuation claim, number, amount, issued date) and Subcontractor "
            + "document (subcontractor, kind, expiry); Discard is restorable, never a delete. You "
            + "have no page actions or dialogs here — route the user here with navigate_to. Emails "
            + "themselves are triaged in the Control Centre, and a filed document's approval follows "
            + "the normal documents workflow.",
            Aliases: new[] { "/document-control" }),
    };
}
