using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

// The portal rewrites the day and flags what the report must not say (Jeremy, 21 Sep 2026): the
// raw notes stay on the record, Section 1 prints the rewrite, every flag opens uncleared and the
// build is refused until the office has cleared them — the flag list is a gate, not a footnote.
public sealed class ContractorsReportRewriteTests
{
    private const string Project = "by-france";
    private const string ReportId = "report-33";
    private static readonly ReportingWeek Week = ReportingWeek.EndingOn(new DateOnly(2026, 10, 8));
    private static readonly DateOnly Monday = new(2026, 10, 5);
    private static readonly DateOnly Tuesday = new(2026, 10, 6);

    private const string ClaudeAnswer = """
        ```json
        {"days":[
          {"date":"2026-10-05","summary":"Second fix continued on the first floor.","bullets":["Second fix carpentry to the first-floor bedrooms.","One radiator fitted in bedroom 2."]},
          {"date":"2026-10-06","summary":"Voids foamed.","bullets":["Service voids to the ground-floor ceiling foamed."]},
          {"date":"2026-10-09","summary":"Outside the week.","bullets":["Ignored."]}
        ],
        "lookAhead":["Remaining radiators to the first floor."],
        "flags":[
          {"kind":"scope","date":"2026-10-05","text":"\"rad fitted\" read as one radiator, not the floor's radiators."},
          {"kind":"compliance","date":"2026-10-06","text":"\"foamed\" kept as written — not stated as fire stopping."},
          {"kind":"nonsense","date":"2026-10-09","text":"An unknown kind reads as a change."}
        ]}
        ```
        """;

    [Fact]
    public async Task TheRewrite_isStoredWithEveryFlagOpen_andTheRawNotesUntouched()
    {
        await using var context = await SeededAsync();
        var claude = new ClaudeThatAnswers(ClaudeAnswer);

        var report = await Handler(context, claude).HandleAsync(new RewriteContractorsReportWeek(ReportId), CancellationToken.None);

        var rewrite = report.Rewrite!;
        Assert.Equal(2, rewrite.Days.Count);
        Assert.Equal(new[] { Monday, Tuesday }, rewrite.Days.Select(day => day.Date));
        Assert.Equal(3, rewrite.OpenFlagCount);
        Assert.Equal(ContractorsReportFlagKind.Changed, rewrite.Flags[2].Kind);
        Assert.Null(rewrite.Flags[2].Day);
        Assert.Equal(new[] { "Remaining radiators to the first floor." }, rewrite.LookAheadCandidates);
        Assert.Contains("rad fitted", claude.LastUserPrompt);
        Assert.Contains("foamed", claude.LastSystemPrompt);
        var raw = await context.ProgressUpdates.SingleAsync(row => row.ProgressUpdateId == "u-mon-jack");
        Assert.Contains("rad fitted", raw.Description);
    }

    [Fact]
    public async Task SectionOne_printsTheRewrite_keepsTheDaysPhotographs_andIsRefusedWhileAFlagIsOpen()
    {
        await using var context = await SeededAsync();
        await Handler(context, new ClaudeThatAnswers(ClaudeAnswer)).HandleAsync(new RewriteContractorsReportWeek(ReportId), CancellationToken.None);

        var view = await new ContractorsReportComposer(context).ViewAsync(ReportId, CancellationToken.None);

        var monday = view!.Document.Progress.Single(day => day.Date == Monday);
        var entry = Assert.Single(monday.Entries);
        Assert.Equal("Second fix carpentry to the first-floor bedrooms.\nOne radiator fitted in bedroom 2.", entry.Description);
        Assert.Equal(2, monday.Photos.Count);
        Assert.False(view.Document.CanBeBuilt);
        var finding = Assert.Single(view.Document.Findings);
        Assert.Equal(ContractorsReportSections.Progress, finding.Section);
        Assert.Contains("3 flags", finding.Line);
    }

