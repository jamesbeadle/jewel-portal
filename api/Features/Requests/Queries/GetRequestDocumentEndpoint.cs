using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Api.Features.Parties;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class GetRequestDocumentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IQueryHandler<GetRequestDocument, RequestDocumentFile?> handler;

    public GetRequestDocumentEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetRequestDocument, RequestDocumentFile?> handler,
        JpmsContext context)
    {
        this.context = context;
        this.users = users;
        this.handler = handler;
    }

    // The delivery team, and the project's client and architect on their own projects with the
    // internal parts stripped (Parties/PartyReads).
    private static readonly RoleSet RolesThatMayReadRequests = JpmsRoleSets.DeliveryTeamAndParties;

    [Function(nameof(GetRequestDocument))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/document")] HttpRequest request,
        string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequests.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!await PartyReads.MayReadRequestAsync(context, signedInUser, requestId, request.HttpContext.RequestAborted)) return new NotFoundResult();

        var file = await handler.HandleAsync(new GetRequestDocument(requestId), request.HttpContext.RequestAborted);
        if (file is null) return new NotFoundResult();

        // Streams the PDF with a friendly download name (e.g. "REQ-0001 - RFI.pdf").
        return new FileContentResult(file.Content, file.ContentType) { FileDownloadName = file.FileName };
    }
}
