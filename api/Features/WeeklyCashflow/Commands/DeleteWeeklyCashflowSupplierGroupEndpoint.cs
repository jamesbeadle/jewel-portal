using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class DeleteWeeklyCashflowSupplierGroupEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteWeeklyCashflowSupplierGroupAuthorisation authorisation;
    private readonly ICommandHandler<DeleteWeeklyCashflowSupplierGroup, WeeklyCashflowSupplierGroup> handler;

    public DeleteWeeklyCashflowSupplierGroupEndpoint(
        SignedInUserResolver users,
        DeleteWeeklyCashflowSupplierGroupAuthorisation authorisation,
        ICommandHandler<DeleteWeeklyCashflowSupplierGroup, WeeklyCashflowSupplierGroup> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(DeleteWeeklyCashflowSupplierGroup))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "weekly-cashflow/supplier-groups/{supplierGroupId}/delete")] HttpRequest request,
        string supplierGroupId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteWeeklyCashflowSupplierGroup(supplierGroupId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
