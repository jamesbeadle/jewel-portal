using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

// Records the human's message, asks the agent to respond (handing it the assembled request context),
// records the agent's reply, and returns that reply. A stub agent answers "not implemented".
public sealed class SendAgentMessageHandler : ICommandHandler<SendAgentMessage, AgentChatMessage>
{
    private readonly JpmsContext context;
    private readonly AgentRegistry registry;
    private readonly RequestContextAssembler assembler;
    public SendAgentMessageHandler(JpmsContext context, AgentRegistry registry, RequestContextAssembler assembler)
    { this.context = context; this.registry = registry; this.assembler = assembler; }

    public async Task<AgentChatMessage> HandleAsync(SendAgentMessage command, CancellationToken cancellationToken)
    {
        var agent = registry.Find(command.AgentKey)
            ?? throw new InvalidOperationException($"Unknown agent '{command.AgentKey}'.");

        var now = DateTimeOffset.UtcNow;

        var userMessage = new AgentChatMessageEntity
        {
            MessageId = AgentsIdentifierFactory.Next(),
            RequestId = command.RequestId,
            AgentKey = command.AgentKey,
            Role = (int)AgentChatRole.User,
            AuthorEmail = command.AuthorEmail,
            AuthorName = command.AuthorName,
            Body = command.Body,
            PostedAt = now
        };
        context.AgentChatMessages.Add(userMessage);

        var requestContext = await assembler.AssembleAsync(command.RequestId, cancellationToken)
            ?? new RequestAgentContext(command.RequestId, "(request not found)", "", "");
        var replyBody = await agent.RespondAsync(requestContext, command.Body, cancellationToken);

        var agentReply = new AgentChatMessageEntity
        {
            MessageId = AgentsIdentifierFactory.Next(),
            RequestId = command.RequestId,
            AgentKey = command.AgentKey,
            Role = (int)AgentChatRole.Agent,
            AuthorEmail = command.AgentKey,
            AuthorName = agent.DisplayName,
            Body = replyBody,
            PostedAt = DateTimeOffset.UtcNow
        };
        context.AgentChatMessages.Add(agentReply);

        await context.SaveChangesAsync(cancellationToken);
        return agentReply.ToModel();
    }
}
