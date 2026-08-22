namespace Jewel.JPMS.Contracts.Ai;

/// <summary>Requests, RFIs and the lead queues. Data only.</summary>
public static class RequestPageGuides
{
    public static readonly IReadOnlyList<PageGuide> Guides = new PageGuide[]
    {
        new("/projects/{project}/requests", "RFI register",
            "The project's RFI register — official Requests for Information with contractual "
            + "response-due dates — with the legacy General requests kept one tab behind (requests "
            + "are being sunset; nothing raises a new one). A person switches the RFIs / Requests "
            + "tabs, the Active / Closed / All status chips, and free-text search over subject, "
            + "description, response, notes and drawing references; presses Raise RFI (a manual RFI "
            + "with no email behind it), exports to Excel, changes status from the row's status "
            + "chip, ticks RFIs to \"Prepare email drafts\" (one Outlook draft each in the projects "
            + "mailbox, PDF attached — sent from Outlook, never here), or merges exactly two open "
            + "General requests. You read it with list_requests and find_by_reference; no dialog "
            + "opens here. Variations live at /projects/{project}/variations; most RFIs are raised "
            + "from emails in the Control Centre.",
            // The register's tab routes (/requests/all, /requests/general, /requests/rfis) are
            // the same page — without the alias they resolved no guide at all.
            Aliases: new[] { "/projects/{project}/requests/{kind}" }),

        new("/projects/{project}/requests/view/{requestId}", "Request / RFI detail",
            "One request's full working papers: detail, official form (itemised queries, basis, "
            + "response/action, impact-if-late), response, attachments, tagged email conversation "
            + "and audit history, with Request and Official-form panes. A person uses the status "
            + "pill dropdown, the Email button (stages an Outlook draft — fresh or reply-all into a "
            + "tagged chain, PDF attached; nothing sends from the portal), Promote to RFI, and the "
            + "Actions menu: record response, close (date + agent gate), edit subject/dates/"
            + "description/official form, critical-path tag, create bid packages, return to Control "
            + "Centre, delete (admin). \"Find & tag emails\" adds correspondence. You open the "
            + "variation_draft dialog here (open_modal) to draft the Create Variation Order Quote "
            + "form, and read with get_request_context, list_request_correspondence and "
            + "read_record_emails. Brand-new standalone emails are composed in the Control Centre "
            + "instead."),

        new("/rfis", "RFI register (all projects)",
            "The company-wide RFI register — every project's RFIs in one flat, read-only table, "
            + "grouped by project reference then RFI number, with overdue day-counts in red and a "
            + "Variation badge where an RFI implies one. A person switches the Active / Closed / All "
            + "chips, exports to Excel, and clicks a row to open that RFI's detail page. You read "
            + "RFIs with list_requests (per project), resolve references with find_by_reference, and "
            + "navigate_to a row's detail page. Nothing is created or edited here — raise or work an "
            + "RFI on its project's register, its detail page, or the Control Centre."),

        new("/estimating-queue", "Estimating queue",
            "The QS's estimating queue — leads currently at the Tendering or Feasibility Review "
            + "stage, ordered by capture date, oldest first. A person reads the leads table "
            + "(Reference, Contact, Site, Source, Value, Stage badge) and exports it to Excel. "
            + "There is nothing to create or edit on the page itself, no tool reads leads, and no "
            + "dialog opens here — you can only navigate_to this page and describe what the user "
            + "sees. Lead capture, qualification and won/lost outcomes are handled elsewhere in the "
            + "leads pipeline."),

        new("/nurture", "Nurture",
            "The CRM nurture list — leads at the Lost or Nurture stage, kept warm for future "
            + "opportunities, newest first; the header notes the lifecycle re-opens when a lead is "
            + "contacted. A person reads the same leads table as the estimating queue and exports it "
            + "to Excel. Nothing is created or edited here, no tool reads leads, and no dialog opens "
            + "here — you can only navigate_to this page. Marking a lead lost or won happens "
            + "elsewhere in the leads pipeline."),
    };
}
