using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.DocumentControl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.DocumentControl.Queries;

public sealed class ListPaymentCertificatesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListPaymentCertificates, IReadOnlyList<PaymentCertificate>> handler;

    public ListPaymentCertificatesEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListPaymentCertificates, IReadOnlyList<PaymentCertificate>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListPaymentCertificates))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "finance/payment-certificates")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!DocumentControlRoles.AllowedToReadPaymentCertificates.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(403);

        string? projectId = request.Query.TryGetValue("projectId", out var value) ? value.ToString() : null;
        var certificates = await handler.HandleAsync(
            new ListPaymentCertificates(string.IsNullOrWhiteSpace(projectId) ? null : projectId),
            request.HttpContext.RequestAborted);
        return new OkObjectResult(certificates);
    }
}
