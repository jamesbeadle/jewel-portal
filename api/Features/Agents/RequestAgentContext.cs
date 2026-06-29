namespace Jewel.JPMS.Api.Features.Agents;

// The single text picture of a request handed to an agent: the request header, its in-app /
// emailed conversation, and the originating intake email(s). Assembled by RequestContextAssembler
// so a real agent can drop straight in; the stub agents ignore it.
public sealed record RequestAgentContext(
    string RequestId,
    string Header,
    string Conversation,
    string IntakeEmails)
{
    // Flattened prompt-ready form combining every section, for handing to Claude.
    public string ToPromptText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("REQUEST:");
        sb.AppendLine(Header);
        sb.AppendLine();
        sb.AppendLine("CONVERSATION:");
        sb.AppendLine(string.IsNullOrWhiteSpace(Conversation) ? "(no messages)" : Conversation);
        sb.AppendLine();
        sb.AppendLine("ORIGINATING EMAILS:");
        sb.AppendLine(string.IsNullOrWhiteSpace(IntakeEmails) ? "(none)" : IntakeEmails);
        return sb.ToString();
    }
}

// The structured result of an agent analysing a request. A real agent fills StructuredJson with its
// discipline-specific object; a stub returns Status = Unavailable with an empty object.
public sealed record AgentAnalysisResult(
    Jewel.JPMS.Models.AgentProposalStatus Status,
    string Summary,
    string StructuredJson,
    string? Rationale);
