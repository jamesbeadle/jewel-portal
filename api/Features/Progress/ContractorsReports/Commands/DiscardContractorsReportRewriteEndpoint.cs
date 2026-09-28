using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

/// <summary>POST contractors-reports/{id}/rewrite/discard — back to the raw notes.</summary>
public sealed class DiscardContractorsReportRewriteEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DiscardContractorsReportRewriteAuthorisation authorisation;
    private readonly ICommandHandler<DiscardContractorsReportRewrite, ContractorsReport> handler;

    public DiscardContractorsReportRewriteEndpoint(
        SignedInUserResolver users, DiscardContractorsReportRewriteAuthorisation authorisation,
        ICommandHandler<DiscardContractorsReportRewrite, ContractorsReport> handler)
    { this.users = users; this.authorisation = authorisation; this.handler = handler; }

    [Function(nameof(DiscardContractorsReportRewrite))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contractors-reports/{contractorsReportId}/rewrite/discard")] HttpRequest request,
        string contractorsReportId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new DiscardContractorsReportRewrite(contractorsReportId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        try { return new OkObjectResult(await handler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException reason) { return new BadRequestObjectResult(new[] { reason.Message }); }
    }
}
