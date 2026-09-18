using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Api.Features.Commercial.Queries;
using Jewel.JPMS.Api.Features.RecordLinks.Providers;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The 2026-09-18 consolidation: a valuation report and its tagged snapshot were two objects for
// one thing. Now the CLAIM is the valuation — locking freezes its own statement lines, the
// invoice, the PDF, the email and the connector all read the claim — and the retired snapshot
// object survives only as an alias register so old ids and JPMS/VRS-… tags still resolve. By
// France as it stood on 2026-09-15 is the fixture: three periods (Aug Draft, Jul Preapproved,
// Jun Confirmed) and three retired snapshots (VI-0004 raise on July; VI-0003 raise superseded
// and VI-0003 submission on June).
public sealed class ValuationClaimStatementTests
{
    private const string Project = "P-BF";
    private const string Aug = "claim-3", Jul = "claim-2", Jun = "claim-1";
    private const string Slab = "line-slab", Roof = "line-roof";

    [Fact]
    public async Task ClaimProvider_listsOneRowPerPeriod_newestFirst_withTheClaimTag()
    {
        await using var context = await ByFranceAsync();
        var claims = new ValuationClaimLinkProvider(context);

        var picker = await claims.ForProjectAsync(Project, CancellationToken.None);

        Assert.Equal(new[] { Aug, Jul, Jun }, picker.Select(r => r.RecordId));
        Assert.All(picker, r => Assert.Equal(RecordType.ValuationClaim, r.Type));
        Assert.Equal("VAL-JBB-2026-001-3", picker[0].TagReference);
        Assert.Equal("Valuation 19 - July 2026", picker[1].Title);
        Assert.False(picker[2].IsActive); // a confirmed period is still filable — a late reply belongs to June
    }

    [Fact]
    public async Task RetiredSnapshotIds_andVrsTags_resolveToTheirClaim_andTheClaimReadsTheirMail()
    {
        await using var context = await ByFranceAsync();
        var claims = new ValuationClaimLinkProvider(context);
        var retired = new RetiredValuationStatementLinkProvider(context);

        // Nothing is listed under the retired type — the picker's row per period is the claim.
        Assert.Empty(await retired.ForProjectAsync(Project, CancellationToken.None));

        // An old snapshot id (a saved link, an old connector call) lands on its claim — as the claim.
        var viaOldId = (await retired.FindAsync("snap-4", CancellationToken.None))!;
        Assert.Equal(RecordType.ValuationClaim, viaOldId.Type);
        Assert.Equal(Jul, viaOldId.RecordId);
        Assert.Equal("VAL-JBB-2026-001-2", viaOldId.TagReference);

        // An old VRS stem on mail resolves back to the claim it was frozen from, from either provider.
        Assert.Equal(Jun, (await claims.FindByTagAsync("VRS-JBB-2026-001-3", CancellationToken.None))!.RecordId);
        Assert.Equal(Jun, (await retired.FindByTagAsync("VRS-JBB-2026-001-2", CancellationToken.None))!.RecordId);
        Assert.Equal(Aug, (await claims.FindByTagAsync("VAL-JBB-2026-001-3", CancellationToken.None))!.RecordId);
        Assert.Null(await claims.FindByTagAsync("VRS-JBB-2026-001-99", CancellationToken.None));

        // The claim reads every retired stem frozen from it, superseded ones included; a claim
        // locked since the consolidation has none.
        var june = (await claims.FindAsync(Jun, CancellationToken.None))!;
        Assert.Equal(new[] { "VRS-JBB-2026-001-3", "VRS-JBB-2026-001-2" },
            (await claims.CompanionTagReferencesAsync(june, CancellationToken.None)).OrderByDescending(s => s));
        var august = (await claims.FindAsync(Aug, CancellationToken.None))!;
        Assert.Empty(await claims.CompanionTagReferencesAsync(august, CancellationToken.None));
    }

