using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Inventory.Commands;

public sealed class CreateInventoryItemFromMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateInventoryItemFromMessageAuthorisation authorisation;
    private readonly CreateInventoryItemFromMessageValidation validation;
    private readonly ICommandHandler<CreateInventoryItemFromMessage, InventoryItem> handler;

    public CreateInventoryItemFromMessageEndpoint(
        SignedInUserResolver users,
        CreateInventoryItemFromMessageAuthorisation authorisation,
        CreateInventoryItemFromMessageValidation validation,
        ICommandHandler<CreateInventoryItemFromMessage, InventoryItem> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(CreateInventoryItemFromMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/create-inventory-item")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateInventoryItemFromMessage>();
        if (command is null || string.IsNullOrWhiteSpace(command.MessageId) || string.IsNullOrWhiteSpace(command.ProjectId))
            return new BadRequestObjectResult("messageId and projectId are required.");

        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to add inventory items.")
            { StatusCode = StatusCodes.Status403Forbidden };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Business-rule refusals (an email that can't be read back for tagging) read back to
            // the user rather than a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
