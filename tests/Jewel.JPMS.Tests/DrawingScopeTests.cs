using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-29, Nigel: "architects may need to see drawings on an RFI". The drawing reads admit the
// architect's login and DrawingScope confines every one of them — the register, a drawing, its
// revisions, the file, the extraction — to the projects the login was given. Staff and the
// subcontractor read the register as they always have.
public sealed class DrawingScopeTests
{
    private const string ArchitectLogin = "architect@practice.example";
    private const string TheirProject = "P-THEIRS";
    private const string AnotherProject = "P-SOMEONE-ELSES";
    private const string TheirDrawing = "drw-theirs";
    private const string AnotherDrawing = "drw-someone-elses";
    private const string TheirRevision = "rev-theirs";
    private const string AnotherRevision = "rev-someone-elses";

    [Fact]
    public async Task AnArchitect_readsTheDrawingsOfTheirOwnProjects_only()
    {
        await using var context = await SeededContextAsync();
        var architect = Signed(Role.Architect, ArchitectLogin);

        Assert.True(await DrawingScope.MayReadProjectAsync(context, architect, TheirProject, CancellationToken.None));
        Assert.True(await DrawingScope.MayReadDrawingAsync(context, architect, TheirDrawing, CancellationToken.None));
        Assert.True(await DrawingScope.MayReadRevisionAsync(context, architect, TheirRevision, CancellationToken.None));
        Assert.False(await DrawingScope.MayReadProjectAsync(context, architect, AnotherProject, CancellationToken.None));
        Assert.False(await DrawingScope.MayReadDrawingAsync(context, architect, AnotherDrawing, CancellationToken.None));
        Assert.False(await DrawingScope.MayReadRevisionAsync(context, architect, AnotherRevision, CancellationToken.None));
    }

    [Fact]
    public async Task AnArchitectWithNoGrant_readsNoDrawing()
    {
        await using var context = await SeededContextAsync();
        var stranger = Signed(Role.Architect, "nobody@practice.example");

        Assert.False(await DrawingScope.MayReadDrawingAsync(context, stranger, TheirDrawing, CancellationToken.None));
        Assert.False(await DrawingScope.MayReadRevisionAsync(context, stranger, "rev-unknown", CancellationToken.None));
    }

    [Fact]
    public async Task StaffAndTheSubcontractor_readTheWholeRegister()
    {
        await using var context = await SeededContextAsync();

        Assert.True(await DrawingScope.MayReadDrawingAsync(context, Signed(Role.SiteManager), AnotherDrawing, CancellationToken.None));
        Assert.True(await DrawingScope.MayReadRevisionAsync(context, Signed(Role.Subcontractor), AnotherRevision, CancellationToken.None));
    }

    private static SignedInUser Signed(Role role, string email = "someone@example.com") =>
        new(email, "Someone", new[] { role });

    private static async Task<JpmsContext> SeededContextAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"drawing-scope-{Guid.NewGuid():N}").Options);
        context.Projects.AddRange(
            new ProjectEntity { ProjectId = TheirProject, Stage = (int)ProjectStage.LiveDelivery },
            new ProjectEntity { ProjectId = AnotherProject, Stage = (int)ProjectStage.LiveDelivery });
        context.ProjectAccessGrants.Add(
            new ProjectAccessGrantEntity { ProjectAccessGrantId = "g-1", Email = ArchitectLogin, ProjectId = TheirProject });
        context.Drawings.AddRange(
            new DrawingEntity { DrawingId = TheirDrawing, ProjectId = TheirProject },
            new DrawingEntity { DrawingId = AnotherDrawing, ProjectId = AnotherProject });
        context.DrawingRevisions.AddRange(
            new DrawingRevisionEntity { DrawingRevisionId = TheirRevision, DrawingId = TheirDrawing },
            new DrawingRevisionEntity { DrawingRevisionId = AnotherRevision, DrawingId = AnotherDrawing });
        await context.SaveChangesAsync();
        return context;
    }
}
