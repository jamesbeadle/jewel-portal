using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Queries;

/// <summary>GET /api/tender-enquiries/{id}/document — the PQQ response PDF, rendered fresh from the
/// answers on every call (nothing is stored). Mirrors the variation document endpoint.</summary>
public sealed class GetTenderEnquiryDocumentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetTenderEnquiryDocument, TenderEnquiryDocumentFile?> handler;

    public GetTenderEnquiryDocumentEndpoint(
        SignedInUserResolver users, IQueryHandler<GetTenderEnquiryDocument, TenderEnquiryDocumentFile?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetTenderEnquiryDocument))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tender-enquiries/{tenderEnquiryId}/document")] HttpRequest request,
        string tenderEnquiryId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TenderEnquiryRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var file = await handler.HandleAsync(new GetTenderEnquiryDocument(tenderEnquiryId), cancellationToken);
        if (file is null) return new NotFoundResult();
        return new FileContentResult(file.Content, file.ContentType) { FileDownloadName = file.FileName };
    }
}
