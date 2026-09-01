using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// POST /api/valuation-report-snapshots/{snapshotId}/draft-email — draft the valuation-report
/// email (frozen report attached as PDF) in the shared mailbox for a human to review and send
/// from Outlook. Body: { subject, htmlBody }. Nothing is sent from here.
/// </summary>
public sealed class PrepareValuationReportSnapshotEmailDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PrepareValuationReportSnapshotEmailDraftAuthorisation authorisation;
    private readonly PrepareValuationReportSnapshotEmailDraftValidation validation;
    private readonly ICommandHandler<PrepareValuationReportSnapshotEmailDraft, ValuationReportSnapshotEmailDraft> handler;

    public PrepareValuationReportSnapshotEmailDraftEndpoint(
        SignedInUserResolver users,
        PrepareValuationReportSnapshotEmailDraftAuthorisation authorisation,
        PrepareValuationReportSnapshotEmailDraftValidation validation,
        ICommandHandler<PrepareValuationReportSnapshotEmailDraft, ValuationReportSnapshotEmailDraft> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(PrepareValuationReportSnapshotEmailDraft))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-report-snapshots/{snapshotId}/draft-email")] HttpRequest request,
        string snapshotId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<PrepareValuationReportSnapshotEmailDraft>();
        if (body is null) return new BadRequestResult();
        var command = body with { ValuationReportSnapshotId = snapshotId };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
