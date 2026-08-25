using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// One field of a registered dialog, described for the MODEL rather than for a developer. The
/// description is prompt text: it is the only place the model learns the house rules for that field,
/// so state the constraint and the reason, and say what NOT to do.
/// </summary>
public sealed record ModalField(
    string Name,
    /// <summary>JSON Schema type: "string", "number", "boolean" or "array".</summary>
    string Type,
    string Description,
    bool Required = false,
    /// <summary>For "array" fields: the shape of one item. Null for a bare list of scalars.</summary>
    IReadOnlyList<ModalField>? ItemFields = null);

/// <summary>
/// A dialog the assistant is allowed to open and fill in — docs/ai/00-agent-architecture.md §5
/// (ADR-003). Registering one here is the explicit opt-in that makes it reachable; the registry is
/// never derived from the component tree, because "every dialog in the app" is not a capability
/// anybody chose to grant.
///
/// <para>Filling a dialog writes NOTHING. It puts values on the user's own screen, in the form they
/// already know, and they press the button. The dialog is the proposal card §4 asks for and its
/// confirm button is the approval step, which is why these are <c>Ui</c> tools and not writes.</para>
/// </summary>
public sealed record ModalDescriptor(
    /// <summary>snake_case, what the model sees and what the client switches on.</summary>
    string ModalKey,
    /// <summary>The dialog's own title, exactly as the user reads it on screen.</summary>
    string DisplayName,
    /// <summary>One or two clauses telling the model what this dialog is for.</summary>
    string Purpose,
    /// <summary>Where it can be opened. <c>{project}</c> and <c>{record}</c> are substituted.</summary>
    string RouteTemplate,
    IReadOnlyList<Role> OpenableBy,
    IReadOnlyList<ModalField> Fields);

