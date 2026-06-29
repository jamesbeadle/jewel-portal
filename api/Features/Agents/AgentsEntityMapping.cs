using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Agents;

// Entity -> contract model projections. RequestAgent / queue rows carry the agent's display name and
// discipline, which live on the agent (resolved via the registry) rather than the watch row, so those
// mappings take the resolved IRequestAgent. Unknown keys fall back to the stored key and Commercial.
internal static class AgentsEntityMapping
{
    public static RequestAgent ToModel(this RequestAgentEntity entity, IRequestAgent? agent) => new(
        RequestAgentId: entity.RequestAgentId,
        RequestId: entity.RequestId,
        AgentKey: entity.AgentKey,
        DisplayName: agent?.DisplayName ?? entity.AgentKey,
        Discipline: agent?.Discipline ?? AgentDiscipline.Commercial,
        Status: (AgentAssignmentStatus)entity.Status,
        IsPrimary: entity.IsPrimary,
        StatusMessage: entity.StatusMessage,
        AssignedByEmail: entity.AssignedByEmail,
        AssignedAt: entity.AssignedAt,
        CompletedAt: entity.CompletedAt);

    public static AgentChatMessage ToModel(this AgentChatMessageEntity entity) => new(
        MessageId: entity.MessageId,
        RequestId: entity.RequestId,
        AgentKey: entity.AgentKey,
        Role: (AgentChatRole)entity.Role,
        AuthorEmail: entity.AuthorEmail,
        AuthorName: entity.AuthorName,
        Body: entity.Body,
        PostedAt: entity.PostedAt);

    public static AgentProposal ToModel(this AgentProposalEntity entity, IRequestAgent? agent) => new(
        ProposalId: entity.ProposalId,
        RequestId: entity.RequestId,
        AgentKey: entity.AgentKey,
        DisplayName: agent?.DisplayName ?? entity.AgentKey,
        Status: (AgentProposalStatus)entity.Status,
        Summary: entity.Summary,
        StructuredJson: entity.StructuredJson,
        Rationale: entity.Rationale,
        CreatedAt: entity.CreatedAt,
        DecidedByEmail: entity.DecidedByEmail,
        DecidedAt: entity.DecidedAt);
}