    [Fact]
    public async Task Locking_freezesEveryBillLineOntoTheClaim_andTheStatementReadsThoseRows()
    {
        await using var context = await ByFranceAsync();
        var august = await context.ValuationClaims.SingleAsync(c => c.ValuationClaimId == Aug);

        // A Draft's statement is the working copy: every bill line, missing entries at 0%.
        var workingCopy = await ValuationStatementLines.ReadAsync(context, august, CancellationToken.None);
        Assert.True(workingCopy.IsDraft);
        Assert.Equal(new[] { Slab, Roof }, workingCopy.Lines.Select(l => l.ValuationLineItemId));
        Assert.Equal(0m, workingCopy.Lines.Single(l => l.ValuationLineItemId == Roof).PercentComplete);
        Assert.Equal(6_000m, workingCopy.Claim.TotalWorksComplete);

        await ValuationStatementLines.FreezeAsync(context, august, CancellationToken.None);
        august.Status = (int)ValuationClaimStatus.Preapproved;
        await context.SaveChangesAsync();

        // The lock wrote the bill onto the claim's own rows — the 0% roof line included — in
        // statement order, and stamped when.
        var rows = await context.ClaimLines.Where(r => r.ValuationClaimId == Aug).OrderBy(r => r.DisplayOrder).ToListAsync();
        Assert.Equal(new[] { Slab, Roof }, rows.Select(r => r.ValuationLineItemId));
        Assert.Equal("Ground slab", rows[0].Description);
        Assert.Equal(10_000m, rows[0].LineAmount);
        Assert.Equal("2.1", rows[0].ClientReference);
        Assert.NotNull(august.LockedAt);

        // Re-pricing and deleting the live bill afterwards changes nothing the client was sent.
        var slab = await context.ValuationLineItems.SingleAsync(l => l.ValuationLineItemId == Slab);
        slab.Description = "Ground slab (re-described)";
        slab.Rate = 99_999m; slab.LineAmount = 99_999m;
        context.ValuationLineItems.Remove(await context.ValuationLineItems.SingleAsync(l => l.ValuationLineItemId == Roof));
        await context.SaveChangesAsync();

        var statement = await ValuationStatementLines.ReadAsync(context, august, CancellationToken.None);
        Assert.False(statement.IsDraft);
        Assert.Equal(new[] { Slab, Roof }, statement.Lines.Select(l => l.ValuationLineItemId));
        Assert.Equal("Ground slab", statement.Lines[0].Description);
        Assert.Equal(10_000m, statement.Lines[0].LineAmount);
        Assert.Equal(6_000m, statement.Lines[0].CumulativeClaimed);
    }

