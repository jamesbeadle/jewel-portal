using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.RecordLinks.Providers;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The 2026-09-15 consolidation (Nigel): a triager files a valuation email to ONE row per period —
// the period's live frozen statement when one exists, the claim otherwise — and whichever row
// they pick, both read the same mail. By France as it stood that morning is the fixture: three
// periods (Aug Draft, Jul Preapproved, Jun Confirmed), three snapshots (VI-0004 raise live on
// July, VI-0003 submission live on June, VI-0003 raise superseded on June).
public sealed class ValuationReportLinkTargetsTests
{
    private const string Project = "P-BF";
    private const string Aug = "claim-3", Jul = "claim-2", Jun = "claim-1";

    [Fact]
    public void Merge_showsAPeriodAsItsLiveStatement_orAsTheClaimWhenNoneIsFrozen()
    {
        var claims = new[] { Claim(Aug, 3, "Valuation 20 - August 2026"), Claim(Jul, 2, "Valuation 19 - July 2026", active: true), Claim(Jun, 1, "June 2026", active: false) };
        var snapshots = new[]
        {
            Snapshot("snap-4", 4, "VI-0004 raise", live: true),
            Snapshot("snap-3b", 3, "VI-0003 submission", live: true),
            Snapshot("snap-3a", 2, "VI-0003 raise", live: false)
        };
        var frozenFrom = new Dictionary<string, string?> { ["snap-4"] = Jul, ["snap-3b"] = Jun, ["snap-3a"] = Jun };

        var rows = ValuationReportLinkTargets.Merge(claims, snapshots, frozenFrom);

        Assert.Equal(new[] { Aug, "snap-4", "snap-3b" }, rows.Select(r => r.RecordId));
        Assert.Equal(RecordType.ValuationClaim, rows[0].Type);          // August: nothing frozen yet — the live period
        Assert.Equal(RecordType.ValuationReportSnapshot, rows[1].Type); // July: its statement, not the claim
        Assert.DoesNotContain(rows, r => r.RecordId == "snap-3a");      // superseded — never a filing target
        Assert.DoesNotContain(rows, r => r.RecordId == Jul || r.RecordId == Jun);
    }

    [Fact]
    public void Merge_keepsAConfirmedPeriodWithNoStatement_atTheBottom_andEveryLiveStatementOfAPeriod()
    {
        var claims = new[] { Claim(Aug, 3, "August 2026"), Claim(Jun, 1, "June 2026", active: false) };
        var snapshots = new[] { Snapshot("snap-b", 2, "VI-0003 submission", live: true), Snapshot("snap-a", 1, "VI-0003 raise", live: true) };
        var frozenFrom = new Dictionary<string, string?> { ["snap-b"] = Aug, ["snap-a"] = Aug };

        var rows = ValuationReportLinkTargets.Merge(claims, snapshots, frozenFrom);

        Assert.Equal(new[] { "snap-b", "snap-a", Jun }, rows.Select(r => r.RecordId));
        Assert.False(rows[2].IsActive); // a late reply about June still has somewhere to go
    }

    [Fact]
    public void Merge_neverDropsALiveStatementWhoseClaimIsMissing()
    {
        var claims = new[] { Claim(Aug, 3, "August 2026") };
        var snapshots = new[] { Snapshot("snap-orphan", 1, "Period-end capture", live: true), Snapshot("snap-gone", 2, "VI-0001 raise", live: true) };
        var frozenFrom = new Dictionary<string, string?> { ["snap-orphan"] = null, ["snap-gone"] = "claim-deleted" };

        var rows = ValuationReportLinkTargets.Merge(claims, snapshots, frozenFrom);

        Assert.Equal(new[] { Aug, "snap-gone", "snap-orphan" }, rows.Select(r => r.RecordId));
    }

