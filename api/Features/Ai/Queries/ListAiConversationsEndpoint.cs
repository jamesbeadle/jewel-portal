using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Queries;

/// <summary>
/// GET /api/ai/conversations — the caller's past conversations, newest first, for the panel's
/// history list. Always scoped to the signed-in user: the email comes from the session, never from
/// the request, so nobody can list anybody else's threads. No separate handler on purpose — the
/// scoping IS the query, and splitting it out would leave a handler that must trust an email
/// parameter.
/// </summary>
public sealed class ListAiConversationsEndpoint
{
    /// <summary>Hard ceiling whatever the client asks for — this is a picker, not an export.</summary>
    private const int MaxTake = 100;

    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;

    public ListAiConversationsEndpoint(SignedInUserResolver users, JpmsContext context)
    {
        this.users = users;
        this.context = context;
    }

    [Function(nameof(ListAiConversations))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ai/conversations")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AiRoles.AllowedToUseAssistant.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var take = int.TryParse(request.Query["take"], out var parsed) ? parsed : 30;
        take = Math.Clamp(take, 1, MaxTake);

        // IX_AiConversations_StartedByEmail_LastMessageAt makes this the cheap read it looks like.
        var rows = await context.AiConversations
            .AsNoTracking()
            .Where(row => row.StartedByEmail == signedInUser.Email)
            .OrderByDescending(row => row.LastMessageAt)
            .Take(take)
            .Select(row => new AiConversationSummary(
                row.ConversationId, row.Title ?? "", row.ProjectId, row.LastMessageAt))
            .ToListAsync(cancellationToken);

        return new OkObjectResult(rows);
    }
}
