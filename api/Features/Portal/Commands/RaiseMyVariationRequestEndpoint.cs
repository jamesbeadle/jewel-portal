using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Portal;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Portal.Commands;

/// <summary>
/// POST /api/portal/my/work-orders/{workOrderId}/variation-requests — the subcontractor raises a
/// priced variation request against one of their own work orders. The subcontractor id comes from
/// the session (SubcontractorScope); the handler verifies the work order belongs to them.
/// </summary>
public sealed class RaiseMyVariationRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<RaiseMyVariationRequest, SubcontractorVariationRequest> handler;

    public RaiseMyVariationRequestEndpoint(
        SignedInUserResolver users, ICommandHandler<RaiseMyVariationRequest, SubcontractorVariationRequest> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function("RaiseMyVariationRequest")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "portal/my/work-orders/{workOrderId}/variation-requests")] HttpRequest request,
        string workOrderId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var subcontractorId = SubcontractorScope.OwnSubcontractorId(signedInUser);
        if (subcontractorId is null) return new StatusCodeResult(403);

        RaiseMyVariationRequest? command;
        try { command = await request.ReadFromJsonAsync<RaiseMyVariationRequest>(cancellationToken); }
        catch { return new BadRequestResult(); }
        if (command is null) return new BadRequestResult();
        if (!string.Equals(command.WorkOrderId, workOrderId, StringComparison.OrdinalIgnoreCase))
            return new BadRequestObjectResult("Route workOrderId does not match body.");

        if (string.IsNullOrWhiteSpace(command.Title)) return new BadRequestObjectResult("A title is required.");
        if (string.IsNullOrWhiteSpace(command.Description)) return new BadRequestObjectResult("Describe the change and why it's needed.");
        if (command.ProposedValue <= 0) return new BadRequestObjectResult("A proposed value greater than zero is required.");

        // Never trust the body's SubcontractorId — the session decides whose request this is.
        try
        {
            var result = await handler.HandleAsync(command with { SubcontractorId = subcontractorId }, cancellationToken);
            return new OkObjectResult(result);
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
