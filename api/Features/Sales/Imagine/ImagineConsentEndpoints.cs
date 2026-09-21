namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>
/// The prospect's own door to withdraw the "keep in touch" tick — on the same page, keyed by the
/// same token as everything else there, so stopping is one press rather than an email to sales.
/// </summary>
public sealed class ImagineConsentEndpoints
{
    private readonly ImaginePublicService service;

    public ImagineConsentEndpoints(ImaginePublicService service)
    {
        this.service = service;
    }

    [Function("ImagineStopKeepingInTouch")]
    public async Task<IActionResult> StopKeepingInTouch(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "imagine/{token}/keep-in-touch/stop")] HttpRequest request, string token)
    {
        try { return new OkObjectResult(await service.StopKeepingInTouchAsync(token, request.HttpContext.RequestAborted)); }
        catch (InvalidOperationException refusal) { return new BadRequestObjectResult(refusal.Message); }
    }
}
