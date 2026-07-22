using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Audit;

// The audit register, newest first: every recorded client-facing interaction (triage decisions,
// records created/linked from email, drafted correspondence, wall refusals, snapshots). All filters
// optional and combinable. Cursor is a plain offset ("25", "50", …).
public sealed record ListAuditEvents(
    string? ProjectId = null,
    string? Pathway = null,
    AuditEventType? EventType = null,
    string? ActorEmail = null,
    string? Cursor = null,
    int Take = 50) : IQuery<AuditEventsPage>;
