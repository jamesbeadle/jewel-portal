using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Inventory.Commands;

public sealed class UpdateInventoryItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateInventoryItemAuthorisation authorisation;
    private readonly UpdateInventoryItemValidation validation;
    private readonly ICommandHandler<UpdateInventoryItem, InventoryItem> handler;
    public UpdateInventoryItemEndpoint(SignedInUserResolver users, UpdateInventoryItemAuthorisation authorisation, UpdateInventoryItemValidation validation, ICommandHandler<UpdateInventoryItem, InventoryItem> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateInventoryItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "inventory/{inventoryItemId}")] HttpRequest request, string inventoryItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateInventoryItem>();
        if (command is null) return new BadRequestResult();
        if (command.InventoryItemId != inventoryItemId) return new BadRequestObjectResult("Route inventoryItemId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
