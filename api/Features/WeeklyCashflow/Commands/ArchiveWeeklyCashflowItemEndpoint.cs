using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class ArchiveWeeklyCashflowItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ArchiveWeeklyCashflowItemAuthorisation authorisation;
    private readonly ICommandHandler<ArchiveWeeklyCashflowItem, WeeklyCashflowItem> handler;

    public ArchiveWeeklyCashflowItemEndpoint(
        SignedInUserResolver users,
        ArchiveWeeklyCashflowItemAuthorisation authorisation,
        ICommandHandler<ArchiveWeeklyCashflowItem, WeeklyCashflowItem> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(ArchiveWeeklyCashflowItem))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "weekly-cashflow/items/{weeklyCashflowItemId}/archive")] HttpRequest request,
        string weeklyCashflowItemId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new ArchiveWeeklyCashflowItem(weeklyCashflowItemId, signedInUser.Email);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