    [Fact]
    public async Task ClaimProvider_listsTheMergedPicker_andEachSideNamesTheOtherAsCompanion()
    {
        await using var context = await ByFranceAsync();
        var claims = new ValuationClaimLinkProvider(context);
        var snapshots = new ValuationReportSnapshotLinkProvider(context);

        var picker = await claims.ForProjectAsync(Project, CancellationToken.None);
        Assert.Equal(new[] { Aug, "snap-4", "snap-3b" }, picker.Select(r => r.RecordId));
        Assert.Equal("VAL-JBB-2026-001-3", picker[0].TagReference);
        Assert.Equal("VRS-JBB-2026-001-4", picker[1].TagReference);
        Assert.Equal("Valuation 19 - July 2026 — VI-0004 raise", picker[1].Title);

        // The snapshot register itself is untouched: every snapshot, superseded flagged inactive.
        var register = await snapshots.ForProjectAsync(Project, CancellationToken.None);
        Assert.Equal(3, register.Count);
        Assert.Single(register, r => !r.IsActive);

        // Companions: the claim reads all its statements (superseded included), the statement its claim.
        var july = (await claims.FindAsync(Jul, CancellationToken.None))!;
        Assert.Equal(new[] { "VRS-JBB-2026-001-4" }, await claims.CompanionTagReferencesAsync(july, CancellationToken.None));
        var june = (await claims.FindAsync(Jun, CancellationToken.None))!;
        Assert.Equal(new[] { "VRS-JBB-2026-001-3", "VRS-JBB-2026-001-2" }, (await claims.CompanionTagReferencesAsync(june, CancellationToken.None)).OrderByDescending(s => s));
        var statement = (await snapshots.FindAsync("snap-4", CancellationToken.None))!;
        Assert.Equal(new[] { "VAL-JBB-2026-001-2" }, await snapshots.CompanionTagReferencesAsync(statement, CancellationToken.None));
        var august = (await claims.FindAsync(Aug, CancellationToken.None))!;
        Assert.Empty(await claims.CompanionTagReferencesAsync(august, CancellationToken.None));
    }

    private static LinkableRecord Claim(string id, int number, string name, bool active = true) =>
        new(RecordType.ValuationClaim, id, Project, $"VAL-JBB-2026-001-{number}", $"VAL-JBB-2026-001-{number}", name, IsActive: active);

    private static LinkableRecord Snapshot(string id, int number, string label, bool live) =>
        new(RecordType.ValuationReportSnapshot, id, Project, $"VRS-JBB-2026-001-{number}", $"VRS-JBB-2026-001-{number}", label,
            StatusLabel: live ? null : "Superseded", IsActive: live);

    private static async Task<JpmsContext> ByFranceAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"valuation-report-link-targets-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2026-001", Name = "By France", ClientName = "Client" });
        context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = Aug, ProjectId = Project, ClaimNumber = 3, Name = "Valuation 20 - August 2026", Status = (int)ValuationClaimStatus.Draft });
        context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = Jul, ProjectId = Project, ClaimNumber = 2, Name = "Valuation 19 - July 2026", Status = (int)ValuationClaimStatus.Preapproved });
        context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = Jun, ProjectId = Project, ClaimNumber = 1, Name = "June 2026", Status = (int)ValuationClaimStatus.Confirmed });
        var t0 = new DateTimeOffset(2026, 7, 24, 8, 33, 0, TimeSpan.Zero);
        context.ValuationReportSnapshots.Add(new ValuationReportSnapshotEntity { ValuationReportSnapshotId = "snap-3a", ProjectId = Project, ValuationClaimId = Jun, Number = 2, Label = "VI-0003 raise", TakenAt = t0, IsSuperseded = true });
        context.ValuationReportSnapshots.Add(new ValuationReportSnapshotEntity { ValuationReportSnapshotId = "snap-3b", ProjectId = Project, ValuationClaimId = Jun, Number = 3, Label = "VI-0003 submission", TakenAt = t0.AddMinutes(1) });
        context.ValuationReportSnapshots.Add(new ValuationReportSnapshotEntity { ValuationReportSnapshotId = "snap-4", ProjectId = Project, ValuationClaimId = Jul, Number = 4, Label = "VI-0004 raise", TakenAt = new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.Zero) });
        await context.SaveChangesAsync();
        return context;
    }
}
