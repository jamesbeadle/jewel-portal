using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Queries;

/// <summary>The enquiry reads — list per project, one by id, its answers. Internal-only: an
/// architect's own portal has no business in Jewel's bid pipeline.</summary>
public sealed class TenderEnquiryQueryEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTenderEnquiries, IReadOnlyList<TenderEnquiry>> listAll;
    private readonly IQueryHandler<ListTenderEnquiriesForProject, IReadOnlyList<TenderEnquiry>> list;
    private readonly IQueryHandler<GetTenderEnquiryById, TenderEnquiry?> get;
    private readonly IQueryHandler<ListTenderEnquiryAnswers, IReadOnlyList<TenderEnquiryAnswer>> answers;

    public TenderEnquiryQueryEndpoints(
        SignedInUserResolver users,
        IQueryHandler<ListTenderEnquiries, IReadOnlyList<TenderEnquiry>> listAll,
        IQueryHandler<ListTenderEnquiriesForProject, IReadOnlyList<TenderEnquiry>> list,
        IQueryHandler<GetTenderEnquiryById, TenderEnquiry?> get,
        IQueryHandler<ListTenderEnquiryAnswers, IReadOnlyList<TenderEnquiryAnswer>> answers)
    {
        this.users = users;
        this.listAll = listAll;
        this.list = list;
        this.get = get;
        this.answers = answers;
    }

    [Function(nameof(ListTenderEnquiries))]
    public async Task<IActionResult> ListAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tender-enquiries")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var refusal = await RefusalFor(request, cancellationToken);
        if (refusal is not null) return refusal;
        return new OkObjectResult(await listAll.HandleAsync(new ListTenderEnquiries(), cancellationToken));
    }

    [Function(nameof(ListTenderEnquiriesForProject))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/tender-enquiries")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var refusal = await RefusalFor(request, cancellationToken);
        if (refusal is not null) return refusal;
        return new OkObjectResult(await list.HandleAsync(new ListTenderEnquiriesForProject(projectId), cancellationToken));
    }

    [Function(nameof(GetTenderEnquiryById))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tender-enquiries/{tenderEnquiryId}")] HttpRequest request,
        string tenderEnquiryId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var refusal = await RefusalFor(request, cancellationToken);
        if (refusal is not null) return refusal;
        // A missing id answers null (204 → a null model client-side), like every other by-id read.
        return new OkObjectResult(await get.HandleAsync(new GetTenderEnquiryById(tenderEnquiryId), cancellationToken));
    }

    [Function(nameof(ListTenderEnquiryAnswers))]
    public async Task<IActionResult> Answers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tender-enquiries/{tenderEnquiryId}/answers")] HttpRequest request,
        string tenderEnquiryId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var refusal = await RefusalFor(request, cancellationToken);
        if (refusal is not null) return refusal;
        return new OkObjectResult(await answers.HandleAsync(new ListTenderEnquiryAnswers(tenderEnquiryId), cancellationToken));
    }

    // 401 when nobody is signed in, 403 when they are but may not read; null when the read may go ahead.
    private async Task<IActionResult?> RefusalFor(HttpRequest request, CancellationToken cancellationToken)
    {
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TenderEnquiryRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return null;
    }
}
