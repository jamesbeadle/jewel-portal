using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Who may work the allocation queue: financially sensitive, so the same
/// finance-facing audience as the Xero ledger view and the cost-code master.
/// Admins pass because Role.Admin is included explicitly.
/// </summary>
internal static class XeroLedgerRoles
{
    public static readonly RoleSet AllowedToAllocate = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);
}

public sealed class ListXeroLedgerLinesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListXeroLedgerLines, IReadOnlyList<XeroLedgerLine>> handler;

    public ListXeroLedgerLinesEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListXeroLedgerLines, IReadOnlyList<XeroLedgerLine>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListXeroLedgerLines))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/ledger")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroLedgerRoles.AllowedToAllocate.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var lines = await handler.HandleAsync(new ListXeroLedgerLines(), request.HttpContext.RequestAborted);
        return new OkObjectResult(lines);
    }
}

public sealed class SyncXeroLedgerEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<SyncXeroLedger, XeroLedgerSyncResult> handler;

    public SyncXeroLedgerEndpoint(
        SignedInUserResolver users,
        ICommandHandler<SyncXeroLedger, XeroLedgerSyncResult> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(SyncXeroLedger))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "xero/ledger/sync")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroLedgerRoles.AllowedToAllocate.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        try
        {
            var result = await handler.HandleAsync(new SyncXeroLedger(), request.HttpContext.RequestAborted);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            // Sync spans Xero paging + a bulk SaveChanges; surface the real cause to the
            // allocation page (via HttpCommandSender) instead of an opaque 500. Inner
            // exceptions carry the SQL detail (e.g. truncation / missing table).
            var detail = ex.InnerException?.Message ?? ex.Message;
            return new BadRequestObjectResult($"Sync failed: {detail}");
        }
    }
}

public sealed class AllocateSuggestedXeroLinesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<AllocateSuggestedXeroLines, int> handler;

    public AllocateSuggestedXeroLinesEndpoint(
        SignedInUserResolver users,
        ICommandHandler<AllocateSuggestedXeroLines, int> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(AllocateSuggestedXeroLines))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "xero/allocations/suggested")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroLedgerRoles.AllowedToAllocate.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var command = new AllocateSuggestedXeroLines(signedInUser.Email);
        var allocated = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(allocated);
    }
}

public sealed class RetryXeroWriteBackEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<RetryXeroWriteBack, XeroWriteBackOutcome> handler;

    public RetryXeroWriteBackEndpoint(
        SignedInUserResolver users,
        ICommandHandler<RetryXeroWriteBack, XeroWriteBackOutcome> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(RetryXeroWriteBack))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "xero/writeback/retry")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroLedgerRoles.AllowedToAllocate.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var command = await request.ReadFromJsonAsync<RetryXeroWriteBack>();
        if (command is null || string.IsNullOrWhiteSpace(command.XeroInvoiceId)) return new BadRequestResult();

        var outcome = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(outcome);
    }
}

public sealed class SetXeroAllocationEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<SetXeroAllocation, int> handler;

    public SetXeroAllocationEndpoint(
        SignedInUserResolver users,
        ICommandHandler<SetXeroAllocation, int> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(SetXeroAllocation))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "xero/allocations")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroLedgerRoles.AllowedToAllocate.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var command = await request.ReadFromJsonAsync<SetXeroAllocation>();
        if (command is null || command.XeroLedgerLineIds.Count == 0) return new BadRequestResult();

        // AllocatedBy is stamped server-side — never trusted from the client.
        command = command with { AllocatedBy = signedInUser.Email };

        try
        {
            var affected = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(affected);
        }
        catch (InvalidOperationException ex)
        {
            // Bare string so HttpCommandSender surfaces it verbatim in the dialog.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
