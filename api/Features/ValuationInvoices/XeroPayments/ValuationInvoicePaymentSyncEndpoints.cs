using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroPayments;

/// <summary>GET /api/valuation-invoices/payment-sync/preview?projectId=… — what syncing payments from Xero would do; nothing is written.</summary>
public sealed class PreviewValuationInvoicePaymentSyncEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<PreviewValuationInvoicePaymentSync, ValuationInvoicePaymentSyncPreview> handler;

    public PreviewValuationInvoicePaymentSyncEndpoint(SignedInUserResolver users, IQueryHandler<PreviewValuationInvoicePaymentSync, ValuationInvoicePaymentSyncPreview> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function(nameof(PreviewValuationInvoicePaymentSync))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-invoices/payment-sync/preview")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ValuationInvoiceRoles.AllowedToManageValuationInvoices.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var projectId = request.Query["projectId"].ToString();
        if (string.IsNullOrWhiteSpace(projectId)) return new BadRequestObjectResult("projectId is required.");
        try
        {
            return new OkObjectResult(await handler.HandleAsync(new PreviewValuationInvoicePaymentSync(projectId), request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}

/// <summary>POST /api/valuation-invoices/payment-sync — apply the plan: link matched invoices, record what Xero holds as paid. Body: { projectId }.</summary>
public sealed class SyncValuationInvoicePaymentsFromXeroEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly Audit.AuditActor auditActor;
    private readonly SyncValuationInvoicePaymentsAuthorisation authorisation;
    private readonly SyncValuationInvoicePaymentsValidation validation;
    private readonly ICommandHandler<SyncValuationInvoicePaymentsFromXero, ValuationInvoicePaymentSyncOutcome> handler;

    public SyncValuationInvoicePaymentsFromXeroEndpoint(
        SignedInUserResolver users,
        Audit.AuditActor auditActor,
        SyncValuationInvoicePaymentsAuthorisation authorisation,
        SyncValuationInvoicePaymentsValidation validation,
        ICommandHandler<SyncValuationInvoicePaymentsFromXero, ValuationInvoicePaymentSyncOutcome> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SyncValuationInvoicePaymentsFromXero))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-invoices/payment-sync")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        SyncValuationInvoicePaymentsFromXero? body;
        try { body = await request.ReadFromJsonAsync<SyncValuationInvoicePaymentsFromXero>(); }
        catch (JsonException) { body = null; }
        if (body is null) return new BadRequestObjectResult("A body with projectId is required.");

        // SyncedBy is stamped server-side — never trusted from the client.
        var command = body with { SyncedBy = signedInUser.Email };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(string.Join(" ", validationOutcome.Errors));

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException refusal)
        {
            return new BadRequestObjectResult(refusal.Message);
        }
    }
}
