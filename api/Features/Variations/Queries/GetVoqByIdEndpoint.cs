using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class GetVoqByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetVoqById, VariationOrderQuote?> handler;

    public GetVoqByIdEndpoint(SignedInUserResolver users, IQueryHandler<GetVoqById, VariationOrderQuote?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetVoqById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "voqs/{voqId}")] HttpRequest request,
        string voqId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var voq = await handler.HandleAsync(new GetVoqById(voqId), request.HttpContext.RequestAborted);
        return new OkObjectResult(voq);
    }
}
