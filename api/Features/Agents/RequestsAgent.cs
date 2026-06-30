using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Agents;

// Requests desk. The predefined agent for every Request record (the RF* family — RFI/RFA/RFC/…).
// Ships as a stub for chat/analysis, but — unlike the other stubs — it does NOT gate closing: a
// request must remain closable by default, exactly as it was before agents became type-derived.
// When real behaviour arrives, override RespondAsync / AnalyseAsync (and tighten EvaluateCompletion).
public sealed class RequestsAgent : StubAgent
{
    public const string AgentKey = "requests";

    public override string Key => AgentKey;
    public override string DisplayName => "Requests Agent";
    public override AgentDiscipline Discipline => AgentDiscipline.Requests;
    public override IReadOnlyCollection<RecordType> AppliesTo => new[] { RecordType.Request };
    public override string Summary =>
        "Watches request records (RFI/RFA/RFC) and assists with drafting and triage.";

    // Non-blocking: this agent never holds a request open, preserving the pre-change close behaviour.
    public override AgentCompletionState EvaluateCompletion(AgentAssignmentStatus status) =>
        new(Key, DisplayName, IsComplete: true, Message: "Requests Agent does not block closing.");

    public override Task<string> RespondAsync(RequestAgentContext context, string userMessage, CancellationToken cancellationToken) =>
        Task.FromResult($"{DisplayName} is not implemented yet, so it can't answer questions about this request.");
}
