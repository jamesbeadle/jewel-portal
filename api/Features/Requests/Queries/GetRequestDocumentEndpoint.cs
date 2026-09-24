using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class GetRequestDocumentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetRequestDocument, RequestDocumentFile?> handler;

    public GetRequestDocumentEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetRequestDocument, RequestDocumentFile?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Internal only: a request carries the business's notes and mail (2026-09-24).
    private static readonly RoleSet RolesThatMayReadRequests = JpmsRoleSets.ProjectDeliveryTeam;

    [Function(nameof(GetRequestDocument))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/document")] HttpRequest request,
        string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequests.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var file = await handler.HandleAsync(new GetRequestDocument(requestId), request.HttpContext.RequestAborted);
        if (file is null) return new NotFoundResult();

        // Streams the PDF with a friendly download name (e.g. "REQ-0001 - RFI.pdf").
        return new FileContentResult(file.Content, file.ContentType) { FileDownloadName = file.FileName };
    }
}
