using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class RemoveValuationLineItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationReportAuthorisation authorisation;
    private readonly ICommandHandler<RemoveValuationLineItem, Acknowledgement> handler;
    public RemoveValuationLineItemEndpoint(SignedInUserResolver users, ValuationReportAuthorisation authorisation, ICommandHandler<RemoveValuationLineItem, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.handler = handler; }

    [Function(nameof(RemoveValuationLineItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "valuation-lines/{lineItemId}")] HttpRequest request, string lineItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new RemoveValuationLineItem(lineItemId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
