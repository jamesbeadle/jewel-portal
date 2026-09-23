using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Acceptance;

/// <summary>
/// The public acceptance page's API — no sign-in, no SignedInUserResolver: the order's token in
/// the route is the whole authorisation (see WorkOrderAcceptanceService). GET shows the purchase
/// order; POST records the acceptance under the name the contact typed. An unknown token is a 404
/// with no detail, a refusal a 400 with the sentence the page shows.
/// </summary>
public sealed class WorkOrderAcceptanceEndpoints
{
    private const string BodyUnreadable = "Something went wrong — please try again.";
    private const string LinkInvalid = "This link isn't valid.";

    private readonly WorkOrderAcceptanceService service;

    public WorkOrderAcceptanceEndpoints(WorkOrderAcceptanceService service)
    {
        this.service = service;
    }

    [Function("WorkOrderAcceptanceGet")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "work-orders/accept/{token}")] HttpRequest request, string token)
    {
        var view = await service.ViewAsync(token, request.HttpContext.RequestAborted);
        return view is null ? NotFound() : new OkObjectResult(view);
    }

    [Function("WorkOrderAcceptanceAccept")]
    public async Task<IActionResult> Accept(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "work-orders/accept/{token}")] HttpRequest request, string token)
    {
        var signature = await ReadBody(request);
        if (signature is null) return new BadRequestObjectResult(BodyUnreadable);
        try
        {
            var view = await service.AcceptAsync(token, signature, request.HttpContext.RequestAborted);
            return view is null ? NotFound() : new OkObjectResult(view);
        }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }

    private static IActionResult NotFound() => new NotFoundObjectResult(LinkInvalid);

    private static async Task<WorkOrderAcceptanceSignature?> ReadBody(HttpRequest request)
    {
        try { return await request.ReadFromJsonAsync<WorkOrderAcceptanceSignature>(); }
        catch { return null; }
    }
}
