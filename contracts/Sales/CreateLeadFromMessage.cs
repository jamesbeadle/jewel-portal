using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// Raise a lead from an enquiry email in the Control Centre — the Sales pane's "create new"
/// (2026-09-15, Nigel: an estimate enquiry forwarded to the projects mailbox is tagged to the lead
/// it is about, or to a new one). The lead itself is exactly a <see cref="CaptureLead"/> landing
/// Engaged with Source = Inbound — an enquiry is already a conversation — and this command
/// additionally links the originating email to it via the shared record-link tag
/// ("JPMS/LD-####"), so the lead reads its mail back live like every other record. A lead
/// belongs to no project, so unlike the other from-message commands there is no ProjectId.
/// </summary>
public sealed record CreateLeadFromMessage(
    string MessageId,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CompanyName,
    LeadProspectKind ProspectKind,
    string PropertyAddress,
    string Postcode,
    string Summary,
    string Notes,
    decimal? EstimatedValue,
    // Portal email of the staff member who will work the lead; blank = whoever tags it.
    string OwnerEmail = "",
    string? InternetMessageId = null,
    LinkThreadScope Scope = LinkThreadScope.ThreadBehindAnchor,
    bool AllowCrossPathway = false) : ICommand<Lead>;
