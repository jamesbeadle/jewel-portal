using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class RecordAttendanceForHsRecordEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordAttendanceForHsRecordAuthorisation authorisation;
    private readonly RecordAttendanceForHsRecordValidation validation;
    private readonly ICommandHandler<RecordAttendanceForHsRecord, HsRecordAttendance> handler;

    public RecordAttendanceForHsRecordEndpoint(SignedInUserResolver users, RecordAttendanceForHsRecordAuthorisation authorisation, RecordAttendanceForHsRecordValidation validation, ICommandHandler<RecordAttendanceForHsRecord, HsRecordAttendance> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RecordAttendanceForHsRecord))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hs-records/{hsRecordId}/attendance")] HttpRequest request, string hsRecordId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<RecordAttendanceForHsRecord>();
        if (command is null) return new BadRequestResult();
        if (command.HsRecordId != hsRecordId) return new BadRequestObjectResult("Route hsRecordId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
