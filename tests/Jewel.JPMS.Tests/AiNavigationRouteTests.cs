using Jewel.JPMS.Api.Features.Ai;
using Xunit;

namespace Jewel.JPMS.Tests;

// Pins the pure half of navigate_to's server-side check (AiNavigationRoute). The live failure
// this guards against (2026-08-25): "load By France RFIs" from an Abbot Road page went out as the
// site-map template /projects/{project}/requests/rfis, the browser landed on "Project not found",
// and the model — told ok:true — narrated the By France register it never reached. The runner's
// database look-up is not exercised here; what is pinned is how a route's project segment is read,
// matched and rewritten, which is where every one of the model's mistakes has to be caught.
public sealed class AiNavigationRouteTests
{
    private const string ByFranceId = "3490f944b29545c4b8d5a04130f42ab8";
    private const string AbbotRoadId = "7d1c5e0a2b3f4c6d8e9f0a1b2c3d4e5f";

    private static readonly IReadOnlyList<AiNavigationRoute.Candidate> Projects = new[]
    {
        new AiNavigationRoute.Candidate(ByFranceId, "JBB-2026-001", "By France"),
        new AiNavigationRoute.Candidate(AbbotRoadId, "JBB-2026-002", "Abbot Road"),
        new AiNavigationRoute.Candidate("a1", "JBB-2025-014", "Beresford Road Phase 1"),
        new AiNavigationRoute.Candidate("a2", "JBB-2025-015", "Beresford Road Phase 2"),
    };

    // ---- reading the project segment ----

    [Theory]
    [InlineData("/projects/{project}/requests/rfis", "{project}")]
    [InlineData("/projects/%7Bproject%7D/requests/rfis", "{project}")]
    [InlineData("/projects/JBB-2026-001/requests/rfis", "JBB-2026-001")]
    [InlineData("/projects/By%20France/requests/rfis", "By France")]
    [InlineData("/projects/3490f944b29545c4b8d5a04130f42ab8", "3490f944b29545c4b8d5a04130f42ab8")]
    [InlineData("/projects/3490f944b29545c4b8d5a04130f42ab8?tab=official", "3490f944b29545c4b8d5a04130f42ab8")]
    [InlineData("/Projects/abc/defects", "abc")]
    public void ProjectSegment_isTheDecodedPieceAfterProjects(string route, string expected) =>
        Assert.Equal(expected, AiNavigationRoute.ProjectSegment(route));

    [Theory]
    [InlineData("/projects")]
    [InlineData("/projects/")]
    [InlineData("/rfis")]
    [InlineData("/control-centre?tab=queue")]
    [InlineData("/todos/abc")]
    public void ProjectSegment_isNullOffAProjectRoute(string route) =>
        Assert.Null(AiNavigationRoute.ProjectSegment(route));

