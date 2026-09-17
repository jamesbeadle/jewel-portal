using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// The status move every route that emails a request's official document makes: emailing it means
/// it is going out, so a Needs-action request becomes Open — with the correspondent, awaiting their
/// response. A request already past Needs action keeps its status, because re-sending a document
/// never rewinds a lifecycle, and the team sets a cancelled send back by hand.
/// </summary>
public static class RequestLifecycle
{
    public static async Task OpenIfNeedsActionAsync(
        JpmsContext context, RequestEntity request, CancellationToken cancellationToken)
    {
        if ((RequestStatus)request.Status != RequestStatus.NeedsAction) return;
        request.Status = (int)RequestStatus.Open;
        await context.SaveChangesAsync(cancellationToken);
    }
}
