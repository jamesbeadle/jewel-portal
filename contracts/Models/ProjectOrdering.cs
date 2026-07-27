namespace Jewel.JPMS.Models;

/// <summary>
/// The one order projects are listed in, everywhere: the work that is running now first.
///
/// Every project list and dropdown in JPMS reads its projects from the same query
/// (ListProjectsVisibleToUser), so the order is applied there once and inherited by the side-nav
/// switcher, the project header's prev/next arrows, the Xero allocation filters and every other
/// picker. Callers that narrow the list (e.g. "active projects only") re-apply
/// <see cref="InWorkOrder"/> after their Where so the order survives the filter; nothing sorts
/// projects by its own rule.
///
/// The rank is deliberately coarse — four bands, not eight — because the point is "am I still
/// building this?", not the exact stage. Sites in flight sort together (a project moving from
/// Procurement to Mobilisation must not jump the list); Defects Period sits below them because the
/// site is finished but the file is not; Leads are below that (they carry no costs and no
/// programme yet); Completed is last but never hidden — historical costs still land on it.
/// </summary>
public static class ProjectOrdering
{
    /// <summary>How live the stage is: 0 = on site, 3 = done. Lower sorts first.</summary>
    public static int WorkRank(this ProjectStage stage) => stage switch
    {
        ProjectStage.PreConstruction  => 0,
        ProjectStage.Procurement      => 0,
        ProjectStage.Mobilisation     => 0,
        ProjectStage.LiveDelivery     => 0,
        ProjectStage.CloseOut         => 0,
        ProjectStage.DefectsPeriod    => 1,
        ProjectStage.Lead             => 2,
        ProjectStage.Completed        => 3,
        _ => 2
    };

    /// <summary>
    /// Live projects first, then Defects Period, then Leads, then Completed — each band A–Z by
    /// name, with the reference as the tie-break so two projects sharing a name (Phase 1 / Phase 2
    /// on one road) keep a stable, readable order rather than swapping between renders.
    /// </summary>
    public static IOrderedEnumerable<Project> InWorkOrder(this IEnumerable<Project> projects) =>
        projects
            .OrderBy(project => project.Stage.WorkRank())
            .ThenBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(project => project.Reference, StringComparer.OrdinalIgnoreCase);
}
