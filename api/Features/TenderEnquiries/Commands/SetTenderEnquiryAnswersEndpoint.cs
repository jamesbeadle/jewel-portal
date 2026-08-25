using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class SetTenderEnquiryAnswersEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetTenderEnquiryAnswersAuthorisation authorisation;
    private readonly SetTenderEnquiryAnswersValidation validation;
    private readonly ICommandHandler<SetTenderEnquiryAnswers, IReadOnlyList<TenderEnquiryAnswer>> handler;

    public SetTenderEnquiryAnswersEndpoint(
        SignedInUserResolver users, SetTenderEnquiryAnswersAuthorisation authorisation,
        SetTenderEnquiryAnswersValidation validation,
        ICommandHandler<SetTenderEnquiryAnswers, IReadOnlyList<TenderEnquiryAnswer>> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetTenderEnquiryAnswers))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "tender-enquiries/{tenderEnquiryId}/answers")] HttpRequest request,
        string tenderEnquiryId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<SetTenderEnquiryAnswers>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("The answers are required.");
        var command = posted with { TenderEnquiryId = tenderEnquiryId };

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