    [Theory]
    [InlineData("{project}", true)]
    [InlineData("{id}", true)]
    [InlineData("{projectId}", true)]
    [InlineData("JBB-2026-001", false)]
    [InlineData("3490f944b29545c4b8d5a04130f42ab8", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPlaceholder_meansBraces(string? segment, bool expected) =>
        Assert.Equal(expected, AiNavigationRoute.IsPlaceholder(segment));

    // ---- portal paths only ----

    [Theory]
    [InlineData("/projects/abc/requests/rfis", true)]
    [InlineData("/rfis", true)]
    [InlineData("https://portal.jewelbb.co.uk/rfis", false)]
    [InlineData("//evil.example/rfis", false)]
    [InlineData("/\\evil.example/rfis", false)]
    [InlineData("projects/abc", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPortalPath_isASingleLeadingSlash(string? route, bool expected) =>
        Assert.Equal(expected, AiNavigationRoute.IsPortalPath(route));

    // ---- rewriting ----

    [Fact]
    public void WithProject_replacesOnlyTheProjectSegment_andKeepsTheRest()
    {
        Assert.Equal($"/projects/{ByFranceId}/requests/rfis",
            AiNavigationRoute.WithProject("/projects/{project}/requests/rfis", ByFranceId));
        Assert.Equal($"/projects/{ByFranceId}/requests/rfis?tab=official#top",
            AiNavigationRoute.WithProject("/projects/JBB-2026-001/requests/rfis?tab=official#top", ByFranceId));
        Assert.Equal($"/projects/{ByFranceId}",
            AiNavigationRoute.WithProject("/projects/By%20France", ByFranceId));
    }

    [Fact]
    public void WithProject_leavesARouteWithNoProjectSegmentAlone()
    {
        Assert.Equal("/rfis", AiNavigationRoute.WithProject("/rfis", ByFranceId));
        Assert.Equal("/projects", AiNavigationRoute.WithProject("/projects", ByFranceId));
    }

    // ---- leftover placeholders (record templates sent instead of a tool's route) ----

    [Theory]
    [InlineData("/projects/3490f944b29545c4b8d5a04130f42ab8/variations/{id}", "{id}")]
    [InlineData("/projects/3490f944b29545c4b8d5a04130f42ab8/variations/%7Bid%7D", "{id}")]
    [InlineData("/projects/{project}/requests/view/{id}", "{project}")]
    public void FirstPlaceholder_findsWhatIsLeftInBraces(string route, string expected) =>
        Assert.Equal(expected, AiNavigationRoute.FirstPlaceholder(route));

    [Theory]
    [InlineData("/projects/3490f944b29545c4b8d5a04130f42ab8/requests/rfis")]
    [InlineData("/rfis")]
    // A brace in the QUERY is a value, not a template — only the path is inspected.
    [InlineData("/control-centre?q=%7Bx%7D")]
    public void FirstPlaceholder_isNullWhenThePathIsClean(string route) =>
        Assert.Null(AiNavigationRoute.FirstPlaceholder(route));

    // ---- matching the segment to a project ----

    [Fact]
    public void Resolve_findsByRealId_first()
    {
        var match = AiNavigationRoute.Resolve(ByFranceId, Projects);
        Assert.True(match.Found);
        Assert.Equal("By France", match.Project!.Name);
    }

    [Theory]
    [InlineData("JBB-2026-001")]
    [InlineData("jbb-2026-001")]
    [InlineData(" JBB-2026-001 ")]
    public void Resolve_findsByReference_ignoringCaseAndSpace(string segment)
    {
        var match = AiNavigationRoute.Resolve(segment, Projects);
        Assert.True(match.Found);
        Assert.Equal(ByFranceId, match.Project!.ProjectId);
    }

    [Theory]
    [InlineData("By France")]
    [InlineData("by france")]
    [InlineData("France")]
    public void Resolve_findsByName_exactOrUniqueContains(string segment)
    {
        var match = AiNavigationRoute.Resolve(segment, Projects);
        Assert.True(match.Found);
        Assert.Equal(ByFranceId, match.Project!.ProjectId);
    }

    [Fact]
    public void Resolve_refusesAContainsMatchThatFitsTwoJobs()
    {
        var match = AiNavigationRoute.Resolve("Beresford", Projects);
        Assert.False(match.Found);
        Assert.True(match.IsAmbiguous);
        Assert.Equal(2, match.Ambiguous.Count);
    }

    [Fact]
    public void Resolve_prefersTheExactNameOverAContainsMatch()
    {
        var match = AiNavigationRoute.Resolve("Beresford Road Phase 2", Projects);
        Assert.True(match.Found);
        Assert.Equal("a2", match.Project!.ProjectId);
    }

    [Theory]
    [InlineData("Windy Ridge")]
    [InlineData("JBB-2030-999")]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_findsNothingForAnUnknownSegment(string segment)
    {
        var match = AiNavigationRoute.Resolve(segment, Projects);
        Assert.False(match.Found);
        Assert.False(match.IsAmbiguous);
    }

    // ---- the live failure, end to end through the pure half ----

    [Fact]
    public void TheLiveFailure_aTemplateRouteForAnotherProject_isRewrittenOnceTheNameResolves()
    {
        // What the model sent while Abbot Road was in view and the user said "load By France RFIs".
        const string sent = "/projects/By France/requests/rfis";

        var segment = AiNavigationRoute.ProjectSegment(sent);
        Assert.False(AiNavigationRoute.IsPlaceholder(segment));
        var match = AiNavigationRoute.Resolve(segment!, Projects);
        Assert.True(match.Found);

        var rewritten = AiNavigationRoute.WithProject(sent, match.Project!.ProjectId);
        Assert.Equal($"/projects/{ByFranceId}/requests/rfis", rewritten);
        Assert.Null(AiNavigationRoute.FirstPlaceholder(rewritten));
    }
}
