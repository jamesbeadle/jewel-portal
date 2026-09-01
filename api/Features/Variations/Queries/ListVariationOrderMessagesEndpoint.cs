using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class ListVariationOrderMessagesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListVariationOrderMessages, IReadOnlyList<VariationOrderMessage>> handler;

    public ListVariationOrderMessagesEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListVariationOrderMessages, IReadOnlyList<VariationOrderMessage>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Variation reads are internal plus the architect, same as the order itself. Clients read
    // the shared thread through their own scoped endpoint (Features/ClientPortal), never here —
    // this view includes internal notes.
    private static readonly RoleSet RolesThatMayReadVariations = JpmsRoleSets.InternalAndArchitect;

    [Function(nameof(ListVariationOrderMessages))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "variation-orders/{voId}/messages")] HttpRequest request,
        string voId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadVariations.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var messages = await handler.HandleAsync(new ListVariationOrderMessages(voId), request.HttpContext.RequestAborted);
        return new OkObjectResult(messages);
    }
}
