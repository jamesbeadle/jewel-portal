namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>
/// The public imagine page's API — no sign-in, no SignedInUserResolver: the lead's token in the
/// route is the whole authorisation (see ImaginePublicService). An unknown token is a 404 with no
/// detail; a refusal is a 400 with the sentence the page shows. Images stream through here, keyed
/// by the same token, so the blob container never needs to be public.
/// </summary>
public sealed class ImaginePublicEndpoints
{
    private readonly ImaginePublicService service;

    public ImaginePublicEndpoints(ImaginePublicService service)
    {
        this.service = service;
    }

    [Function("ImagineGet")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "imagine/{token}")] HttpRequest request, string token)
    {
        var view = await service.GetAsync(token, request.HttpContext.RequestAborted);
        return view is null ? NotFound() : NoStore(new OkObjectResult(view));
    }

    [Function("ImagineSubmit")]
    public async Task<IActionResult> Submit(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "imagine/{token}/submit")] HttpRequest request, string token)
    {
        var submission = await ReadBody<ImagineSubmission>(request);
        if (submission is null) return new BadRequestObjectResult("Something went wrong reading your photos — please try again.");
        return await Run(() => service.SubmitAsync(token, submission, ImaginePublicService.ClientHash(request), request.HttpContext.RequestAborted));
    }

    [Function("ImagineRevise")]
    public async Task<IActionResult> Revise(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "imagine/{token}/revise")] HttpRequest request, string token)
    {
        var revision = await ReadBody<ImagineRevisionRequest>(request);
        if (revision is null) return new BadRequestObjectResult("Something went wrong — please try again.");
        return await Run(() => service.ReviseAsync(token, revision, ImaginePublicService.ClientHash(request), request.HttpContext.RequestAborted));
    }

    [Function("ImagineReact")]
    public async Task<IActionResult> React(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "imagine/{token}/react")] HttpRequest request, string token)
    {
        var reaction = await ReadBody<ImagineReaction>(request);
        if (reaction is null) return new BadRequestObjectResult("Something went wrong — please try again.");
        return await Run(() => service.ReactAsync(token, reaction, request.HttpContext.RequestAborted));
    }

    [Function("ImagineAcceptProposal")]
    public async Task<IActionResult> AcceptProposal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "imagine/{token}/proposal/accept")] HttpRequest request, string token)
    {
        var acceptance = await ReadBody<ProposalAcceptance>(request);
        if (acceptance is null) return new BadRequestObjectResult("Something went wrong — please try again.");
        return await Run(() => service.AcceptProposalAsync(token, acceptance, ImaginePublicService.ClientHash(request), request.HttpContext.RequestAborted));
    }

    [Function("ImagineDeclineProposal")]
    public async Task<IActionResult> DeclineProposal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "imagine/{token}/proposal/decline")] HttpRequest request, string token)
    {
        var decline = await ReadBody<ProposalDecline>(request);
        if (decline is null) return new BadRequestObjectResult("Something went wrong — please try again.");
        return await Run(() => service.DeclineProposalAsync(token, decline, request.HttpContext.RequestAborted));
    }

    [Function("ImagineImage")]
    public async Task<IActionResult> Image(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "imagine/{token}/images/{imageId}")] HttpRequest request, string token, string imageId)
    {
        var blob = await service.OpenImageAsync(token, imageId, request.HttpContext.RequestAborted);
        if (blob is null) return NotFound();
        request.HttpContext.Response.Headers["Cache-Control"] = "private, max-age=86400";
        return new FileStreamResult(blob.Content, blob.ContentType);
    }

    private async Task<IActionResult> Run(Func<Task<ImagineView>> handle)
    {
        try { return NoStore(new OkObjectResult(await handle())); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }

    private static IActionResult NotFound() => new NotFoundObjectResult("This link isn't valid.");

    private static IActionResult NoStore(IActionResult result) => result;

    private static async Task<T?> ReadBody<T>(HttpRequest request) where T : class
    {
        try { return await request.ReadFromJsonAsync<T>(); }
        catch { return null; }
    }
}
