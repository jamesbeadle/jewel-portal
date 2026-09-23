using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>
/// What came in: the Received list and each form (the form's store decides who may open it), the
/// people and companies it is filed under, and the emergency contacts — which the site manager and
/// the H&amp;S lead reach as well, since the point of them is being reached fast.
/// </summary>
public sealed class FormSubmissionReadEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly FormSubmissionAccess access;
    private readonly AuditActor auditActor;
    private readonly IQueryHandler<ListFormSubmissions, IReadOnlyList<FormSubmission>> list;
    private readonly IQueryHandler<OpenFormSubmission, FormSubmissionView> get;
    private readonly IQueryHandler<RevealHealthAnswers, IReadOnlyDictionary<string, string>> reveal;
    private readonly IQueryHandler<ListFormFolders, IReadOnlyList<FormFolder>> folders;
    private readonly IQueryHandler<ListEmergencyContacts, IReadOnlyList<EmergencyContactCard>> contacts;

    public FormSubmissionReadEndpoints(
        SignedInUserResolver users, FormSubmissionAccess access, AuditActor auditActor,
        IQueryHandler<ListFormSubmissions, IReadOnlyList<FormSubmission>> list,
        IQueryHandler<OpenFormSubmission, FormSubmissionView> get,
        IQueryHandler<RevealHealthAnswers, IReadOnlyDictionary<string, string>> reveal,
        IQueryHandler<ListFormFolders, IReadOnlyList<FormFolder>> folders,
        IQueryHandler<ListEmergencyContacts, IReadOnlyList<EmergencyContactCard>> contacts)
    {
        this.users = users;
        this.access = access;
        this.auditActor = auditActor;
        this.list = list;
        this.get = get;
        this.reveal = reveal;
        this.folders = folders;
        this.contacts = contacts;
    }

    [Function(nameof(ListFormSubmissions))]
    public async Task<IActionResult> List([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "form-submissions")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.Office.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var formFolderId = request.Query["folder"].ToString();
        var query = new ListFormSubmissions(string.IsNullOrWhiteSpace(formFolderId) ? null : formFolderId);
        return await OfficeAnswers.ReadAsync(list, query, request.HttpContext.RequestAborted);
    }

    [Function(nameof(OpenFormSubmission))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "form-submissions/{formSubmissionId}")] HttpRequest request,
        string formSubmissionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var mayRead = FormRoleSets.Office.IncludesAny(signedInUser.Roles) && await access.MayReadAsync(signedInUser, formSubmissionId, cancellationToken);
        if (!mayRead) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.ReadAsync(get, new OpenFormSubmission(formSubmissionId), cancellationToken);
    }

    [Function(nameof(RevealHealthAnswers))]
    public async Task<IActionResult> Reveal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "form-submissions/{formSubmissionId}/health")] HttpRequest request,
        string formSubmissionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.EmergencyContactReaders.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        auditActor.Email = signedInUser.Email;
        return await OfficeAnswers.ReadAsync(reveal, new RevealHealthAnswers(formSubmissionId), cancellationToken);
    }

    [Function(nameof(ListFormFolders))]
    public async Task<IActionResult> Folders([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "form-folders")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.Office.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.ReadAsync(folders, new ListFormFolders(), request.HttpContext.RequestAborted);
    }

    [Function(nameof(ListEmergencyContacts))]
    public async Task<IActionResult> EmergencyContacts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "emergency-contacts")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.EmergencyContactReaders.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.ReadAsync(contacts, new ListEmergencyContacts(), request.HttpContext.RequestAborted);
    }
}
