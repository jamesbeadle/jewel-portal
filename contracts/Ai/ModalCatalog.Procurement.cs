using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;


public static partial class ModalCatalog
{
    /// <summary>
    /// The bid package page's "Edit package details" dialog — the specification summary AND the
    /// line-item schedule together, because they are one act of authorship: the summary says what
    /// the package covers, the lines are the measurable scope behind it. This is how the assistant
    /// BUILDS OUT a package: it reads the package's context (get_bid_package_context,
    /// read_record_emails, the attachments) and proposes both in ONE update; the user reviews
    /// everything and presses Save details themselves. (One dialog on purpose — a two-dialog
    /// version relied on the model following through across turns, and it didn't: 2026-08-16.)
    /// </summary>
    public static readonly ModalDescriptor BidPackageDetails = new(
        "bid_package_details",
        "Edit package details",
        "It fills the bid package's details in one go: the specification summary (the \"what this "
        + "package covers\" points printed at the top of the pricing schedule each tenderer "
        + "receives) and the schedule of line items (the measurable scope a subcontractor prices). "
        + "The dialog opens pre-filled with what the package already has; the lines sent back "
        + "REPLACE the whole schedule, so keep every existing line that is still right and only "
        + "drop one when the context says it is wrong. Send BOTH fields together in one update. "
        + "The user reviews everything and presses Save details themselves; nothing is written "
        + "until they do.",
        "/projects/{project}/bid-package-invites/{record}",
        // Exactly the page's own CanManage gate: whoever can press Edit on the tab by hand.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.ProjectManager
        },
        new ModalField[]
        {
            new("summary", "string",
                "The specification summary: one point per line, plain text, newline-separated — "
                + "what the package covers and to what standard, as a tenderer needs to read it. "
                + "No bullet characters (the page renders them), no markdown, no headings. Only "
                + "what the package's context actually supports.",
                Required: true),

            new("lines", "array",
                "The complete schedule as it should stand — this replaces the dialog's list. Only "
                + "lines the package's context actually supports: an invented line is a real line "
                + "as far as a tender is concerned.",
                Required: true,
                ItemFields: new ModalField[]
                {
                    new("description", "string", "What is to be done, as a subcontractor would read it.", Required: true),
                    new("unit", "string", "One of: nr, m, m2, m3, item. Use \"item\" for lump-sum scope."),
                    new("quantity", "number", "The measured quantity. Use 1 with unit \"item\" for lump-sum scope."),
                    new("trade", "string", "The trade that prices this line. Defaults to the package's trade."),
                    new("costCode", "string",
                        "The cost centre this line's committed value lands on. It must be a Code "
                        + "returned by list_cost_codes, spelled exactly as that tool returned it. If "
                        + "no code clearly fits, leave this out — the user picks it from a list. "
                        + "Every line needs one before the schedule can save, so say which lines "
                        + "you left blank.")
                })
        });

    /// <summary>
    /// The "Edit work order" dialog on a project's Work Orders tab — the accountant's correction
    /// path (2026-08-21): a live order needs an extra line the supplier's email priced, the FD
    /// edits the order, saves, and downloads the updated purchase order from the PO page to send
    /// back by hand — saving never re-emails the supplier. The route carries the order in the
    /// query (?record=): work orders have no detail page of their own, the dialog lives on the
    /// register. Directors only, matching the API's gate for editing issued orders; the dialog
    /// opens pre-filled with the order as it stands.
    /// </summary>
    public static readonly ModalDescriptor WorkOrderEdit = new(
        "work_order_edit",
        "Edit work order",
        "It edits a work order — title, scope of works and the priced lines — pre-filled with the "
        + "order as it stands. Read the order's context first (get_work_order_context, then "
        + "read_record_emails record_type work_order for the correspondence) and send the corrected "
        + "fields in ONE update. The lines sent back REPLACE the schedule, so keep every existing "
        + "line that is still right, spelled exactly as the dialog shows it — that is what keeps "
        + "its payment history attached; a line with money paid against it can never be removed or "
        + "priced below what has been paid. The user reviews everything and presses Save changes "
        + "themselves; nothing is written until they do, and saving never emails the supplier — "
        + "the updated purchase order is downloaded from the PO page and sent by hand.",
        "/projects/{project}/work-orders?record={record}",
        // Exactly the API's gate for editing issued orders (UpdateManualWorkOrderAuthorisation
        // .MayEditAnyOrder): the MD, the FD and administrators. The wider team can edit manual
        // orders by hand, but offering this dialog to them would route some of them into a 400
        // on orders a source flow owns.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector
        },
        new ModalField[]
        {
            new("title", "string",
                "The order's title, at most 256 characters, in the house style. Leave it out to "
                + "keep what the dialog already shows."),

            new("scope", "string",
                "The scope of works printed on the purchase order, plain text. Itemised £ "
                + "breakdowns belong here too (it prints pre-wrap). Only what the order's "
                + "correspondence actually supports; leave it out to keep what stands."),

            new("lines", "array",
                "The complete priced schedule as it should stand — this replaces the dialog's "
                + "list. Keep every existing line that is still right, with its title spelled "
                + "EXACTLY as the dialog shows it (that match is what preserves the line's payment "
                + "history); add only lines the correspondence actually prices. A line with money "
                + "paid against it can't be removed and can't drop below what has been paid — the "
                + "dialog's state says paidToDate per line.",
                ItemFields: new ModalField[]
                {
                    new("title", "string",
                        "The line as the purchase order prints it — an existing line's title "
                        + "verbatim to keep it, or a new line's short label.", Required: true),
                    new("description", "string",
                        "The longer detail for the PO's Description column — optional, and never "
                        + "the place for quantities or rates (those have their own fields)."),
                    new("costCode", "string",
                        "The cost centre this line's committed value lands on. It must be a Code "
                        + "returned by list_cost_codes, spelled exactly as that tool returned it. "
                        + "If no code clearly fits, leave this out — the user picks it from a "
                        + "list. A wrong cost code sends real money to the wrong place."),
                    new("quantity", "number",
                        "How much, in the unit — 14 for \"14 m2\". Give quantity, unit and "
                        + "unitCost TOGETHER whenever the quote prices by measure: they print in "
                        + "the PO's own Qty/Unit and Unit Cost columns and the line's amount "
                        + "becomes quantity × unitCost. NEVER restate them as prose in the "
                        + "description — \"14 m2 @ £54.00/m2\" in the description prints beside "
                        + "a Qty/Unit column reading \"1 item\", which is exactly the confusion "
                        + "these fields exist to end."),
                    new("unit", "string",
                        "The measure the quantity counts — m2, m, no., day, load. Printed beside "
                        + "the quantity."),
                    new("unitCost", "number",
                        "The rate per unit in GBP, NET of VAT — 54 for £54.00/m2. Only rates the "
                        + "correspondence actually states."),
                    new("amount", "number",
                        "The line's TOTAL value in GBP as a plain number, NET of VAT. Negative "
                        + "only for a credit line. Only figures the correspondence actually "
                        + "states. Leave it out when quantity and unitCost are given — the "
                        + "dialog computes quantity × unitCost itself.")
                })
        });

    /// <summary>
    /// The "Add work order" dialog on a project's Work Orders tab — raising a brand-NEW manual
    /// order. Registered 2026-08-21 after the "Raise this WO" to-do: the assistant read Nigel's
    /// £1,800 email end to end and then had to tell the user to press the button itself, because
    /// only EDITING was registered. Same modal as work_order_edit, opened empty; no <c>{record}</c>
    /// in the route because this dialog CREATES. Saving a live order mints the WO number and
    /// emails the purchase order to the supplier from the projects mailbox at once; a draft sends
    /// nothing until its two-click Approve on the tab.
    /// </summary>
    public static readonly ModalDescriptor WorkOrderCreate = new(
        "work_order_create",
        "Add work order",
        "It raises a NEW manual work order — supplier, title, scope of works and the priced lines "
        + "— from correspondence the user already has: a supplier's priced email, a \"raise this "
        + "WO\" to-do, the conversation. Read that correspondence first (read_record_emails on the "
        + "to-do or record that holds it) and send the fields in ONE update. The user reviews "
        + "everything and presses the button themselves; nothing exists until they do. Saving a "
        + "LIVE order mints the WO number and emails the purchase order to the supplier at once — "
        + "propose saveAsDraft true unless the correspondence clearly confirms the figures.",
        "/projects/{project}/work-orders",
        // Exactly the API's gate for raising a manual order (CreateManualWorkOrderAuthorisation):
        // Admin, the MD, the FD, project managers and estimators (the Estimator seat is the
        // QuantitySurveyor role).
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector,
            Role.ProjectManager,
            Role.QuantitySurveyor
        },
        new ModalField[]
        {
            new("supplier", "string",
                "The subcontractor the order is raised to, named as the correspondence says it — "
                + "\"MGN Drywall\". The dialog matches the name against the live directory and "
                + "says on screen when nothing matches, so pass it through rather than guessing — "
                + "the user picks an unmatched supplier from the list themselves.",
                Required: true),

            new("title", "string",
                "The order's title, at most 256 characters, in the house style — "
                + "\"Render materials — WH89 colour change\". Not a sentence.",
                Required: true),

            new("scope", "string",
                "The scope of works printed on the purchase order, plain text (it prints "
                + "pre-wrap, so an itemised breakdown can sit one charge per line). Only what the "
                + "correspondence actually supports."),

            new("saveAsDraft", "boolean",
                "true stores a draft — no WO number, no email to the supplier — awaiting the "
                + "two-click Approve on the Work Orders tab; false or left out releases on save, "
                + "which mints the number and emails the purchase order at once. Propose true "
                + "unless the correspondence clearly confirms the figures."),

            new("lines", "array",
                "The priced schedule. Only lines the correspondence actually prices — an invented "
                + "figure ends up on a purchase order.",
                ItemFields: new ModalField[]
                {
                    new("title", "string",
                        "The line as the purchase order prints it — a short label.", Required: true),
                    new("description", "string",
                        "The longer detail for the PO's Description column — optional, and never "
                        + "the place for quantities or rates (those have their own fields)."),
                    new("costCode", "string",
                        "The cost centre this line's committed value lands on. It must be a Code "
                        + "returned by list_cost_codes, spelled exactly as that tool returned it. "
                        + "If no code clearly fits, leave this out — the user picks it from a "
                        + "list. A wrong cost code sends real money to the wrong place."),
                    new("quantity", "number",
                        "How much, in the unit — 14 for \"14 m2\". Give quantity, unit and "
                        + "unitCost TOGETHER whenever the quote prices by measure: they print in "
                        + "the PO's own Qty/Unit and Unit Cost columns and the line's amount "
                        + "becomes quantity × unitCost. NEVER restate them as prose in the "
                        + "description — \"14 m2 @ £54.00/m2\" in the description prints beside "
                        + "a Qty/Unit column reading \"1 item\", which is exactly the confusion "
                        + "these fields exist to end."),
                    new("unit", "string",
                        "The measure the quantity counts — m2, m, no., day, load. Printed beside "
                        + "the quantity."),
                    new("unitCost", "number",
                        "The rate per unit in GBP, NET of VAT — 54 for £54.00/m2. Only rates the "
                        + "correspondence actually states."),
                    new("amount", "number",
                        "The line's TOTAL value in GBP as a plain number, NET of VAT. Only "
                        + "figures the correspondence actually states. Leave it out when "
                        + "quantity and unitCost are given — the dialog computes "
                        + "quantity × unitCost itself.")
                })
        });

    /// <summary>
    /// The PQQ response editor on a tender enquiry's page — the questionnaire's numbered questions
    /// with Jewel's answer under each. Page-anchored like tender_reply: the user presses "Draft
    /// with AI" on the PQQ tab and the editor is already open beside the chat; the assistant reads
    /// the questionnaire as received (get_tender_enquiry_context → read_tender_enquiry_document),
    /// lifts the questions and drafts the answers. The user reviews and presses Save themselves.
    /// </summary>
    public static readonly ModalDescriptor TenderEnquiryAnswers = new(
        "tender_enquiry_answers",
        "PQQ response",
        "It drafts Jewel's response to an architect's pre-qualification questionnaire: the questions "
        + "exactly as the architect asked them, each with Jewel's answer beneath. The editor is already "
        + "open beside the chat with whatever has been typed so far — send the complete list; the user "
        + "reviews every answer and presses Save answers themselves.",
        "/tender-enquiries/{record}",
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.ProjectManager,
            Role.QuantitySurveyor
        },
        new ModalField[]
        {
            new("answers", "array",
                "The complete questionnaire as it should stand — this replaces the editor's list, so "
                + "carry every existing row forward (edited or not) and keep the architect's order. "
                + "Questions come from the questionnaire document or email, numbering stripped, wording "
                + "kept. Answers are plain UK English in Jewel Bespoke Build's own voice, first person "
                + "plural, no markdown. Only state facts you have READ — in the enquiry, its documents, "
                + "its emails, the conversation, or a loaded skill. Where a question needs a fact you "
                + "do not have (turnover, insurance limits, company number, referees, staff numbers), "
                + "write the answer's frame and leave the figure as a bracketed prompt such as "
                + "[turnover FY2025], and say which ones you left for the user. Never invent an "
                + "accreditation, a figure, a client or a project.",
                Required: true,
                ItemFields: new ModalField[]
                {
                    new("question", "string", "The question as the architect asked it, without its number.", Required: true),
                    new("answer", "string", "Jewel's answer, plain text. Blank lines between paragraphs.")
                })
        });

}
