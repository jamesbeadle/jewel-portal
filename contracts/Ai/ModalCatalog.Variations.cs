using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;


public static partial class ModalCatalog
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

}
