using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

/// <summary>
/// GET /api/variation-orders/{voId}/document — the variation order's official PDF, rendered fresh
/// from the record on every call (nothing is stored). Mirrors the request document endpoint.
/// </summary>
public sealed class GetVariationOrderDocumentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetVariationOrderDocument, VariationDocumentFile?> handler;

    public GetVariationOrderDocumentEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetVariationOrderDocument, VariationDocumentFile?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // The same read set as the variation record itself: internal plus the architect, who reads and
    // approves variations per the permissions matrix (see GetVariationOrderByIdEndpoint).
    private static readonly RoleSet RolesThatMayReadVariations = JpmsRoleSets.InternalAndArchitect;

    [Function(nameof(GetVariationOrderDocument))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "variation-orders/{voId}/document")] HttpRequest request,
        string voId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadVariations.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var file = await handler.HandleAsync(new GetVariationOrderDocument(voId), request.HttpContext.RequestAborted);
        if (file is null) return new NotFoundResult();

        // Streams the PDF with a friendly download name (e.g. "V31 - Staircase Enclosure Ply.pdf").
        return new FileContentResult(file.Content, file.ContentType) { FileDownloadName = file.FileName };
    }
}
