using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Queries;

/// <summary>GET /api/ai/conversations/{conversationId} — replay a conversation the caller started.</summary>
public sealed class ListAiConversationEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IQueryHandler<ListAiConversation, IReadOnlyList<AiChatMessage>> handler;

    public ListAiConversationEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IQueryHandler<ListAiConversation, IReadOnlyList<AiChatMessage>> handler)
    {
        this.users = users;
        this.context = context;
        this.handler = handler;
    }

    [Function(nameof(ListAiConversation))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ai/conversations/{conversationId}")] HttpRequest request,
        string conversationId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AiRoles.AllowedToUseAssistant.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        // A conversation id is not a capability — you can only replay your own.
        var owned = await context.AiConversations
            .AsNoTracking()
            .AnyAsync(row => row.ConversationId == conversationId
                             && row.StartedByEmail == signedInUser.Email, cancellationToken);
        if (!owned) return new NotFoundResult();

        return new OkObjectResult(await handler.HandleAsync(new ListAiConversation(conversationId), cancellationToken));
    }
}
