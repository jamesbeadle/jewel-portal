using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Cost centre groups roll several cost centres up into one named line on the
/// Financials tab, so related centres (e.g. aluminium windows and specialist
/// glazing) can be compared as one. Saved per project and shared by everyone
/// viewing it; a cost centre belongs to at most one group per project.
/// </summary>
public sealed record ListCostCentreGroupsForProject(string ProjectId) : IQuery<IReadOnlyList<CostCentreGroup>>;

/// <summary>Creates a named roll-up from two or more cost centres. Rejected when any
/// of the centres already belongs to another group on this project — unless that group
/// is listed in <paramref name="ReplaceGroupIds"/>: those groups are dissolved and their
/// members absorbed in the same save, which is how an existing roll-up is grown or two
/// roll-ups are merged without an ungroup-and-redo dance.</summary>
public sealed record CreateCostCentreGroup(
    string ProjectId,
    string Name,
    IReadOnlyList<string> CostCodes,
    IReadOnlyList<string>? ReplaceGroupIds = null) : ICommand<CostCentreGroup>;

/// <summary>Dissolves a roll-up; its cost centres return to individual rows.
/// Nothing else is deleted — the group is presentation only.</summary>
public sealed record RemoveCostCentreGroup(
    string ProjectId,
    string CostCentreGroupId) : ICommand<Acknowledgement>;
