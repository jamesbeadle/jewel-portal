using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class ListLeadsInPipelineEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListLeadsInPipeline, IReadOnlyList<Lead>> handler;

    public ListLeadsInPipelineEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListLeadsInPipeline, IReadOnlyList<Lead>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Lead pipeline reads are internal-only; external portal logins have no business here.
    private static readonly RoleSet RolesThatMayReadLeads = JpmsRoleSets.AllInternal;

    [Function(nameof(ListLeadsInPipeline))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadLeads.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var leads = await handler.HandleAsync(new ListLeadsInPipeline(), request.HttpContext.RequestAborted);
        return new OkObjectResult(leads);
    }
}
