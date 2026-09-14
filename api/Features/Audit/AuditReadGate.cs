using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)

namespace Jewel.JPMS.Api.Features.Audit;

// Who may read the audit register, decided by the SHAPE of the read — one rule for the HTTP
// endpoint and the connector's list_audit_trail, so the assistant sees exactly what the page shows.
//
// The whole register is an oversight surface, gated like triage. A read narrowed to ONE record is
// that record's own history — the History panel on the request page — so it opens to the internal
// team: the people who can draft the correspondence must be able to see that it was drafted. A read
// narrowed to the finance reconciliation event (cost-centre recodes) is a money-facing register, not
// a triage one — it opens to the commercial team, mirroring the sidebar's Financials gate. The KPI
// register is administrators-only (2026-09-03); its rows carry only a reference, but a read NARROWED
// to them is a read of the register's rhythm — refused to everyone else.
internal static class AuditReadGate
{
    public static bool Allows(SignedInUser user, string? recordId, AuditEventType? eventType)
    {
        if (user.Roles.Contains(Role.Admin)) return true;
        if (IsAdministratorsOnly(eventType)) return false;
        return RolesFor(recordId, eventType).IncludesAny(user.Roles);
    }

    public static RoleSet RolesFor(string? recordId, AuditEventType? eventType) =>
        !string.IsNullOrWhiteSpace(recordId) ? JpmsRoleSets.AllInternal
        : eventType == AuditEventType.CostCentreRecoded ? JpmsRoleSets.CommercialTeam
        : TriageRoles.AllowedToTriage;

    private static bool IsAdministratorsOnly(AuditEventType? eventType) =>
        eventType is AuditEventType.KpiEmailMarked or AuditEventType.KpiEmailRemoved;
}
