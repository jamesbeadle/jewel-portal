using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.DocumentControl.Queries;

public sealed class ListDocumentControlItemsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListDocumentControlItems, IReadOnlyList<DocumentControlItem>> handler;

    public ListDocumentControlItemsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListDocumentControlItems, IReadOnlyList<DocumentControlItem>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListDocumentControlItems))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "document-control/items")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!DocumentControlRoles.AllowedToManage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var items = await handler.HandleAsync(new ListDocumentControlItems(), request.HttpContext.RequestAborted);
        return new OkObjectResult(items);
    }
}
