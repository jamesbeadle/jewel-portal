using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class LinkXeroLineToWorkOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly LinkXeroLineToWorkOrderAuthorisation authorisation;
    private readonly LinkXeroLineToWorkOrderValidation validation;
    private readonly ICommandHandler<LinkXeroLineToWorkOrder, Acknowledgement> handler;

    public LinkXeroLineToWorkOrderEndpoint(
        SignedInUserResolver users,
        LinkXeroLineToWorkOrderAuthorisation authorisation,
        LinkXeroLineToWorkOrderValidation validation,
        ICommandHandler<LinkXeroLineToWorkOrder, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(LinkXeroLineToWorkOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/xero-line-work-order-links")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<LinkXeroLineToWorkOrder>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        // Not ForbidResult: executing it needs a registered authentication scheme, and this app's
        // cookie-session auth has none — it throws, the function 500s, and Static Web Apps hands the
        // client an opaque "Backend call failure". Return the 403 with a message the modal can show.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to link invoices to work orders.")
            { StatusCode = StatusCodes.Status403Forbidden };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
