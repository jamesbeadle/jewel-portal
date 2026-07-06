using System.Text;
using System.Text.Json;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Agents;

// Programme. The predefined agent for a project's Scheduling record (the per-project scheduling
// bucket — its record id IS the project id). Its analysis is deterministic, not LLM-driven: it
// compares the live programme against the latest baseline (ProgrammeMovementCalculator) and turns
// the slippage into a delay-event proposal — which tasks moved, whether completion moved, and which
// delay events still need a Notice of Delay under the JCT ICD 2024 cl. 2.19 duty to notify
// forthwith. A human reviews the proposal and raises the NOD/EOT from the Schedule tab; nothing is
// issued automatically (the CEO sign-off gate on notices stays with the humans).
//
// Agents are singletons but JpmsContext is scoped, so data access goes through an IServiceScopeFactory
// scope per call.
public sealed class SchedulingAgent : StubAgent
{
    public const string AgentKey = "scheduling";

    private readonly IServiceScopeFactory scopes;
    public SchedulingAgent(IServiceScopeFactory scopes) { this.scopes = scopes; }

    public override string Key => AgentKey;
    public override string DisplayName => "Scheduling Agent";
    public override AgentDiscipline Discipline => AgentDiscipline.Programme;
    public override IReadOnlyCollection<RecordType> AppliesTo => new[] { RecordType.Scheduling };
    public override string Summary =>
        "Watches the programme against its baseline, surfaces delay events, and flags where a Notice of Delay or Extension of Time is due.";

    public override bool IsImplemented => true;

    // The scheduling bucket never "closes" — programme oversight is continuous — so this agent
    // never blocks, mirroring the Requests Agent's non-blocking stance.
    public override AgentCompletionState EvaluateCompletion(AgentAssignmentStatus status) =>
        new(Key, DisplayName, IsComplete: true, Message: "Programme oversight is continuous; the Scheduling Agent does not block closing.");

    public override async Task<string> RespondAsync(RequestAgentContext context, string userMessage, CancellationToken cancellationToken)
    {
        var analysis = await AnalyseProgrammeAsync(context.RequestId, cancellationToken);
        return analysis.Narrative;
    }

    public override async Task<AgentAnalysisResult> AnalyseAsync(RequestAgentContext context, CancellationToken cancellationToken)
    {
        var analysis = await AnalyseProgrammeAsync(context.RequestId, cancellationToken);
        return new AgentAnalysisResult(
            Status: AgentProposalStatus.Pending,
            Summary: analysis.Summary,
            StructuredJson: analysis.StructuredJson,
            Rationale: analysis.Narrative);
    }

    private sealed record ProgrammeAnalysis(string Summary, string StructuredJson, string Narrative);

