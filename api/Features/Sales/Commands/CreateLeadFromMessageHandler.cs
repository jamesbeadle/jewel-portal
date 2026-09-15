using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// Raises a lead from an enquiry email and links the email to it (the Sales pane's "create new",
// 2026-09-15). The lead is captured by the SAME handler as a hand-typed one — numbering and the
// first timeline entry are one set of rules whichever door it came in through — landing Engaged
// with Source = Inbound, because an enquiry is already a conversation. Then the originating
// email is tagged to it through the shared record-link path, exactly like
// CreateInventoryItemFromMessage. The lead is persisted first because the link path resolves the
// record from the database; a link failure therefore throws with the lead already saved, and the
// email stays in the queue to retry against the existing lead.
public sealed class CreateLeadFromMessageHandler : ICommandHandler<CreateLeadFromMessage, Lead>
{
    private readonly ICommandHandler<CaptureLead, Lead> capture;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;
    private readonly AuditActor actor;

    public CreateLeadFromMessageHandler(
        ICommandHandler<CaptureLead, Lead> capture,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> link,
        AuditActor actor)
    {
        this.capture = capture;
        this.link = link;
        this.actor = actor;
    }

    public async Task<Lead> HandleAsync(CreateLeadFromMessage command, CancellationToken cancellationToken)
    {
        var lead = await capture.HandleAsync(
            new CaptureLead(
                command.ContactName,
                command.ContactEmail,
                command.ContactPhone,
                command.CompanyName,
                command.ProspectKind,
                command.PropertyAddress,
                command.Postcode,
                command.Summary,
                command.Notes,
                LeadSource.Inbound,
                StrategyId: null,
                command.EstimatedValue,
                OwnerFor(command),
                LeadStage.Engaged),
            cancellationToken);

        await link.HandleAsync(
            new LinkMessageToRecord(
                command.MessageId, RecordType.Lead, lead.LeadId, command.InternetMessageId,
                Pathway: "Sales",
                AllowCrossPathway: command.AllowCrossPathway,
                Scope: command.Scope),
            cancellationToken);

        return lead;
    }

    // The owner named on the command, else whoever is tagging the email — the signed-in actor
    // every gate (HTTP and MCP) stamps before a handler runs.
    private string OwnerFor(CreateLeadFromMessage command) =>
        string.IsNullOrWhiteSpace(command.OwnerEmail) ? actor.Email : command.OwnerEmail.Trim();
}
