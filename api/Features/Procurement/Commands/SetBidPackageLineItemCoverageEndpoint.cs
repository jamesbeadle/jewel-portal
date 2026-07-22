using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SetBidPackageLineItemCoverageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetBidPackageLineItemCoverageAuthorisation authorisation;
    private readonly SetBidPackageLineItemCoverageValidation validation;
    private readonly ICommandHandler<SetBidPackageLineItemCoverage, IReadOnlyList<BidPackageLineItem>> handler;

    public SetBidPackageLineItemCoverageEndpoint(SignedInUserResolver users, SetBidPackageLineItemCoverageAuthorisation authorisation, SetBidPackageLineItemCoverageValidation validation, ICommandHandler<SetBidPackageLineItemCoverage, IReadOnlyList<BidPackageLineItem>> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(SetBidPackageLineItemCoverage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "bid-package-line-items/{lineItemId}/coverage")] HttpRequest request,
        string lineItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SetBidPackageLineItemCoverage>();
        if (command is null) return new BadRequestResult();
        if (command.LineItemId != lineItemId) return new BadRequestObjectResult("Route lineItemId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
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
