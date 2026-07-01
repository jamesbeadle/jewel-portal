namespace Jewel.JPMS.Models;

// The kind of record an agent can attach to. A record's type — not a human — decides which
// agent(s) are applicable to it: agents declare the record type(s) they serve (IRequestAgent.AppliesTo)
// and the applicable set is derived, never assigned. Today every request-family record is a Request;
// Bid Package Invites are the first additional record type (built in a later phase).
public enum RecordType
{
    Request = 0,           // the RF* family (RFI/RFA/RFC/RFQ/RFP) plus NOD/EOT
    BidPackageInvite = 1,  // a bid package and the subcontractors invited to tender (Part B)
    CostCentre = 2         // a valuation-report cost centre on a project (project + cost-centre grouping)
}

// The discipline an agent belongs to. Mirrors the columns of the request-agent flow diagram:
// procurement (bid packages), programme (scheduling), commercial (valuations), plus the general
// requests desk that serves the RF* record family.
public enum AgentDiscipline
{
    Procurement = 0, // bid packages, purchase orders
    Programme = 1,   // scheduling, EoT / NoD notices
    Commercial = 2,  // valuations, variation-order quotes
    Requests = 3     // RFI/RFA/RFC desk for request records
}

// Where an applied agent sits on a request. An agent is Active while it still has work to
// do; it moves to WorkComplete once it (or a human on its behalf) agrees its work is done.
// A request cannot be closed while any applied agent is still Active.
public enum AgentAssignmentStatus
{
    Active = 0,      // watching the request, work outstanding
    WorkComplete = 1 // agent agrees its work is complete
}

// Author role for a line in a per-(request, agent) chat. User messages come from a human
// operator, Agent messages are the agent's replies, System messages are framing/notices.
public enum AgentChatRole
{
    User = 0,
    Agent = 1,
    System = 2
}

// Lifecycle of a structured proposal returned by an agent's analysis. Pending awaits a
// human decision; Accepted/Rejected are the human-in-the-loop outcomes; Superseded marks
// an older proposal replaced by a newer run; Unavailable is what a stub agent returns
// because it cannot actually produce a proposal yet.
public enum AgentProposalStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Superseded = 3,
    Unavailable = 4
}

// A catalogue entry for one of the agents the system offers. Static metadata used to
// render the "apply agent" option screen; IsImplemented is false for every stub agent.
public sealed record AgentDescriptor(
    string Key,
    string DisplayName,
    AgentDiscipline Discipline,
    string Summary,
    bool IsImplemented);

// An agent applied to a request — one row in the global watch queue, per (request, agent).
public sealed record RequestAgent(
    string RequestAgentId,
    string RequestId,
    string AgentKey,
    string DisplayName,
    AgentDiscipline Discipline,
    AgentAssignmentStatus Status,
    bool IsPrimary,
    string StatusMessage,
    string AssignedByEmail,
    DateTimeOffset AssignedAt,
    DateTimeOffset? CompletedAt = null)
{
    public bool IsComplete => Status == AgentAssignmentStatus.WorkComplete;
}

// A single message in a per-(request, agent) conversation.
public sealed record AgentChatMessage(
    string MessageId,
    string RequestId,
    string AgentKey,
    AgentChatRole Role,
    string AuthorEmail,
    string AuthorName,
    string Body,
    DateTimeOffset PostedAt);

// A persisted structured proposal produced by an agent's analysis of a request. StructuredJson
// is the raw discipline-specific object the agent returned (empty for a stub's Unavailable
// proposal); a human accepts or rejects it before anything takes effect.
public sealed record AgentProposal(
    string ProposalId,
    string RequestId,
    string AgentKey,
    string DisplayName,
    AgentProposalStatus Status,
    string Summary,
    string StructuredJson,
    string? Rationale,
    DateTimeOffset CreatedAt,
    string? DecidedByEmail = null,
    DateTimeOffset? DecidedAt = null)
{
    public bool IsPending => Status == AgentProposalStatus.Pending;
}

// One row of the global queue page: a watched (request, agent) pair flattened with enough
// request context to render the queue without a second lookup.
public sealed record AgentQueueItem(
    string RequestAgentId,
    string RequestId,
    string ProjectId,
    int RequestNumber,
    string RequestTitle,
    RequestStatus RequestStatus,
    string AgentKey,
    string DisplayName,
    AgentDiscipline Discipline,
    AgentAssignmentStatus Status,
    bool IsPrimary,
    string StatusMessage,
    DateTimeOffset AssignedAt)
{
    public string RequestDisplayNumber => RequestNumber > 0 ? $"REQ-{RequestNumber:0000}" : "";
}

public static class AgentDisciplineExtensions
{
    public static string DisplayName(this AgentDiscipline discipline) => discipline switch
    {
        AgentDiscipline.Procurement => "Procurement",
        AgentDiscipline.Programme   => "Programme",
        AgentDiscipline.Commercial  => "Commercial",
        AgentDiscipline.Requests    => "Requests",
        _ => discipline.ToString()
    };
}

// An agent's completion opinion for the close gate. IsComplete drives whether the request
// may close; Message is the agent's explanation (e.g. the stub's "not implemented" reason).
public sealed record AgentCompletionState(
    string AgentKey,
    string DisplayName,
    bool IsComplete,
    string Message);

// Result of attempting to close a request through the agent gate. When Closed is false the
// request stays open and BlockingAgents lists every applied agent that is not yet complete.
public sealed record RequestCloseOutcome(
    bool Closed,
    IReadOnlyList<AgentCompletionState> BlockingAgents);
