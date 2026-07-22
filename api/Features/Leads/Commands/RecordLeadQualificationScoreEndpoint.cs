using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordLeadQualificationScoreEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordLeadQualificationScoreAuthorisation authorisation;
    private readonly RecordLeadQualificationScoreValidation validation;
    private readonly ICommandHandler<RecordLeadQualificationScore, QualificationAssessment> handler;

    public RecordLeadQualificationScoreEndpoint(
        SignedInUserResolver users,
        RecordLeadQualificationScoreAuthorisation authorisation,
        RecordLeadQualificationScoreValidation validation,
        ICommandHandler<RecordLeadQualificationScore, QualificationAssessment> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RecordLeadQualificationScore))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leads/{leadId}/qualification")] HttpRequest request,
        string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RecordLeadQualificationScore>();
        if (command is null) return new BadRequestResult();
        if (command.LeadId != leadId) return new BadRequestObjectResult("Route leadId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var qualification = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(qualification);
    }
}
