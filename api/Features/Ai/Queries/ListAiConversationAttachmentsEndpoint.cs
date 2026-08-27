using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Queries;

/// <summary>
/// GET /api/ai/conversations/{conversationId}/attachments — the files attached to one
/// conversation: names, types and sizes, never the bytes. Read by chat-aware dialogs (the
/// work-order form's "files from this chat" list) so the quote an order was drafted from can be
/// kept on the order. Scoped like the replay endpoint: only the conversation's own starter gets
/// rows back — anyone else's id answers an empty list, indistinguishable from a bare
/// conversation, so the endpoint leaks nothing about other people's chats. No separate handler on
/// purpose — the scoping IS the query (the ListAiConversations reasoning).
/// </summary>
public sealed class ListAiConversationAttachmentsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;

    public ListAiConversationAttachmentsEndpoint(SignedInUserResolver users, JpmsContext context)
    {
        this.users = users;
        this.context = context;
    }

    [Function(nameof(ListAiConversationAttachments))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ai/conversations/{conversationId}/attachments")] HttpRequest request,
        string conversationId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AiRoles.AllowedToUseAssistant.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var owned = await context.AiConversations
            .AsNoTracking()
            .AnyAsync(row => row.ConversationId == conversationId
                             && row.StartedByEmail == signedInUser.Email, cancellationToken);
        if (!owned) return new OkObjectResult(Array.Empty<AiConversationAttachment>());

        var rows = await context.AiAttachments
            .AsNoTracking()
            .Where(row => row.ConversationId == conversationId)
            .OrderBy(row => row.UploadedAt)
            .Select(row => new AiConversationAttachment(
                row.AttachmentId, row.FileName, row.ContentType, row.SizeBytes, row.UploadedAt))
            .ToListAsync(cancellationToken);

        return new OkObjectResult(rows);
    }
}
