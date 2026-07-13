using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Marks a Xero purchase line as settlement of approved timesheets (scope §6): the approved
// timesheet is the timely actual, the invoice is settlement of it, so covered lines are
// excluded from the cost-of-sales aggregation to prevent labour double-counting.

public sealed class SetXeroLineTimesheetCoverEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetXeroLineTimesheetCoverHandler handler;
    public SetXeroLineTimesheetCoverEndpoint(SignedInUserResolver users, SetXeroLineTimesheetCoverHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SetXeroLineTimesheetCover))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/timesheet-covers")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        var command = await request.ReadFromJsonAsync<SetXeroLineTimesheetCover>();
        if (command is null || string.IsNullOrWhiteSpace(command.XeroLedgerLineId)) return new BadRequestResult();
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class SetXeroLineTimesheetCoverHandler : ICommandHandler<SetXeroLineTimesheetCover, Acknowledgement>
{
    private readonly JpmsContext context;
    public SetXeroLineTimesheetCoverHandler(JpmsContext context) { this.context = context; }

    public Task<Acknowledgement> HandleAsync(SetXeroLineTimesheetCover command, CancellationToken cancellationToken) =>
        HandleAsync(command, createdByEmail: "", cancellationToken);

    public async Task<Acknowledgement> HandleAsync(SetXeroLineTimesheetCover command, string createdByEmail, CancellationToken cancellationToken)
    {
        var existing = await context.XeroLineTimesheetCovers.FirstOrDefaultAsync(
            cover => cover.XeroLedgerLineId == command.XeroLedgerLineId, cancellationToken);

        if (!command.IsCovered)
        {
            if (existing is not null) context.XeroLineTimesheetCovers.Remove(existing);
            await context.SaveChangesAsync(cancellationToken);
            return new Acknowledgement(command.XeroLedgerLineId);
        }

        var lineExists = await context.XeroLedgerLines.AnyAsync(
            line => line.XeroLedgerLineId == command.XeroLedgerLineId, cancellationToken);
        if (!lineExists) throw new InvalidOperationException("Xero line not found.");

        if (existing is null)
        {
            existing = new XeroLineTimesheetCoverEntity
            {
                XeroLineTimesheetCoverId = LabourIdentifierFactory.NextXeroLineTimesheetCoverId(),
                XeroLedgerLineId = command.XeroLedgerLineId,
            };
            context.XeroLineTimesheetCovers.Add(existing);
        }
        existing.ProjectId = command.ProjectId;
        existing.SubcontractorId = command.SubcontractorId;
        existing.PeriodStart = command.PeriodStart;
        existing.PeriodEnd = command.PeriodEnd;
        existing.CreatedByEmail = createdByEmail;
        existing.CreatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(existing.XeroLineTimesheetCoverId);
    }
}
