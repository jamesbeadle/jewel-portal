using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>POST /api/valuation-claims/{claimId}/entries/bulk — upsert many lines' % complete at once.</summary>
public sealed class RecordClaimEntriesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationReportAuthorisation authorisation;
    private readonly RecordClaimEntriesValidation validation;
    private readonly ICommandHandler<RecordClaimEntries, IReadOnlyList<ClaimLine>> handler;
    public RecordClaimEntriesEndpoint(SignedInUserResolver users, ValuationReportAuthorisation authorisation, RecordClaimEntriesValidation validation, ICommandHandler<RecordClaimEntries, IReadOnlyList<ClaimLine>> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RecordClaimEntries))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-claims/{claimId}/entries/bulk")] HttpRequest request, string claimId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<RecordClaimEntries>();
        if (command is null) return new BadRequestResult();
        if (command.ValuationClaimId != claimId) return new BadRequestObjectResult("Route claimId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
