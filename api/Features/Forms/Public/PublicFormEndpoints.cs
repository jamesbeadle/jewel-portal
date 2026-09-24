using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Public;

/// <summary>
/// The public forms' API — no sign-in, no SignedInUserResolver. A form's address is public, as it was
/// on the dashboard, and a one-time link is its own authorisation. An unknown form is a 404 with no
/// detail; a refusal is a 400 carrying the sentence the page shows, and any other failure a 400 with
/// the page's "try again", its reason kept for the log.
/// </summary>
public sealed class PublicFormEndpoints
{
    private readonly PublicFormService forms;
    private readonly ILogger<PublicFormEndpoints> logger;

    public PublicFormEndpoints(PublicFormService forms, ILogger<PublicFormEndpoints> logger)
    {
        this.forms = forms;
        this.logger = logger;
    }

    [Function("PublicFormOpen")]
    public async Task<IActionResult> Open(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public-forms/{slug}")] HttpRequest request,
        string slug)
    {
        var inviteToken = request.Query["k"].ToString();
        var packToken = request.Query["p"].ToString();
        var view = await forms.OpenAsync(slug, inviteToken, packToken, request.HttpContext.RequestAborted);
        return view is null ? new NotFoundObjectResult(FormSheetWording.NotAvailable) : new OkObjectResult(view);
    }

    [Function("PublicFormPolicyFile")]
    public async Task<IActionResult> PolicyFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public-forms/{slug}/policy-file")] HttpRequest request,
        string slug)
    {
        var inviteToken = request.Query["k"].ToString();
        var packToken = request.Query["p"].ToString();
        var file = await forms.OpenPolicyFileAsync(slug, inviteToken, packToken, request.HttpContext.RequestAborted);
        return file is null ? new NotFoundResult() : new FileStreamResult(file.Content, file.ContentType);
    }

    [Function("PublicFormPack")]
    public async Task<IActionResult> Pack(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public-form-packs/{token}")] HttpRequest request,
        string token) =>
        new OkObjectResult(await forms.OpenPackAsync(token, request.HttpContext.RequestAborted));

    [Function("PublicFormUpload")]
    public async Task<IActionResult> Upload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "public-forms/{slug}/uploads")] HttpRequest request,
        string slug)
    {
        var upload = await ReadBody<PublicFormUpload>(request);
        if (upload is null) return new BadRequestObjectResult("Bad file data.");
        var clientHash = ClientHashOf(request);
        return await Answer(() => forms.UploadAsync(slug, upload, clientHash, request.HttpContext.RequestAborted));
    }

    [Function("PublicFormSubmit")]
    public async Task<IActionResult> Submit(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "public-forms/{slug}/submit")] HttpRequest request,
        string slug)
    {
        var submission = await ReadBody<PublicFormSubmission>(request);
        if (submission?.Answers is null || submission.Uploads is null) return new BadRequestObjectResult("Could not read the form - try again.");
        var clientHash = ClientHashOf(request);
        return await Answer(() => forms.SubmitAsync(slug, submission, clientHash, request.HttpContext.RequestAborted));
    }

    private async Task<IActionResult> Answer<TResult>(Func<Task<TResult>> handle)
    {
        try { return new OkObjectResult(await handle()); }
        catch (PublicFormRefusal refusal) { return new BadRequestObjectResult(refusal.Message); }
        catch (InvalidOperationException failure)
        {
            logger.LogWarning(failure, "Forms: a public form request failed.");
            return new BadRequestObjectResult(FormSheetWording.CouldNotSend);
        }
    }

    private static string ClientHashOf(HttpRequest request) => FormTokens.Hash(ClientKey.Of(request));

    private static async Task<TBody?> ReadBody<TBody>(HttpRequest request) where TBody : class
    {
        try { return await request.ReadFromJsonAsync<TBody>(); }
        catch (Exception unreadable) when (unreadable is JsonException or InvalidOperationException or NotSupportedException) { return null; }
    }
}
