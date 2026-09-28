using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

/// <summary>POST contractors-reports/{id}/rewrite — the week rewritten in house language, flags open.</summary>
public sealed class RewriteContractorsReportWeekEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RewriteContractorsReportWeekAuthorisation authorisation;
    private readonly ICommandHandler<RewriteContractorsReportWeek, ContractorsReport> handler;

    public RewriteContractorsReportWeekEndpoint(
        SignedInUserResolver users, RewriteContractorsReportWeekAuthorisation authorisation,
        ICommandHandler<RewriteContractorsReportWeek, ContractorsReport> handler)
    { this.users = users; this.authorisation = authorisation; this.handler = handler; }

    [Function(nameof(RewriteContractorsReportWeek))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contractors-reports/{contractorsReportId}/rewrite")] HttpRequest request,
        string contractorsReportId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new RewriteContractorsReportWeek(contractorsReportId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        try { return new OkObjectResult(await handler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException reason) { return new BadRequestObjectResult(new[] { reason.Message }); }
    }
}