public static class ModalCatalog
{
    /// <summary>
    /// The Create Variation Order Quote dialog on the RFI page. The field rules below are the ones
    /// that used to live in PrepareVoqDraftHandler's system prompt — they moved here with the
    /// drafting itself, so there is one statement of them rather than two that can drift.
    /// </summary>
    public static readonly ModalDescriptor VariationDraft = new(
        "variation_draft",
        "Create Variation Order Quote",
        "It drafts the variation an RFI has led to: its title, the scope of works, an estimated "
        + "value, and the measurable scope lines that go out to subcontractors to price. The user "
        + "reviews every field and presses Raise variation themselves; nothing exists until they do.",
        "/projects/{project}/requests/view/{record}",
        // Exactly VariationRoles.AllowedToManageVariations — whoever the API will accept
        // CreateVoqFromRfq from, and nobody else. The Finance Director has the assistant but not
        // this dialog: offering it would walk them through reading a whole email thread, reviewing a
        // draft, and then a 403 on the button.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.ProjectManager,
            Role.QuantitySurveyor
        },
        new ModalField[]
        {
            new("title", "string",
                "A concise variation title, at most 200 characters, in the house style — "
                + "\"Revised Coving and LED Lighting Details\". Not a sentence, not a description.",
                Required: true),

            new("description", "string",
                "The scope of works, at most 1900 characters of plain text. State what has changed "
                + "against the contract and why, drawn from what was actually said in the "
                + "correspondence. No headings, no markdown, no bullet characters."),

            new("estimatedValue", "number",
                "The variation's likely value in GBP as a plain number. Set it ONLY where the "
                + "correspondence actually quotes a price or unambiguously implies one. Otherwise "
                + "leave the field out entirely and say in the chat that nothing has been priced yet "
                + "— a figure with nothing behind it is worse than a blank, because it gets quoted "
                + "to a client."),

            new("trade", "string",
                "The single trade best placed to price the work — Joinery, Electrical, Plastering, "
                + "Groundworks and so on. This becomes the draft bid package's trade."),

            new("lines", "array",
                "The measurable scope items a subcontractor would price. Return an empty array if "
                + "the correspondence contains no scope you can itemise — an invented line is a real "
                + "line as far as a tender is concerned.",
                ItemFields: new ModalField[]
                {
                    new("description", "string", "What is to be done, as a subcontractor would read it.", Required: true),
                    new("unit", "string", "One of: nr, m, m2, m3, item. Use \"item\" for lump-sum scope."),
                    new("quantity", "number", "The measured quantity. Use 1 with unit \"item\" for lump-sum scope."),
                    new("trade", "string", "The trade that prices this line. Defaults to the variation's trade."),
                    new("costCode", "string",
                        "The cost centre this line's committed value lands on. It must be a Code "
                        + "returned by list_cost_codes, spelled exactly as that tool returned it. If "
                        + "no code clearly fits, leave this out — the user picks it from a list. A "
                        + "wrong cost code sends real money to the wrong place and nobody notices "
                        + "for a month.")
                })
        });

    /// <summary>
    /// The "Add variation manually" dialog on the Variations register — a standalone variation with
    /// no RFI behind it, which is exactly the shape of work arriving from outside the system: the
    /// boss's spreadsheet, a client instruction, a historic reconciliation. No <c>{record}</c> in
    /// the route: this dialog CREATES, so open_modal needs no record id for it.
    /// </summary>
    public static readonly ModalDescriptor ManualVariation = new(
        "manual_variation",
        "Add variation manually",
        "It creates a standalone variation order — in Quoting, with no RFI behind it — from data the "
        + "user already has: an attached spreadsheet, the conversation, a client instruction. The "
        + "user reviews every field and presses Add variation themselves; nothing exists until they do.",
        "/projects/{project}/variations",
        // Same set as variation_draft: whoever the API accepts CreateManualVariationOrder from.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.ProjectManager,
            Role.QuantitySurveyor
        },
        new ModalField[]
        {
            new("title", "string",
                "A concise variation title, at most 200 characters, in the house style — "
                + "\"Internal Doors: Additional Frames and Plant Room Door Change\". Not a sentence.",
                Required: true),

            new("description", "string",
                "What the variation covers, plain text. Drawn from the user's data — the attached "
                + "file or what they told you — never invented. No headings, no markdown."),

            new("estimatedValue", "number",
                "The variation's value in GBP as a plain number, NET of VAT — the ex-VAT figure, "
                + "never the inc-VAT total. Negative for an omit. Set it only where the user's data "
                + "actually states it; otherwise leave the field out and say so."),

            new("number", "number",
                "The variation number, only when the user's data names one (V86 → 86) — it fixes "
                + "the reference so the register lines up with what the client has already seen. "
                + "Leave it out to take the project's next number. Never guess one."),

            new("commercialBasis", "string",
                "The document's commercial-basis narrative — what the price is based on: rate "
                + "basis, tender-face position, OH&P. Only what the user's data supports."),

            new("programmeImpact", "string",
                "The document's programme-impact narrative — effect on procurement, mobilisation, "
                + "works duration. Only what the user's data supports."),

            new("exclusions", "string",
                "What this variation expressly does not price. Only what the user's data supports.")
        });

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

    /// <summary>
    /// The manual timesheet entry dialog on a project's Labour tab — the chat's way into "put
    /// Danny down for 8 hours on the Chiltern job yesterday, second fix"
    /// (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md §4b). Filling it writes nothing:
    /// the user reads worker, date, hours and cost code resolved on their own screen and presses
    /// Add day themselves, which creates an ordinary Submitted timesheet — same validation, same
    /// approval, same budget hard-block as any other entry.
    /// </summary>
    public static readonly ModalDescriptor ManualTimesheet = new(
        "manual_timesheet",
        "Add a day",
        "It enters one worker's day on this project: who, the date, the hours and the cost code. "
        + "Use it for missed sign-outs and verbal reports. The entry lands as a Submitted "
        + "timesheet for normal approval — never approved by this dialog. Ask rather than assume "
        + "when the worker, date or cost code is unclear; a wrong cost code miscosts real labour.",
        "/projects/{project}/labour",
        // Exactly LabourRoleSets.ApproveTimesheets — whoever the API will accept AddWorkerTimesheet
        // from, and nobody else.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector,
            Role.ProjectManager
        },
        new ModalField[]
        {
            new("workerName", "string",
                "The worker's name exactly as the Workers registry spells it. If more than one "
                + "worker could match what the user said, ask — never guess between two names.",
                Required: true),
            new("date", "string",
                "The worked date as yyyy-MM-dd. Resolve relative dates (\"yesterday\", \"Monday\") "
                + "against today and say the resolved date back in the chat.",
                Required: true),
            new("hours", "number",
                "Hours worked, in half-hour steps of at least 0.5. A full day is 8.",
                Required: true),
            new("costCode", "string",
                "A cost code from this project's list, spelled exactly. If none clearly fits, "
                + "leave it out — the user picks from the dropdown.")
        });

    /// <summary>
    /// The Record absence dialog on the Labour overview — "Frank's on holiday Thursday and
    /// Friday" from the chat. One date per confirm; the assistant stages consecutive days one
    /// after another. Absence explains a missing day (it leaves the chase list) and reduces the
    /// month's projected labour spend at the day rate.
    /// </summary>
    public static readonly ModalDescriptor RecordAbsence = new(
        "record_absence",
        "Record absence",
        "It records one worker's absence on one date: holiday, half day, not worked, or sick. "
        + "The user confirms each day; for a run of days, stage them one at a time.",
        "/labour/overview",
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector,
            Role.ProjectManager
        },
        new ModalField[]
        {
            new("workerName", "string",
                "The worker's name exactly as the Workers registry spells it.",
                Required: true),
            new("date", "string",
                "The absent date as yyyy-MM-dd. Resolve relative dates against today and say the "
                + "resolved date back in the chat.",
                Required: true),
            new("kind", "string",
                "One of: holiday, half-day, not-worked, sick. Defaults to holiday.",
                Required: true),
            new("note", "string",
                "A short optional note — only what the user actually said.")
        });

    /// <summary>
    /// The "Enter a worker's week" dialog on the Labour overview — the accountant's transcription
    /// path for how the crews actually report: a WhatsApp message naming a site per day. One
    /// worker, one week, all seven days in ONE update (the one-dialog-one-update rule from
    /// bid_package_details: never a flow that relies on the model acting again after a save).
    /// Days land as Submitted timesheets on each site's approval queue; the MD codes the cost
    /// code and approves on the project's Labour tab. Days already recorded show locked in the
    /// dialog and are skipped on save — never overwritten.
    /// </summary>
    public static readonly ModalDescriptor WorkerWeek = new(
        "worker_week",
        "Enter a worker's week",
        "It enters ONE worker's whole week — a site (and hours) per day, transcribed from what "
        + "the user has: a WhatsApp attendance message, the conversation, an attached list. Send "
        + "the whole week in ONE update. Each day lands as a Submitted timesheet on its site for "
        + "normal approval — the MD codes and approves it on the project's Labour tab, so leave "
        + "cost codes out unless one clearly applies. Days shown as already recorded are locked; "
        + "leave them alone. For several workers, do one worker per fill: after the user presses "
        + "Save, open this dialog again for the next and keep count out loud.",
        "/labour/overview",
        // Exactly LabourRoleSets.ApproveTimesheets — whoever the API will accept SubmitWorkerWeek
        // from, and nobody else.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector,
            Role.ProjectManager
        },
        new ModalField[]
        {
            new("workerName", "string",
                "The worker's name exactly as the Workers registry spells it. If more than one "
                + "worker could match what the user said, ask — never guess between two names.",
                Required: true),
            new("weekStart", "string",
                "The MONDAY of the week as yyyy-MM-dd. Resolve what the user gave: \"wk ending "
                + "16/08\" is the Sunday, so the Monday is the 10th; \"last week\" resolves "
                + "against today. Say the resolved w/c date back in the chat.",
                Required: true),
            new("days", "array",
                "One item per day the worker worked — weekends included when the message names "
                + "them. Leave out days with nothing reported; days the dialog shows as already "
                + "recorded stay out too. Send the whole week in one update.",
                Required: true,
                ItemFields: new ModalField[]
                {
                    new("date", "string", "The day as yyyy-MM-dd, inside the stated week.", Required: true),
                    new("siteName", "string",
                        "The site as the user said it (\"Guildford\", \"by france\"). The page "
                        + "matches it against the live project list and shows what it could not "
                        + "match, so pass the name through rather than guessing an id — the user "
                        + "picks unmatched sites from the list themselves.",
                        Required: true),
                    new("hours", "number",
                        "Hours worked, in half-hour steps. Leave out for a normal full day — the "
                        + "form defaults to 8."),
                    new("costCode", "string",
                        "A cost code, spelled exactly as list_cost_codes returns it — but ONLY "
                        + "when the user's data actually names the work. Normally leave it out: "
                        + "the MD codes the day when he approves it.")
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
                        "The longer detail for the PO's Description column — optional."),
                    new("costCode", "string",
                        "The cost centre this line's committed value lands on. It must be a Code "
                        + "returned by list_cost_codes, spelled exactly as that tool returned it. "
                        + "If no code clearly fits, leave this out — the user picks it from a "
                        + "list. A wrong cost code sends real money to the wrong place."),
                    new("amount", "number",
                        "The line's value in GBP as a plain number, NET of VAT. Negative only for "
                        + "a credit line. Only figures the correspondence actually states.",
                        Required: true)
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
                        "The longer detail for the PO's Description column — optional."),
                    new("costCode", "string",
                        "The cost centre this line's committed value lands on. It must be a Code "
                        + "returned by list_cost_codes, spelled exactly as that tool returned it. "
                        + "If no code clearly fits, leave this out — the user picks it from a "
                        + "list. A wrong cost code sends real money to the wrong place."),
                    new("amount", "number",
                        "The line's value in GBP as a plain number, NET of VAT. Only figures the "
                        + "correspondence actually states.",
                        Required: true)
                })
        });

    /// <summary>
    /// The "Edit lines" dialog on an APPROVED variation's page (2026-08-25, the valuation loop —
    /// docs/ai/06-context-retrieval.md): "update V01 to the V01 tab of the valuation" means the
    /// assistant reads the tab, opens this dialog pre-filled with the variation's current lines,
    /// sends the corrected build-up, and the user presses Save lines. The save re-prices the
    /// variation's lines on the Valuation Report and moves the CVR and cost-centre budgets by the
    /// difference — the same write-through as editing by hand. Pre-approval variations have no
    /// lines to edit: their estimate is set at approval.
    /// </summary>
    public static readonly ModalDescriptor VariationEditLines = new(
        "variation_edit_lines",
        "Edit variation lines",
        "It edits an APPROVED variation's priced build-up — the lines that stand on the Valuation "
        + "Report under its V-number, each coded to a cost centre — pre-filled with the lines as "
        + "they stand. Read the variation first (get_variation_context gives every current line "
        + "with its valuationLineItemId) and the evidence the user named (the workbook tab, the "
        + "email attachment — read_source), then send the corrected schedule in ONE update. The "
        + "lines sent REPLACE the dialog's list: keep every existing line that is still right and "
        + "carry its valuationLineItemId so its claim history stays attached; a line left out is "
        + "removed. The user reviews everything and presses Save lines themselves; nothing is "
        + "written until they do.",
        "/projects/{project}/variations/{record}",
        // Exactly the page's CanManage set — who may approve and revise a variation.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.ProjectManager,
            Role.QuantitySurveyor
        },
        new ModalField[]
        {
            new("lines", "array",
                "The complete priced schedule as it should stand — this replaces the dialog's list. "
                + "One entry per line: an existing line keeps its valuationLineItemId (from the "
                + "dialog's state or get_variation_context); a new line has none. Every line needs "
                + "a cost centre code from list_cost_codes. Quantity × rate is the line's value, NET "
                + "of VAT; a negative rate is an omit. Only figures the evidence actually states.",
                Required: true,
                ItemFields: new ModalField[]
                {
                    new("valuationLineItemId", "string",
                        "The existing report line this entry re-prices — copy it verbatim from the "
                        + "dialog's state. Leave it out for a line that is new."),
                    new("costCode", "string",
                        "The cost centre code, exactly as list_cost_codes returned it. A wrong "
                        + "code sends real money to the wrong place.", Required: true),
                    new("description", "string",
                        "What the line is — the tab's item wording, short.", Required: true),
                    new("quantity", "number", "The quantity as a plain number (1 for a lump sum).", Required: true),
                    new("rate", "number",
                        "The rate per unit in GBP as a plain number, NET of VAT. Negative for an omit.",
                        Required: true)
                })
        });

    /// <summary>
    /// The "Set % complete" dialog on a project's Valuation Report tab (2026-08-25): "review and
    /// correct the % complete against the valuation" means the assistant reads the report
    /// (get_valuation_context — every line with its id, current % and previous %), reads the
    /// evidence, and puts the corrected percentages into this dialog for the user to check and
    /// press Save. It records entries on the SELECTED claim, which must be a Draft — the page
    /// refuses otherwise, and the dialog's state names the claim.
    /// </summary>
    public static readonly ModalDescriptor ClaimProgress = new(
        "claim_progress",
        "Set % complete",
        "It sets the cumulative % complete on lines of the Valuation Report's selected Draft claim "
        + "— the same act as typing into the report's % column, batched. Read "
        + "get_valuation_context first: it gives every line's valuationLineItemId, its current % "
        + "on the claim and the previous claim's %, and says which claim is selected and whether "
        + "it is Draft. Send only lines whose % should change, as CUMULATIVE percentages (what is "
        + "complete to date, not this period's increment); 0–100 on contract lines, wider on "
        + "variation lines. The entries sent replace the dialog's pending list. The user reviews "
        + "them and presses Save themselves; nothing is recorded until they do.",
        "/projects/{project}/valuation",
        // Exactly the API's gate for recording claim entries (ValuationReportAuthorisation
        // .RolesThatMayRecordClaimEntries): Director, FD, PM, QS — plus administrators.
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
            new("entries", "array",
                "The lines to change, each with its new cumulative % complete — this replaces the "
                + "dialog's pending list. Only lines whose % should change; every entry needs the "
                + "line's valuationLineItemId from get_valuation_context.",
                Required: true,
                ItemFields: new ModalField[]
                {
                    new("valuationLineItemId", "string",
                        "The report line, exactly as get_valuation_context returned it.", Required: true),
                    new("percentComplete", "number",
                        "The cumulative % complete to date as a plain number — 100 for finished. "
                        + "Not the period's increment.", Required: true)
                })
        });

    /// <summary>
    /// The "Agreed build-up" dialog on a PRE-approval variation's page (2026-08-25, rebuilt from the
    /// lost 2026-08-22 design): "update the draft VO to these client-agreed details" on an Issued
    /// variation — the assistant reads the agreed spreadsheet or email, stages the priced lines
    /// (and the narratives) here, and the user presses Stage build-up. Nothing reaches the
    /// Valuation Report: the staged total becomes the estimate and the approve modal opens
    /// pre-seeded with the lines, so approval is a check. Once approved, variation_edit_lines
    /// is the dialog instead.
    /// </summary>
    public static readonly ModalDescriptor VariationBuildUp = new(
        "variation_build_up",
        "Agreed build-up",
        "It stages the client-agreed priced build-up on a variation that is NOT yet approved — "
        + "Quoting, Issued or Awaiting AI — one line per cost centre, plus the VO document's "
        + "narrative sections. Read the variation first (get_variation_context) and the evidence "
        + "the user named (the agreed spreadsheet tab, the client's email — find_in_source, "
        + "read_source), then send the whole schedule in ONE update. The lines sent REPLACE the "
        + "dialog's list; their total becomes the variation's estimate when the user presses "
        + "Stage build-up, and the approve modal opens pre-seeded with them. Nothing is written "
        + "to the Valuation Report — that happens at approval, by the user.",
        "/projects/{project}/variations/{record}",
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.ProjectManager,
            Role.QuantitySurveyor
        },
        new ModalField[]
        {
            new("lines", "array",
                "The complete agreed schedule — this replaces the dialog's list. Every line needs a "
                + "cost centre code from list_cost_codes. Quantity × rate is the line's value, NET of "
                + "VAT; a negative rate is an omit. Only figures the evidence actually states.",
                Required: true,
                ItemFields: new ModalField[]
                {
                    new("costCode", "string",
                        "The cost centre code, exactly as list_cost_codes returned it.", Required: true),
                    new("description", "string", "What the line is — the agreed wording, short.", Required: true),
                    new("quantity", "number", "The quantity as a plain number (1 for a lump sum).", Required: true),
                    new("rate", "number",
                        "The rate per unit in GBP as a plain number, NET of VAT. Negative for an omit.",
                        Required: true)
                }),
            new("commercialBasis", "string",
                "The VO document's commercial basis — what the price is based on. Leave it out to keep "
                + "what stands."),
            new("programmeImpact", "string",
                "The VO document's programme impact. Leave it out to keep what stands."),
            new("exclusions", "string",
                "What the VO expressly does not price. Leave it out to keep what stands.")
        });

    public static IReadOnlyList<ModalDescriptor> All { get; } =
        new[]
        {
            VariationDraft, ManualVariation, ComposeEmail, ReplyEmail, BidPackageDetails, TenderReply,
            ManualTimesheet, RecordAbsence, WorkerWeek, WorkOrderEdit, WorkOrderCreate,
            VariationEditLines, ClaimProgress, VariationBuildUp
        };

    public static ModalDescriptor? Find(string? modalKey) =>
        string.IsNullOrWhiteSpace(modalKey)
            ? null
            : All.FirstOrDefault(modal =>
                string.Equals(modal.ModalKey, modalKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>The dialogs a set of roles may open. Admin passes everything, as it does everywhere
    /// else (SignedInUserResolver grants administrators all roles).</summary>
    public static IReadOnlyList<ModalDescriptor> For(IEnumerable<Role> roles)
    {
        var held = roles as IReadOnlyCollection<Role> ?? roles.ToList();
        return All.Where(modal => CanOpen(modal, held)).ToList();
    }

    public static bool CanOpen(ModalDescriptor modal, IEnumerable<Role> roles) =>
        roles.Any(role => role == Role.Admin || modal.OpenableBy.Contains(role));

    /// <summary>
    /// The dialog's fields as a JSON Schema object, for a tool's input schema. Mirrors the shape
    /// AiToolSchema.Object produces so the two are interchangeable to the Anthropic API.
    /// </summary>
    public static object SchemaFor(ModalDescriptor modal) => BuildObjectSchema(modal.Fields);

    private static object BuildObjectSchema(IReadOnlyList<ModalField> fields)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var field in fields)
        {
            properties[field.Name] = field.ItemFields is { Count: > 0 }
                ? new
                {
                    type = field.Type,
                    description = field.Description,
                    items = BuildObjectSchema(field.ItemFields)
                }
                : (object)new { type = field.Type, description = field.Description };

            if (field.Required) required.Add(field.Name);
        }

        return new
        {
            type = "object",
            properties,
            required = required.ToArray()
        };
    }
}
