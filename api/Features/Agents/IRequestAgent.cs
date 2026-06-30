using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Agents;

// The abstraction every agent implements. Identity/metadata up top; three behaviours below:
//   RespondAsync       — answer a chat message from a human operator.
//   AnalyseAsync       — read the whole request and return a structured proposal.
//   EvaluateCompletion — the close-gate opinion: may the request be closed as far as this agent
//                        is concerned, given its current assignment state.
// StubAgent supplies a not-implemented implementation of all three; the three concrete agents
// subclass it so the full wiring is exercised while real behaviour is deferred.
public interface IRequestAgent
{
    string Key { get; }
    string DisplayName { get; }
    AgentDiscipline Discipline { get; }
    string Summary { get; }
    bool IsImplemented { get; }

    // The record type(s) this agent serves. Applicability is derived from this — a record of one of
    // these types has this agent available by default, with no assignment step. An agent that serves
    // no record type yet (its record type is not built) returns an empty set and is provisioned nowhere.
    IReadOnlyCollection<RecordType> AppliesTo { get; }

    Task<string> RespondAsync(RequestAgentContext context, string userMessage, CancellationToken cancellationToken);

    Task<AgentAnalysisResult> AnalyseAsync(RequestAgentContext context, CancellationToken cancellationToken);

    // status is the agent's current assignment status on the request; the gate asks whether that
    // amounts to "work complete". A real agent could inspect its own outputs; the stub never agrees.
    AgentCompletionState EvaluateCompletion(AgentAssignmentStatus status);

    // Catalogue projection for the "apply agent" option screen.
    AgentDescriptor Describe() => new(Key, DisplayName, Discipline, Summary, IsImplemented);
}
