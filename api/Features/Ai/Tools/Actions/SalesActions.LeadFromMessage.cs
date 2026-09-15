using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The Sales pane over the connector (2026-09-15, Nigel): an estimate enquiry forwarded to the
/// projects mailbox is tagged to the lead it is about — an existing one (file_email_to_record,
/// type Lead, once LeadLinkProvider exists there is nothing else to add) or a new one raised
/// from the email, which is this action. Gated as every triage decision is.
/// </summary>
internal sealed partial class SalesActions
{
    private static IEnumerable<AiAction> LeadFromMessageActions() => new AiAction[]
    {
        new AiAction(
            Name: "create_lead_from_message",
            Area: Area,
            Description: "Raises a sales lead FROM a mailbox email (the Control Centre's Sales pane, "
                + "\"create new\"): captures the lead exactly like capture_lead — landing Engaged "
                + "with source Inbound, because an enquiry is already a conversation — and links "
                + "the originating email to it by its LD-#### tag, so the lead reads its mail back "
                + "like every other record. A lead belongs to no project.",
            CommandType: typeof(CreateLeadFromMessage),
            ResultType: typeof(Lead),
            AuthorisationType: typeof(CreateLeadFromMessageAuthorisation),
            ValidationType: typeof(CreateLeadFromMessageValidation),
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Read list_leads first — if the person or property is already a lead, tag the "
                + "email to it with file_email_to_record (type Lead, recordId = its leadId) instead "
                + "of raising a second one. messageId is the mailbox message id; scope says how far "
                + "the tag spreads across the conversation (default ThreadBehindAnchor). Send "
                + "allowCrossPathway true — the pane choice is the decision and the guard is a "
                + "no-op. ownerEmail blank = whoever is signed in. " + LeadFieldNotes,
            RequiresConfirmation: true)
    };
}
