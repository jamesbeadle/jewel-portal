using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// POST /api/valuation-report-snapshots/{snapshotId}/draft-email — draft the valuation-report
/// email (frozen report attached as PDF) in the shared mailbox for a human to review and send
/// from Outlook. Body: { subject, htmlBody }. Nothing is sent from here.
/// </summary>
public sealed class SendValuationReportSnapshotEmailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SendValuationReportSnapshotEmailAuthorisation authorisation;
    private readonly SendValuationReportSnapshotEmailValidation validation;
    private readonly ICommandHandler<SendValuationReportSnapshotEmail, ValuationReportSnapshotEmailOutcome> handler;

    public SendValuationReportSnapshotEmailEndpoint(
        SignedInUserResolver users,
        SendValuationReportSnapshotEmailAuthorisation authorisation,
        SendValuationReportSnapshotEmailValidation validation,
        ICommandHandler<SendValuationReportSnapshotEmail, ValuationReportSnapshotEmailOutcome> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(SendValuationReportSnapshotEmail))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-report-snapshots/{snapshotId}/draft-email")] HttpRequest request,
        string snapshotId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<SendValuationReportSnapshotEmail>();
        if (body is null) return new BadRequestResult();
        var command = body with { ValuationReportSnapshotId = snapshotId };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
