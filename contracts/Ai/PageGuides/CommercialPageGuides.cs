namespace Jewel.JPMS.Contracts.Ai;

/// <summary>Variations, the valuation report and its satellites. Data only.</summary>
public static class CommercialPageGuides
{
    public static readonly IReadOnlyList<PageGuide> Guides = new PageGuide[]
    {
        new("/projects/{project}/variations", "Variation Orders",
            "The project's variation book — one row per variation (ref, title, originating request, "
            + "status, value, issued/approved dates, work orders), with free-text search and Excel "
            + "export. Each variation is one document through the ladder Quoting → Issued → "
            + "Awaiting AI → Approved/Rejected; only Approved writes value onto the valuation "
            + "report. The status chip's dropdown moves the side-effect-free stages directly; "
            + "Approve and the post-approval transitions link through to the variation itself, and "
            + "pre-approval Rejected confirms first (terminal). Subcontractor variation requests are "
            + "accepted (creating a variation) or rejected here, and approved variations with a "
            + "selected sub get \"Issue WO\". You can open_modal manual_variation — the \"Add "
            + "variation manually\" dialog for a standalone variation from the user's own data — and "
            + "read via list_variations. RFI-led drafting (variation_draft) happens on the request "
            + "page, not here.",
            Aliases: new[] { "/projects/{project}/requests/variations" }),

        new("/projects/{project}/variations/{variationOrderId}", "Variation detail",
            "One variation document through Quoting → Issued → Awaiting AI → Approved/Rejected, "
            + "with the official Variation Order PDF (scope, commercial basis, programme impact, "
            + "exclusions — all editable in place, title too). The status pill moves the free stages "
            + "directly; \"Approved…\" opens the approve modal where priced lines (one per cost "
            + "centre) are built up and written to the Valuation Report, CVR and budgets; "
            + "post-approval offers Edit lines, Revise value, Issue work order, Return to quoting "
            + "(un-approve) and Reject (reverses the writes). \"Record agreed tender\" captures the "
            + "chosen subcontractor and value pre-approval; an Awaiting-AI banner shows whether an "
            + "Architect's Instruction is linked. The Communications panel reads the tagged mail "
            + "(read_record_emails works here) with reply/forward and \"Find & tag emails\". "
            + "get_variation_context reads the whole record in one call — header, request, the "
            + "approved lines with their ids, work orders. Two dialogs are registered here, one per "
            + "side of approval: BEFORE it, open_modal \"variation_build_up\" (record_id = the "
            + "variation's id) opens the Agreed build-up dialog — stage the client-agreed priced lines "
            + "and narratives from the evidence, the user presses Stage build-up, the total becomes "
            + "the estimate and the approve modal opens pre-seeded; AFTER it, open_modal "
            + "\"variation_edit_lines\" opens Edit lines pre-filled with the real build-up so you can "
            + "send the corrected schedule — the user presses Save lines.",
            Aliases: new[] { "/projects/{project}/voq/{variationOrderId}" }),

        new("/projects/{project}/valuation", "Valuation Report",
            "The picked project's LIVE valuation report — the system's flagship output, internal "
            + "only (clients only ever receive frozen snapshots). Work runs in monthly claims: the "
            + "claim card's stepper is Value & lock → Claim → Approve → Invoice → Paid → Confirm & "
            + "roll over, with one primary button per stage and an Actions menu for rename, reopen, "
            + "record rejection/payment, issue without approval and delete. Lines are added or "
            + "edited while the claim is Draft; the Valuation Invoices and Snapshots sections sit "
            + "inline, and working-copy PDF/Excel exports are always available. The toolbar's "
            + "\"Client references\" tag button maps each cost centre on the report to the client's "
            + "own schedule-of-works item number (\"3.12\", \"2.1–2.4\"); once any are set, the "
            + "client PDF gains a \"Client ref\" column beside Code, frozen into each snapshot at "
            + "capture. get_valuation_context "
            + "reads the whole report — every line with its id, the selected claim's % complete, the "
            + "previous claim's, the totals. One dialog is registered here: open_modal "
            + "\"claim_progress\" (project_id; no record) opens Set % complete for the selected "
            + "claim, which must be Draft — you send the lines to change with their cumulative %, "
            + "the user checks and presses Save. Approving variations, which writes their lines "
            + "here, happens on the variation record; editing an approved variation's lines is "
            + "variation_edit_lines on that record."),

        new("/projects/{project}/valuation-snapshots", "Valuation Snapshots",
            "The read-only register of the project's frozen valuation report snapshots — the "
            + "point-in-time statements the client was actually sent (the live Valuation Report tab "
            + "is internal; only snapshots are client-facing). A snapshot freezes automatically when "
            + "a valuation invoice is raised, and again on submit/issue after an amendment; "
            + "superseded rows stay listed, muted. Clicking a row opens the frozen report in a "
            + "viewer; each row also offers the branded PDF download and, for report-running roles, "
            + "an Email button that drafts the report to the client from the shared mailbox — "
            + "nothing sends from this page. Taking or deleting snapshots, and managing the invoices "
            + "behind them, is done on the Valuation Report tab. You can route users here with "
            + "navigate_to; no dialog is registered."),

        new("/projects/{project}/reconciliation-audit", "Reconciliation Audit",
            "The finance reconciliation trail for this project's valuation report: every cost-centre "
            + "recode of a report line, newest first — who moved it, which line, from which centre "
            + "to which, and the value that moved; \"Load more\" pages in batches of 50. The page is "
            + "entirely read-only by design — the moves themselves are made on the Valuation Report "
            + "tab and its cost-centre modals; this is the record finance reconciles against. You "
            + "can route users here with navigate_to but have no dialog or write here."),

        new("/projects/{project}/architect-instructions", "Architect's Instructions",
            "The register of the project's formal Architect's Instructions — the written authority "
            + "a variation sitting at Awaiting AI is waiting for before it can be approved. "
            + "Instructions arrive by email to the projects mailbox (imported from the attachment) "
            + "or are filed here directly via \"File an instruction\": the architect's own "
            + "reference, date, title, issuing architect's email, notes, an optional document (a row "
            + "can be filed now and the document attached when it arrives, showing \"Awaited\" "
            + "meanwhile), and tick-boxes for the variations it covers — Awaiting-AI ones listed "
            + "first. Each row links through to its variations, offers \"Link a variation…\"/Unlink, "
            + "the stored document, and Delete (variations survive). One instruction routinely "
            + "covers several variations. You read via list_variations and route here with "
            + "navigate_to; no dialog is registered."),
    };
}
