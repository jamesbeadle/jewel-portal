using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress.ContractorsReports;

/// <summary>The rewrite as the editor holds it between saves: each day's bullets as one text the
/// office edits, one line a bullet, and each flag with whether it has been cleared. The
/// candidates and the request time are carried as they came.</summary>
public sealed class ContractorsReportRewriteDraft
{
    public DateTimeOffset RequestedAt { get; init; }
    public List<ContractorsReportRewrittenDayDraft> Days { get; } = new();
    public List<string> LookAheadCandidates { get; } = new();
    public List<ContractorsReportFlagDraft> Flags { get; } = new();

    public int OpenFlagCount => Flags.Count(flag => !flag.IsCleared);

    public static ContractorsReportRewriteDraft From(ContractorsReportRewrite rewrite)
    {
        var draft = new ContractorsReportRewriteDraft { RequestedAt = rewrite.RequestedAt };
        draft.Days.AddRange(rewrite.Days.Select(day => new ContractorsReportRewrittenDayDraft
        {
            Date = day.Date, Summary = day.Summary, BulletsText = string.Join("\n", day.Bullets)
        }));
        draft.LookAheadCandidates.AddRange(rewrite.LookAheadCandidates);
        draft.Flags.AddRange(rewrite.Flags.Select(flag => new ContractorsReportFlagDraft
        {
            Kind = flag.Kind, Day = flag.Day, Text = flag.Text, IsCleared = flag.IsCleared
        }));
        return draft;
    }

    public ContractorsReportRewrite ToRewrite() => new(
        RequestedAt,
        Days.Select(day => new ContractorsReportRewrittenDay(day.Date, day.Summary, ContractorsReportPrintedText.Bullets(day.BulletsText))).ToList(),
        LookAheadCandidates.ToList(),
        Flags.Select(flag => new ContractorsReportFlag(flag.Kind, flag.Day, flag.Text, flag.IsCleared)).ToList());
}

public sealed class ContractorsReportRewrittenDayDraft
{
    public DateOnly Date { get; init; }
    public string Summary { get; init; } = "";
    public string BulletsText { get; set; } = "";
}

public sealed class ContractorsReportFlagDraft
{
    public ContractorsReportFlagKind Kind { get; init; }
    public DateOnly? Day { get; init; }
    public string Text { get; init; } = "";
    public bool IsCleared { get; set; }
}
