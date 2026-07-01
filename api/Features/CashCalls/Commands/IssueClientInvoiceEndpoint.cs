using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

/// <summary>POST /api/cash-calls/{cashCallId}/invoice — mark the client invoice as prepared.</summary>
public sealed class IssueClientInvoiceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IssueClientInvoiceAuthorisation authorisation;
    private readonly IssueClientInvoiceValidation validation;
    private readonly ICommandHandler<IssueClientInvoice, CashCall> handler;

    public IssueClientInvoiceEndpoint(
        SignedInUserResolver users,
        IssueClientInvoiceAuthorisation authorisation,
        IssueClientInvoiceValidation validation,
        ICommandHandler<IssueClientInvoice, CashCall> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(IssueClientInvoice))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cash-calls/{cashCallId}/invoice")] HttpRequest request,
        string cashCallId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new IssueClientInvoice(cashCallId);

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