    // The record id of a Scheduling record is the project id.
    private async Task<ProgrammeAnalysis> AnalyseProgrammeAsync(string projectId, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JpmsContext>();

        var tasks = (await db.ProgrammeTasks.Where(t => t.ProjectId == projectId).ToListAsync(cancellationToken))
            .Select(t => new ProgrammeTask(t.ProgrammeTaskId, t.ProjectId, t.Title, t.PlannedStart, t.PlannedEnd, t.ProgressPercent, t.BoqLineItemId))
            .ToList();

        if (tasks.Count == 0)
            return new ProgrammeAnalysis(
                "No programme tasks exist yet.",
                "{}",
                "There is no programme to analyse yet — add tasks on the Schedule tab, then take a baseline so movement can be measured.");

        var baseline = await db.ProgrammeBaselines
            .Where(b => b.ProjectId == projectId)
            .OrderByDescending(b => b.TakenAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (baseline is null)
            return new ProgrammeAnalysis(
                "No baseline has been taken, so movement cannot be measured.",
                "{}",
                $"The programme has {tasks.Count} task(s) but no baseline. Take a baseline on the Schedule tab — without one, slippage can't be evidenced and delay notices can't be substantiated.");

        var baselineTasks = (await db.ProgrammeBaselineTasks
                .Where(t => t.ProgrammeBaselineId == baseline.ProgrammeBaselineId)
                .ToListAsync(cancellationToken))
            .Select(t => new ProgrammeBaselineTask(t.ProgrammeBaselineTaskId, t.ProgrammeBaselineId, t.ProgrammeTaskId, t.Title, t.PlannedStart, t.PlannedEnd))
            .ToList();

        var movement = ProgrammeMovementCalculator.Compare(tasks, baselineTasks);

        // A delay event is treated as notified when any Notice of Delay has been raised on the
        // project since the baseline was taken. Deliberately coarse: the agent flags candidates
        // for a notice, the humans decide coverage.
        var nodsSinceBaseline = await db.Requests
            .Where(r => r.ProjectId == projectId
                        && r.Kind == (int)RequestType.NoticeOfDelay
                        && r.RaisedAt >= baseline.TakenAt)
            .CountAsync(cancellationToken);

        var noticeDue = movement.CompletionSlipDays > 0 && nodsSinceBaseline == 0;

        var structured = JsonSerializer.Serialize(new
        {
            projectId,
            baselineId = baseline.ProgrammeBaselineId,
            baselineLabel = baseline.Label,
            baselineTakenAt = baseline.TakenAt,
            completionSlipDays = movement.CompletionSlipDays,
            baselineCompletion = movement.BaselineCompletion,
            currentCompletion = movement.CurrentCompletion,
            nodsRaisedSinceBaseline = nodsSinceBaseline,
            noticeOfDelayDue = noticeDue,
            delayEvents = movement.DelayEvents.Select(e => new
            {
                e.ProgrammeTaskId,
                e.Title,
                e.BaselineEnd,
                e.PlannedEnd,
                e.SlipDays,
                e.DrivesCompletion
            })
        });

        var narrative = BuildNarrative(baseline.Label, baseline.TakenAt, movement, nodsSinceBaseline, noticeDue);
        var summary = movement.HasSlippage
            ? $"{movement.DelayEvents.Count} task(s) have slipped against baseline '{baseline.Label}'; completion has moved {movement.CompletionSlipDays} day(s)."
              + (noticeDue ? " A Notice of Delay appears to be due." : "")
            : $"The programme is holding against baseline '{baseline.Label}' — no slippage.";

        return new ProgrammeAnalysis(summary, structured, narrative);
    }

    private static string BuildNarrative(string baselineLabel, DateTimeOffset baselineTakenAt, ProgrammeMovement movement, int nodsSinceBaseline, bool noticeDue)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Programme movement against baseline '{baselineLabel}' (taken {baselineTakenAt:d MMM yyyy}):");
        sb.AppendLine();

        if (!movement.HasSlippage)
        {
            sb.AppendLine("No task has slipped past its baselined end date. The programme is holding.");
            return sb.ToString().TrimEnd();
        }

        foreach (var e in movement.DelayEvents)
        {
            sb.AppendLine($"- {e.Title}: end moved {e.BaselineEnd:d MMM yyyy} → {e.PlannedEnd:d MMM yyyy} (+{e.SlipDays} day(s))"
                          + (e.DrivesCompletion ? " — drives project completion" : ""));
        }
        sb.AppendLine();

        if (movement.CompletionSlipDays > 0)
        {
            sb.AppendLine($"Project completion has moved {movement.CompletionSlipDays} day(s): {movement.BaselineCompletion:d MMM yyyy} → {movement.CurrentCompletion:d MMM yyyy}.");
            sb.AppendLine(noticeDue
                ? "No Notice of Delay has been raised since this baseline was taken. Under JCT ICD 2024 cl. 2.19 notice is due forthwith once delay is apparent — raise the NOD from the Schedule tab's Claims view."
                : $"{nodsSinceBaseline} Notice(s) of Delay have been raised since the baseline — check they cover the events above before considering an Extension of Time (cl. 2.19/2.20).");
        }
        else
        {
            sb.AppendLine("Individual tasks have slipped but project completion is unmoved — monitor, no notice appears due yet.");
        }

        return sb.ToString().TrimEnd();
    }
}
