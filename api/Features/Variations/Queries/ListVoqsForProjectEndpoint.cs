using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class ListVoqsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListVoqsForProject, IReadOnlyList<VariationOrderQuote>> handler;

    public ListVoqsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListVoqsForProject, IReadOnlyList<VariationOrderQuote>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListVoqsForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/voqs")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var voqs = await handler.HandleAsync(new ListVoqsForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(voqs);
    }
}
