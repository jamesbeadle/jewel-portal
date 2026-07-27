using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The one order projects are listed in across JPMS — applied once in ListProjectsVisibleToUser and
// re-applied by the callers that narrow the list. The rule the UI depends on: what is on site now
// comes first, Completed never disappears but always sits last.
public sealed class ProjectOrderingTests
{
    private static Project Make(string name, ProjectStage stage, string reference = "JBB-2026-000") =>
        new(
            ProjectId: name.ToLowerInvariant().Replace(" ", "-"),
            Reference: reference,
            Name: name,
            ClientName: "Client",
            Organisation: Organisation.JewelBespokeBuild,
            Stage: stage,
            ProjectManagerEmail: "pm@jewelbb.co.uk",
            CreatedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void LiveStagesShareOneBand_soAStageChangeDoesNotReshuffleTheList()
    {
        // Pre-Construction through Close-Out all rank 0: a project moving from Procurement to
        // Mobilisation must not jump up the dropdown mid-build.
        Assert.Equal(0, ProjectStage.PreConstruction.WorkRank());
        Assert.Equal(0, ProjectStage.Procurement.WorkRank());
        Assert.Equal(0, ProjectStage.Mobilisation.WorkRank());
        Assert.Equal(0, ProjectStage.LiveDelivery.WorkRank());
        Assert.Equal(0, ProjectStage.CloseOut.WorkRank());
    }

    [Fact]
    public void BandsRunLiveThenDefectsThenLeadsThenCompleted()
    {
        Assert.True(ProjectStage.LiveDelivery.WorkRank() < ProjectStage.DefectsPeriod.WorkRank());
        Assert.True(ProjectStage.DefectsPeriod.WorkRank() < ProjectStage.Lead.WorkRank());
        Assert.True(ProjectStage.Lead.WorkRank() < ProjectStage.Completed.WorkRank());
    }

    [Fact]
    public void LiveProjectsComeFirstAndCompletedLast()
    {
        var ordered = new[]
        {
            Make("Zeta Road", ProjectStage.Completed),
            Make("Alpha Avenue", ProjectStage.Lead),
            Make("Beresford Road", ProjectStage.DefectsPeriod),
            Make("Woodhouse", ProjectStage.LiveDelivery),
            Make("Abbot Road", ProjectStage.Mobilisation),
        }.InWorkOrder().Select(project => project.Name).ToList();

        Assert.Equal(
            new[] { "Abbot Road", "Woodhouse", "Beresford Road", "Alpha Avenue", "Zeta Road" },
            ordered);
    }

    [Fact]
    public void WithinABandProjectsAreAlphabeticalRegardlessOfCase()
    {
        var ordered = new[]
        {
            Make("woodhouse", ProjectStage.LiveDelivery),
            Make("Abbot Road", ProjectStage.CloseOut),
            Make("newnham Ave", ProjectStage.Procurement),
        }.InWorkOrder().Select(project => project.Name).ToList();

        Assert.Equal(new[] { "Abbot Road", "newnham Ave", "woodhouse" }, ordered);
    }

    [Fact]
    public void SharedNamesAreSeparatedByReference()
    {
        // Phase 1 / Phase 2 on one road: the reference keeps the pair in a stable, readable order
        // rather than letting them swap between renders.
        var ordered = new[]
        {
            Make("Abbot Road", ProjectStage.LiveDelivery, "JBB-2026-014"),
            Make("Abbot Road", ProjectStage.LiveDelivery, "JBB-2026-002"),
        }.InWorkOrder().Select(project => project.Reference).ToList();

        Assert.Equal(new[] { "JBB-2026-002", "JBB-2026-014" }, ordered);
    }
}
