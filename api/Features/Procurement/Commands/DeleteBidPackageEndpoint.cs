using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class DeleteBidPackageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteBidPackageAuthorisation authorisation;
    private readonly DeleteBidPackageValidation validation;
    private readonly ICommandHandler<DeleteBidPackage, Acknowledgement> handler;

    public DeleteBidPackageEndpoint(SignedInUserResolver users, DeleteBidPackageAuthorisation authorisation, DeleteBidPackageValidation validation, ICommandHandler<DeleteBidPackage, Acknowledgement> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(DeleteBidPackage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "bid-packages/{bidPackageId}")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteBidPackage(bidPackageId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
