using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Agents;

// The not-implemented base every agent currently derives from. It exercises the full pipeline —
// chat, analysis, and close-gate — while declining to do real work: it replies that it is not
// implemented, returns an Unavailable proposal, and never reports its work complete. Replacing a
// stub with a real agent is a matter of overriding RespondAsync / AnalyseAsync / EvaluateCompletion.
public abstract class StubAgent : IRequestAgent
{
    public abstract string Key { get; }
    public abstract string DisplayName { get; }
    public abstract AgentDiscipline Discipline { get; }
    public abstract string Summary { get; }

    public virtual bool IsImplemented => false;

    // The line a human sees in the agent chat and, verbatim, as the close-gate blocking reason.
    protected virtual string NotImplementedMessage =>
        $"{DisplayName} is not implemented yet, so its work cannot be completed. " +
        "While I'm applied this request can't be closed.";

    public virtual Task<string> RespondAsync(RequestAgentContext context, string userMessage, CancellationToken cancellationToken)
        => Task.FromResult(NotImplementedMessage);

    public virtual Task<AgentAnalysisResult> AnalyseAsync(RequestAgentContext context, CancellationToken cancellationToken)
        => Task.FromResult(new AgentAnalysisResult(
            Status: AgentProposalStatus.Unavailable,
            Summary: $"{DisplayName} cannot produce a proposal yet (not implemented).",
            StructuredJson: "{}",
            Rationale: NotImplementedMessage));

    public virtual AgentCompletionState EvaluateCompletion(AgentAssignmentStatus status)
        => new(Key, DisplayName, IsComplete: false, Message: NotImplementedMessage);
}
