using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Hs.Queries;

public sealed class ListAttendanceForHsRecordEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListAttendanceForHsRecord, IReadOnlyList<HsRecordAttendance>> handler;
    public ListAttendanceForHsRecordEndpoint(SignedInUserResolver users, IQueryHandler<ListAttendanceForHsRecord, IReadOnlyList<HsRecordAttendance>> handler) { this.users = users; this.handler = handler; }

    // H&S records are internal-only reads; external portal logins have no business here.
    private static readonly RoleSet RolesThatMayReadHsRecords = JpmsRoleSets.AllInternal;

    [Function(nameof(ListAttendanceForHsRecord))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hs-records/{hsRecordId}/attendance")] HttpRequest request,
        string hsRecordId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadHsRecords.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListAttendanceForHsRecord(hsRecordId), request.HttpContext.RequestAborted));
    }
}
