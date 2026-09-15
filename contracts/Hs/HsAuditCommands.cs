using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Hs;

/// <summary>
/// The editable face of an audit's header — shared by create and update so the two routes cannot
/// drift apart. InspectionDate is a UK-local calendar date stored as midnight UTC.
/// </summary>
public sealed record HsAuditDetails(
    HsAuditType Type,
    DateTimeOffset InspectionDate,
    string SiteManagerName,
    string SafetyOfficerName,
    string SummaryOfWorkActivities,
    int? SiteOperativeCount,
    string FurtherComments);

/// <summary>Starts an audit on the project: the header, plus every item of the current
/// framework planted blank (HsAuditTemplate). CreatedByEmail is stamped server-side.</summary>
public sealed record CreateHsAudit(
    string ProjectId,
    HsAuditDetails Details,
    string CreatedByEmail = "") : ICommand<HsAudit>;

public sealed record UpdateHsAuditDetails(
    string HsAuditId,
    HsAuditDetails Details) : ICommand<HsAudit>;

/// <summary>What the officer found on one item — every field of the row, replacing the item's
/// current values wholesale (carry forward what should not change).</summary>
public sealed record HsAuditItemEntry(
    string HsAuditItemId,
    HsAuditComment? Comment,
    HsAuditRate? Rate,
    HsAuditClass? Class,
    int Minus,
    HsAuditTimeScale? TimeScale,
    string Findings,
    string OwnerName,
    DateTimeOffset? DateRectified);

/// <summary>Writes the named items (any number, any section); items not named are untouched.
/// The audit's score is recomputed from every item after the write. Refused on a Closed audit.</summary>
public sealed record UpdateHsAuditItems(
    string HsAuditId,
    IReadOnlyList<HsAuditItemEntry> Items) : ICommand<HsAuditView>;

/// <summary>The officer's declaration: the report is a true reflection of the site and has been
/// explained to the manager. Moves Draft → Issued and mints one corrective action on the
/// project's H&S register for every finding — an item with an owner or a rate below full marks,
/// not marked N/A (HsAuditCorrectiveActions). IssuedByEmail is stamped server-side.</summary>
public sealed record IssueHsAudit(
    string HsAuditId,
    string IssuedByEmail = "") : ICommand<HsAuditView>;

/// <summary>The manager's declaration that every action is done. Refused while a corrective
/// action minted from this audit is still open.</summary>
public sealed record CloseHsAudit(
    string HsAuditId,
    string ManagerName) : ICommand<HsAudit>;
