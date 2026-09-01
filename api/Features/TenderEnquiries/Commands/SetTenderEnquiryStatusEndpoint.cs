using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class SetTenderEnquiryStatusEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly SetTenderEnquiryStatusAuthorisation authorisation;
    private readonly SetTenderEnquiryStatusValidation validation;
    private readonly ICommandHandler<SetTenderEnquiryStatus, TenderEnquiry> handler;

    public SetTenderEnquiryStatusEndpoint(
        SignedInUserResolver users, AuditActor auditActor,
        SetTenderEnquiryStatusAuthorisation authorisation, SetTenderEnquiryStatusValidation validation,
        ICommandHandler<SetTenderEnquiryStatus, TenderEnquiry> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetTenderEnquiryStatus))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tender-enquiries/{tenderEnquiryId}/status")] HttpRequest request,
        string tenderEnquiryId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<SetTenderEnquiryStatus>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A status is required.");
        var command = posted with { TenderEnquiryId = tenderEnquiryId, ChangedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
