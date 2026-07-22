using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class IssueProposalEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IssueProposalAuthorisation authorisation;
    private readonly IssueProposalValidation validation;
    private readonly ICommandHandler<IssueProposal, Proposal> handler;

    public IssueProposalEndpoint(
        SignedInUserResolver users,
        IssueProposalAuthorisation authorisation,
        IssueProposalValidation validation,
        ICommandHandler<IssueProposal, Proposal> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(IssueProposal))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leads/{leadId}/proposal")] HttpRequest request,
        string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<IssueProposal>();
        if (command is null) return new BadRequestResult();
        if (command.LeadId != leadId) return new BadRequestObjectResult("Route leadId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var proposal = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(proposal);
    }
}
