using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

public sealed class GetSettlementSchedulesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetSettlementSchedules, SettlementScheduleSnapshot> handler;
    public GetSettlementSchedulesEndpoint(SignedInUserResolver users, IQueryHandler<GetSettlementSchedules, SettlementScheduleSnapshot> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetSettlementSchedules))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "labour/schedules/{year:int}/{month:int}")] HttpRequest request,
        int year, int month)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (year < 2020 || year > 2100 || month < 1 || month > 12) return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(new GetSettlementSchedules(year, month), request.HttpContext.RequestAborted));
    }
}

public sealed class GetSettlementSchedulesHandler : IQueryHandler<GetSettlementSchedules, SettlementScheduleSnapshot>
{
    private readonly SettlementScheduleBuilder builder;
    public GetSettlementSchedulesHandler(SettlementScheduleBuilder builder) { this.builder = builder; }

    public Task<SettlementScheduleSnapshot> HandleAsync(GetSettlementSchedules query, CancellationToken cancellationToken) =>
        builder.BuildAsync(query.Year, query.Month, cancellationToken);
}
