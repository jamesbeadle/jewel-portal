using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class ApproveTimesheetEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ApproveTimesheetAuthorisation authorisation;
    private readonly ApproveTimesheetValidation validation;
    private readonly ICommandHandler<ApproveTimesheet, Timesheet> handler;
    public ApproveTimesheetEndpoint(SignedInUserResolver users, ApproveTimesheetAuthorisation authorisation, ApproveTimesheetValidation validation, ICommandHandler<ApproveTimesheet, Timesheet> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(ApproveTimesheet))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "timesheets/{timesheetId}/approval")] HttpRequest request, string timesheetId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new ApproveTimesheet(timesheetId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
