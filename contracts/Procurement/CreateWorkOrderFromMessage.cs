using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Raise a work order from a tagged mailbox message — the Control Centre's "create new" for the
// Subcontractor pathway, alongside CreateBidPackageFromMessage. The order itself is exactly a
// CreateManualWorkOrder (same lines/programme/deposit/draft semantics, same validation and
// numbering); this command additionally links the originating email to the new order via the
// shared record-link tag, so the order reads its mail back live like every other record.
// RaisedByEmail is stamped server-side from the signed-in user; InternetMessageId lets the link
// re-find the message if its Graph id has changed since the queue was rendered.
public sealed record CreateWorkOrderFromMessage(
    string MessageId,
    string ProjectId,
    string SubcontractorId,
    string Title,
    string Scope,
    IReadOnlyList<ManualWorkOrderLine> Lines,
    DateTimeOffset? ProgrammeStart = null,
    DateTimeOffset? TargetCompletion = null,
    string ProgrammeNotes = "",
    bool SaveAsDraft = false,
    bool DepositRequired = false,
    decimal? DepositPercent = null,
    string? InternetMessageId = null,
    string RaisedByEmail = "") : ICommand<WorkOrder>;
