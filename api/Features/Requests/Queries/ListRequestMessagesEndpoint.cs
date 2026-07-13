using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestMessagesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>> handler;
    public ListRequestMessagesEndpoint(SignedInUserResolver users, IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>> handler) { this.users = users; this.handler = handler; }

    // Request reads are internal plus the architect, who reads/approves RFIs per the permissions matrix.
    private static readonly RoleSet RolesThatMayReadRequests = JpmsRoleSets.InternalAndArchitect;

    [Function(nameof(ListRequestMessages))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/messages")] HttpRequest request, string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequests.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListRequestMessages(requestId), request.HttpContext.RequestAborted));
    }
}
