using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class UpdateTenderEnquiryDetailsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateTenderEnquiryDetailsAuthorisation authorisation;
    private readonly UpdateTenderEnquiryDetailsValidation validation;
    private readonly ICommandHandler<UpdateTenderEnquiryDetails, TenderEnquiry> handler;

    public UpdateTenderEnquiryDetailsEndpoint(
        SignedInUserResolver users, UpdateTenderEnquiryDetailsAuthorisation authorisation,
        UpdateTenderEnquiryDetailsValidation validation, ICommandHandler<UpdateTenderEnquiryDetails, TenderEnquiry> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateTenderEnquiryDetails))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "tender-enquiries/{tenderEnquiryId}/details")] HttpRequest request,
        string tenderEnquiryId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<UpdateTenderEnquiryDetails>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("The enquiry details are required.");
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
