using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The rule behind the project picker's Recent group: last opened first, no duplicates, a bounded
// tail. Recency only ever orders the handful of pinned rows — the full list stays in
// ProjectOrdering.InWorkOrder, which is covered by ProjectOrderingTests.
public sealed class RecentProjectsTests
{
    [Fact]
    public void AVisitLeadsTheList()
    {
        var after = RecentProjects.WithVisit(new[] { "by-france", "abbott" }, "woodhouse");

        Assert.Equal(new[] { "woodhouse", "by-france", "abbott" }, after);
    }

    [Fact]
    public void RevisitingMovesAProjectToTheFront_withoutDuplicatingIt()
    {
        // By France → Abbott → By France again: the picker should read By France, Abbott —
        // one entry each, most recent first.
        var after = RecentProjects.WithVisit(new[] { "abbott", "by-france" }, "by-france");

        Assert.Equal(new[] { "by-france", "abbott" }, after);
    }

    [Fact]
    public void RevisitDeduplicationIgnoresCase_likeEveryOtherProjectIdComparison()
    {
        var after = RecentProjects.WithVisit(new[] { "By-France", "abbott" }, "by-france");

        Assert.Equal(new[] { "by-france", "abbott" }, after);
    }

    [Fact]
    public void TheListNeverOutgrowsMaxRemembered()
    {
        var remembered = new List<string>();
        for (var visit = 0; visit < RecentProjects.MaxRemembered + 3; visit++)
        {
            remembered = RecentProjects.WithVisit(remembered, $"project-{visit}");
        }

        Assert.Equal(RecentProjects.MaxRemembered, remembered.Count);
        // The freshest visits survive; the earliest fall off the tail.
        Assert.Equal($"project-{RecentProjects.MaxRemembered + 2}", remembered[0]);
        Assert.DoesNotContain("project-0", remembered);
    }

    [Fact]
    public void FirstEverVisitSeedsTheList()
    {
        Assert.Equal(new[] { "by-france" }, RecentProjects.WithVisit(Array.Empty<string>(), "by-france"));
    }
}