    [Fact]
    public async Task ClearingEveryFlag_letsTheReportBuild_andDiscarding_printsTheRawNotesAgain()
    {
        await using var context = await SeededAsync();
        var report = await Handler(context, new ClaudeThatAnswers(ClaudeAnswer)).HandleAsync(new RewriteContractorsReportWeek(ReportId), CancellationToken.None);
        var cleared = report.Rewrite! with { Flags = report.Rewrite.Flags.Select(flag => flag with { IsCleared = true }).ToList() };
        var update = new UpdateContractorsReport(ReportId, "", "", "", "", Week.End.AddDays(1), Array.Empty<ContractorsReportLookAheadItem>(),
            "", "", "", Array.Empty<ContractorsReportAttendance>(), report.SelectedUpdateIds, Rewrite: cleared);
        await new UpdateContractorsReportHandler(context).HandleAsync(update, CancellationToken.None);

        var built = await new ContractorsReportComposer(context).ViewAsync(ReportId, CancellationToken.None);
        Assert.True(built!.Document.CanBeBuilt);

        await new DiscardContractorsReportRewriteHandler(context).HandleAsync(new DiscardContractorsReportRewrite(ReportId), CancellationToken.None);
        var raw = await new ContractorsReportComposer(context).ViewAsync(ReportId, CancellationToken.None);
        Assert.Null(raw!.Report.Rewrite);
        Assert.Equal(2, raw.Document.Progress.Single(day => day.Date == Monday).Entries.Count);
    }

    [Fact]
    public async Task NoUsableAnswer_isARefusal_notASilentReport()
    {
        await using var context = await SeededAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Handler(context, new ClaudeThatAnswers("Sorry, I cannot help with that.")).HandleAsync(new RewriteContractorsReportWeek(ReportId), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Handler(context, new NullClaudeClient()).HandleAsync(new RewriteContractorsReportWeek(ReportId), CancellationToken.None));

        var entity = await context.ContractorsReports.SingleAsync();
        Assert.Null(entity.RewriteJson);
    }

    private static RewriteContractorsReportWeekHandler Handler(JpmsContext context, IClaudeClient claude) =>
        new(context, new ContractorsReportComposer(context), claude, new AnthropicOptions(), NullLogger<RewriteContractorsReportWeekHandler>.Instance);

    private sealed class ClaudeThatAnswers : IClaudeClient
    {
        private readonly string answer;
        public ClaudeThatAnswers(string answer) { this.answer = answer; }
        public string LastSystemPrompt { get; private set; } = "";
        public string LastUserPrompt { get; private set; } = "";
        public bool IsConfigured => true;

        public Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct, string? modelOverride = null, int? maxTokensOverride = null)
        {
            LastSystemPrompt = systemPrompt;
            LastUserPrompt = userPrompt;
            return Task.FromResult<string?>(answer);
        }

        public Task<ClaudeChunk?> CompleteChunkAsync(string systemPrompt, string userPrompt, string assistantPrefill, string model, int maxTokens, CancellationToken ct) =>
            Task.FromResult<ClaudeChunk?>(null);
    }

    private static async Task<JpmsContext> SeededAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"contractors-report-rewrite-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2026-001", Name = "By France" });
        context.ProgressUpdates.AddRange(
            Note("u-mon-jack", "Daily log — Jack Eastly", "2nd fix carp to 1st floor beds, rad fitted bed 2, still more to be done", Monday),
            Note("u-mon-dan", "Daily log — Dan Prowse", "Painting prep 1st floor", Monday),
            Note("u-tue-jack", "Daily log — Jack Eastly", "Foamed the voids gf ceiling", Tuesday));
        context.ProgressPhotos.AddRange(Photo("ph-1", "u-mon-jack"), Photo("ph-2", "u-mon-dan"));
        context.ContractorsReports.Add(new ContractorsReportEntity
        {
            ContractorsReportId = ReportId, ProjectId = Project, Number = 33,
            PeriodStart = Week.Start, PeriodEnd = Week.End, DateOfIssue = Week.End.AddDays(1),
            SelectedUpdateIdsJson = ContractorsReportJson.Write(new[] { "u-mon-jack", "u-mon-dan", "u-tue-jack" })
        });
        await context.SaveChangesAsync();
        return context;
    }

    private static ProgressUpdateEntity Note(string id, string title, string words, DateOnly day) => new()
    {
        ProgressUpdateId = id, ProjectId = Project, Title = title, Description = words, CreatedByEmail = "site@example.com",
        WorkDate = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), CreatedAt = DateTimeOffset.UtcNow
    };

    private static ProgressPhotoEntity Photo(string id, string updateId) => new()
    {
        ProgressPhotoId = id, ProgressUpdateId = updateId, ProjectId = Project, FileName = $"{id}.jpg", ContentType = "image/jpeg"
    };
}
