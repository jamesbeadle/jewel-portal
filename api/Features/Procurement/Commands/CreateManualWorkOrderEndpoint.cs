using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateManualWorkOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateManualWorkOrderAuthorisation authorisation;
    private readonly CreateManualWorkOrderValidation validation;
    private readonly ICommandHandler<CreateManualWorkOrder, WorkOrder> handler;
    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public CreateManualWorkOrderEndpoint(
        SignedInUserResolver users,
        CreateManualWorkOrderAuthorisation authorisation,
        CreateManualWorkOrderValidation validation,
        ICommandHandler<CreateManualWorkOrder, WorkOrder> handler,
        JpmsContext context,
        AuditTrail audit)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.context = context;
        this.audit = audit;
    }

    [Function(nameof(CreateManualWorkOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/work-orders")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateManualWorkOrder>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to raise work orders.")
            { StatusCode = StatusCodes.Status403Forbidden };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        // The raise guardrail (2026-08-17): every centre the order commits cost to should have a
        // priced sale — a contract or variation line — on the valuation report. The raise dialog
        // warns and re-sends acknowledged; an unacknowledged order is refused here, and a
        // confirmed override is recorded in the audit trail once the order exists. The gate
        // lives on this HTTP door only: CreateWorkOrderFromMessage delegates to the same handler
        // for triage raises, which stay unguarded by design (scope decision 2026-08-17).
        var uncoveredCentres = await UncoveredCostCentres.FindAsync(
            context, command.ProjectId, command.Lines.Select(line => line.CostCode),
            request.HttpContext.RequestAborted);
        if (uncoveredCentres.Count > 0 && !command.UncoveredCostCentresAcknowledged)
            return new BadRequestObjectResult(
                $"The valuation report has no priced line for cost centre{Plural(uncoveredCentres)} "
                + $"{string.Join(", ", uncoveredCentres)} — confirm the warning to raise the order anyway.");

        try
        {
            var order = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            await RecordOverrideAsync(command, order, uncoveredCentres, request.HttpContext.RequestAborted);
            return new OkObjectResult(order);
        }
        catch (InvalidOperationException ex)
        {
            // Business-rule refusals (unknown project / subcontractor / cost centre)
            // read back to the user rather than surfacing as a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }

    /// <summary>The confirmed override, on the audit trail once the order exists — best-effort,
    /// after the save, per the AuditTrail convention. Reaching here with uncovered centres means
    /// the command was acknowledged: the gate above refused every other combination.</summary>
    private async Task RecordOverrideAsync(
        CreateManualWorkOrder command, WorkOrder order, IReadOnlyList<string> uncoveredCentres, CancellationToken cancellationToken)
    {
        if (uncoveredCentres.Count == 0) return;
        var orderLabel = order.Number > 0 ? order.Reference : $"Draft work order \"{order.Title}\"";
        await audit.WriteAsync(
            AuditEventType.WorkOrderSaleWarningOverridden,
            $"{orderLabel} raised against cost centre{Plural(uncoveredCentres)} {string.Join(", ", uncoveredCentres)} "
            + "with no priced valuation report line — the warning was confirmed and overridden.",
            projectId: command.ProjectId,
            recordType: RecordType.WorkOrder,
            recordId: order.WorkOrderId,
            recordReference: order.Reference,
            actorEmail: command.RaisedByEmail,
            cancellationToken: cancellationToken);
    }

    private static string Plural(IReadOnlyList<string> centres) => centres.Count == 1 ? "" : "s";
}