    [Fact]
    public async Task TheStatementQuery_acceptsAClaimId_orARetiredSnapshotId()
    {
        await using var context = await ByFranceAsync();
        var handler = new GetValuationStatementHandler(context);

        var byClaim = await handler.HandleAsync(new GetValuationStatement(Jul), CancellationToken.None);
        var byOldId = await handler.HandleAsync(new GetValuationStatement("snap-4"), CancellationToken.None);

        Assert.Equal(Jul, byClaim.Claim.ValuationClaimId);
        Assert.Equal(Jul, byOldId.Claim.ValuationClaimId);
        Assert.False(byClaim.IsDraft);
        Assert.Equal("Valuation 19 - July 2026", byClaim.Label);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new GetValuationStatement("no-such-thing"), CancellationToken.None));
    }

    [Fact]
    public void Stage_isReadOffTheClaimAndItsInvoice_neverStored()
    {
        var claim = new ValuationClaim(Jul, Project, 2, DateTimeOffset.UtcNow, ValuationClaimStatus.Preapproved,
            5m, 0m, DateTimeOffset.UtcNow, null, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, Name: "July 2026");
        ValuationInvoice Invoice(ValuationInvoiceStatus status, string? claimId = Jul, int number = 4) =>
            new("vi", Project, claimId, number, $"VI-{number:0000}", DateTimeOffset.UtcNow, 100m, 0m, status, DateTimeOffset.UtcNow);

        Assert.Equal(ValuationStage.Draft, ValuationStages.Of(claim with { Status = ValuationClaimStatus.Draft }, null));
        Assert.Equal(ValuationStage.Locked, ValuationStages.Of(claim, null));
        Assert.Equal(ValuationStage.Locked, ValuationStages.Of(claim, Invoice(ValuationInvoiceStatus.Cancelled)));
        Assert.Equal(ValuationStage.Invoiced, ValuationStages.Of(claim, Invoice(ValuationInvoiceStatus.Raised)));
        Assert.Equal(ValuationStage.Sent, ValuationStages.Of(claim, Invoice(ValuationInvoiceStatus.Submitted)));
        Assert.Equal(ValuationStage.Certified, ValuationStages.Of(claim, Invoice(ValuationInvoiceStatus.Issued)));
        Assert.Equal(ValuationStage.Paid, ValuationStages.Of(claim, Invoice(ValuationInvoiceStatus.Paid)));
        Assert.Equal(ValuationStage.Confirmed, ValuationStages.Of(claim with { Status = ValuationClaimStatus.Confirmed }, Invoice(ValuationInvoiceStatus.Paid)));

        // The claim's live invoice: the newest non-cancelled one drawn against it.
        var live = ValuationStages.InvoiceFor(claim, new[]
        {
            Invoice(ValuationInvoiceStatus.Cancelled, number: 5),
            Invoice(ValuationInvoiceStatus.Raised, number: 6),
            Invoice(ValuationInvoiceStatus.Issued, claimId: "other", number: 7)
        });
        Assert.Equal(6, live!.Number);
    }

    private static async Task<JpmsContext> ByFranceAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"valuation-claim-statement-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2026-001", Name = "By France", ClientName = "Client" });
        context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = Aug, ProjectId = Project, ClaimNumber = 3, Name = "Valuation 20 - August 2026", Status = (int)ValuationClaimStatus.Draft, RetentionPercent = 5m });
        context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = Jul, ProjectId = Project, ClaimNumber = 2, Name = "Valuation 19 - July 2026", Status = (int)ValuationClaimStatus.Preapproved, LockedAt = new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.Zero) });
        context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = Jun, ProjectId = Project, ClaimNumber = 1, Name = "June 2026", Status = (int)ValuationClaimStatus.Confirmed });
        var t0 = new DateTimeOffset(2026, 7, 24, 8, 33, 0, TimeSpan.Zero);
        context.ValuationClaimLegacyStatements.Add(new ValuationClaimLegacyStatementEntity { ValuationReportSnapshotId = "snap-3a", ProjectId = Project, ValuationClaimId = Jun, Number = 2, Label = "VI-0003 raise", TakenAt = t0, IsSuperseded = true });
        context.ValuationClaimLegacyStatements.Add(new ValuationClaimLegacyStatementEntity { ValuationReportSnapshotId = "snap-3b", ProjectId = Project, ValuationClaimId = Jun, Number = 3, Label = "VI-0003 submission", TakenAt = t0.AddMinutes(1) });
        context.ValuationClaimLegacyStatements.Add(new ValuationClaimLegacyStatementEntity { ValuationReportSnapshotId = "snap-4", ProjectId = Project, ValuationClaimId = Jul, Number = 4, Label = "VI-0004 raise", TakenAt = new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.Zero) });

        // The live bill: two contract lines; August has an entry on the slab only.
        context.ValuationLineItems.Add(new ValuationLineItemEntity { ValuationLineItemId = Slab, ProjectId = Project, ElementType = (int)ValuationElementType.ContractWorks, SectionCode = "A", SectionName = "Substructure", CostCode = "GW", Description = "Ground slab", Unit = "item", Quantity = 1m, Rate = 10_000m, LineAmount = 10_000m, DisplayOrder = 1, ClientReference = "2.1" });
        context.ValuationLineItems.Add(new ValuationLineItemEntity { ValuationLineItemId = Roof, ProjectId = Project, ElementType = (int)ValuationElementType.ContractWorks, SectionCode = "B", SectionName = "Roof", CostCode = "RF", Description = "Roof covering", Unit = "item", Quantity = 1m, Rate = 4_000m, LineAmount = 4_000m, DisplayOrder = 2 });
        context.ClaimLines.Add(new ClaimLineEntity { ClaimLineId = "cl-aug-slab", ValuationClaimId = Aug, ValuationLineItemId = Slab, PercentComplete = 60m, CumulativeClaimed = 6_000m, PeriodIncrement = 6_000m });
        // July's frozen statement rows (what the migration would have written from its snapshot).
        context.ClaimLines.Add(new ClaimLineEntity { ClaimLineId = "cl-jul-slab", ValuationClaimId = Jul, ValuationLineItemId = Slab, PercentComplete = 30m, CumulativeClaimed = 3_000m, PeriodIncrement = 3_000m, ElementType = (int)ValuationElementType.ContractWorks, SectionCode = "A", SectionName = "Substructure", CostCode = "GW", Description = "Ground slab", Unit = "item", Quantity = 1m, Rate = 10_000m, LineAmount = 10_000m, DisplayOrder = 0 });
        await context.SaveChangesAsync();
        return context;
    }
}
