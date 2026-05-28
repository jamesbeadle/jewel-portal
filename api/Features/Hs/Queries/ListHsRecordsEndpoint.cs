using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Hs.Queries;

public sealed class ListHsRecordsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListHsRecords, IReadOnlyList<HsRecord>> handler;
    public ListHsRecordsEndpoint(SignedInUserResolver users, IQueryHandler<ListHsRecords, IReadOnlyList<HsRecord>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListHsRecords))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hs-records")] HttpRequest request)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListHsRecords(), request.HttpContext.RequestAborted));
    }
}
