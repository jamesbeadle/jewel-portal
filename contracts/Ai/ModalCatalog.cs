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

    public static IReadOnlyList<ModalDescriptor> All { get; } = new[] { VariationDraft, ManualVariation, ComposeEmail };

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
