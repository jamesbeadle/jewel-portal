using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class ReviseProposalEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReviseProposalAuthorisation authorisation;
    private readonly ReviseProposalValidation validation;
    private readonly ICommandHandler<ReviseProposal, Proposal> handler;

    public ReviseProposalEndpoint(
        SignedInUserResolver users,
        ReviseProposalAuthorisation authorisation,
        ReviseProposalValidation validation,
        ICommandHandler<ReviseProposal, Proposal> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ReviseProposal))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "leads/{leadId}/proposal")] HttpRequest request,
        string leadId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<ReviseProposal>();
        if (command is null) return new BadRequestResult();
        if (command.LeadId != leadId) return new BadRequestObjectResult("Route leadId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var proposal = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(proposal);
    }
}
