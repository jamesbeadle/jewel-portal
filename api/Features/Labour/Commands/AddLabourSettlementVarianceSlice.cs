using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Resolution path (4) of the settlement model: an accepted invoice-vs-timesheet difference
// posts as a visible variance against the cost code, so posted cost of sales equals cash paid.

public sealed class AddLabourSettlementVarianceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddLabourSettlementVarianceHandler handler;
    public AddLabourSettlementVarianceEndpoint(SignedInUserResolver users, AddLabourSettlementVarianceHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(AddLabourSettlementVariance))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/labour/settlement-variances")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        var body = await request.ReadFromJsonAsync<AddLabourSettlementVariance>();
        if (body is null) return new BadRequestResult();
        var command = body with { ProjectId = projectId };
        if (string.IsNullOrWhiteSpace(command.CostCode)) return new BadRequestObjectResult(new[] { "A cost code is required." });
        if (command.Amount == 0m) return new BadRequestObjectResult(new[] { "A non-zero amount is required." });
        if (string.IsNullOrWhiteSpace(command.Reason)) return new BadRequestObjectResult(new[] { "A reason is required — settlement variances are never silent." });
        return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
    }
}

public sealed class AddLabourSettlementVarianceHandler : ICommandHandler<AddLabourSettlementVariance, LabourSettlementVariance>
{
    private readonly JpmsContext context;
    public AddLabourSettlementVarianceHandler(JpmsContext context) { this.context = context; }

    public Task<LabourSettlementVariance> HandleAsync(AddLabourSettlementVariance command, CancellationToken cancellationToken) =>
        HandleAsync(command, createdByEmail: "", cancellationToken);

    public async Task<LabourSettlementVariance> HandleAsync(AddLabourSettlementVariance command, string createdByEmail, CancellationToken cancellationToken)
    {
        var variance = new LabourSettlementVarianceEntity
        {
            LabourSettlementVarianceId = LabourIdentifierFactory.NextLabourSettlementVarianceId(),
            ProjectId = command.ProjectId,
            CostCode = command.CostCode,
            SubcontractorId = command.SubcontractorId ?? "",
            Amount = command.Amount,
            Reason = command.Reason.Trim(),
            XeroLedgerLineId = command.XeroLedgerLineId,
            CreatedByEmail = createdByEmail,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        context.LabourSettlementVariances.Add(variance);
        await context.SaveChangesAsync(cancellationToken);
        return variance.ToModel();
    }
}
