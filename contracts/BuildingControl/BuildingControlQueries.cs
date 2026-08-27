using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.BuildingControl;

/// <summary>
/// Everything the Building Control tab renders, in one answer: the project's cases (usually one —
/// newest first, the active one leading), the inspection stages in running order, and every file
/// on the case or its inspections. The inspection detail page slices this same answer client-side.
/// </summary>
public sealed record BuildingControlProjectView(
    IReadOnlyList<BuildingControlCase> Cases,
    IReadOnlyList<BuildingControlInspection> Inspections,
    IReadOnlyList<BuildingControlAttachment> Attachments);

public sealed record GetBuildingControlForProject(
    string ProjectId) : IQuery<BuildingControlProjectView>;
