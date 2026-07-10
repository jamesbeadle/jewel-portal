using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// POST /api/projects/{projectId}/valuation-report-snapshots — freeze the report as it stands
/// (on-demand period-end record, or the automatic capture behind an invoice submission).
/// Body: { label, valuationInvoiceId? }.
/// </summary>
public sealed class TakeValuationReportSnapshotEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationReportAuthorisation authorisation;
    private readonly TakeValuationReportSnapshotValidation validation;
    private readonly ICommandHandler<TakeValuationReportSnapshot, ValuationReportSnapshot> handler;
    public TakeValuationReportSnapshotEndpoint(SignedInUserResolver users, ValuationReportAuthorisation authorisation, TakeValuationReportSnapshotValidation validation, ICommandHandler<TakeValuationReportSnapshot, ValuationReportSnapshot> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(TakeValuationReportSnapshot))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/valuation-report-snapshots")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<TakeValuationReportSnapshot>();
        if (body is null) return new BadRequestResult();
        var command = body with { ProjectId = projectId };
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
