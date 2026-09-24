namespace Jewel.JPMS.Models;

/// <summary>
/// Shared role sets for endpoint authorisation — and for the pages that render what those
/// endpoints serve. A page names the set its primary read is gated by (Page.OpenTo), so the
/// two doors read one constant and cannot drift. The floor for every endpoint is a role check —
/// "is signed in" alone is never enough, because external logins (subcontractor portal, and in
/// future clients/architects) carry valid session cookies too. Administrators pass every gate
/// (SignedInUserResolver grants them all roles).
///
/// Guidance (see docs/05-data-model/permissions-matrix.md): external roles are never owners of
/// internal workflows — default internal queries to AllInternal and add external roles only where
/// the matrix names them (e.g. Architect on RFI/variation approval reads, Subcontractor on
/// drawings for their assigned work).
/// </summary>
public static class JpmsRoleSets
{
    /// <summary>Every internal (JBB staff) role. Mirrors DesktopNavigation.AllInternalRoles.
    /// Accounts belongs here — it is staff, and this is the floor that lets it read its own
    /// to-dos (ListMyTodoItems) — but NOT in CommercialTeam below, which is the money-facing
    /// reach the role is deliberately without.</summary>
    public static readonly RoleSet AllInternal = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing,
        JpmsRoles.Foreman,
        JpmsRoles.Accounts);

    /// <summary>The internal team who deliver the project: the reads of requests, RFIs,
    /// variations and their conversations. Internal only — those reads return the business's own
    /// correspondence and notes, and no external login ever reaches them. An external party reads
    /// its own records through its own portal's scoped reads.
    ///
    /// NOT a superset of AllInternal: Accounts is deliberately absent here and from DrawingReaders
    /// below. Those two sets gate the request/RFI/submittal/variation reads and the drawing
    /// downloads — project delivery, which Accounts has no part in and no UI for (it holds no
    /// DesktopNavigation.ProjectRoles rows). Adding it to "keep the lists in step" would widen the
    /// role well past the to-do list it exists for.</summary>
    public static readonly RoleSet ProjectDeliveryTeam = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing,
        JpmsRoles.Foreman);

    /// <summary>Internal roles plus the architect — the WRITES the architect makes on RFIs,
    /// submittals and variations per the permissions matrix, each scoped to their own practice's
    /// projects. Never a read gate: see ProjectDeliveryTeam.</summary>
    public static readonly RoleSet InternalAndArchitect = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing,
        JpmsRoles.Foreman,
        JpmsRoles.Architect);

    /// <summary>Drawing readers: internal roles plus the subcontractor, who reads revisions for
    /// their assigned work (P10). The architect reads drawings through the architect portal's
    /// scoped reads, never through this set, which is not scoped to a practice's projects.</summary>
    public static readonly RoleSet DrawingReaders = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing,
        JpmsRoles.Foreman,
        JpmsRoles.Subcontractor);

    /// <summary>The commercial team: money-facing reads (cashflow, Xero, ledger detail) that the
    /// wider site team has no business seeing. Mirrors the Xero/Allocation navigation gates.</summary>
    public static readonly RoleSet CommercialTeam = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator);

    /// <summary>Administrators alone: the true administrator test. A non-administrator's
    /// expanded role list can never contain Role.Admin (UserRoles.Expand).</summary>
    public static readonly RoleSet Administrators = RoleSet.Of(Role.Admin);

    /// <summary>The admin-only surfaces (user directory, invites, access requests, the system
    /// page): administrators, and Finance Directors, who hold the same PERMISSIONS without being
    /// linked to the Admin identity. This is AdminGate's rule.</summary>
    public static readonly RoleSet AdministratorsAndFinanceDirector = RoleSet.Of(Role.Admin, JpmsRoles.FinanceDirector);

    /// <summary>Every role there is — for a page every approved sign-in may open (the role home,
    /// a person's own sign-offs and connections), where the API scopes by the caller rather than
    /// by role.</summary>
    public static readonly RoleSet Everyone = RoleSet.Of(Enum.GetValues<Role>());

    /// <summary>The client portal: a Client login, which reads its own client's records and
    /// nothing else (ClientScope). A Client role without a ClientId reaches nothing.</summary>
    public static readonly RoleSet ClientPortal = RoleSet.Of(JpmsRoles.Client);

    /// <summary>The subcontractor portal: a Subcontractor login, which reads its own directory
    /// record and its own work orders (SubcontractorScope).</summary>
    public static readonly RoleSet SubcontractorPortal = RoleSet.Of(JpmsRoles.Subcontractor);
}
