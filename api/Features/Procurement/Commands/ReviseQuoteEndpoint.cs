using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class ReviseQuoteEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReviseQuoteAuthorisation authorisation;
    private readonly ReviseQuoteValidation validation;
    private readonly ICommandHandler<ReviseQuote, Quote> handler;

    public ReviseQuoteEndpoint(SignedInUserResolver users, ReviseQuoteAuthorisation authorisation, ReviseQuoteValidation validation, ICommandHandler<ReviseQuote, Quote> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(ReviseQuote))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "quotes/{quoteId}")] HttpRequest request,
        string quoteId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<ReviseQuote>();
        if (command is null) return new BadRequestResult();
        if (command.QuoteId != quoteId) return new BadRequestObjectResult("Route quoteId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
