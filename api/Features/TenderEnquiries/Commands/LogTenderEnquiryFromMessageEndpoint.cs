using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class LogTenderEnquiryFromMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly LogTenderEnquiryFromMessageAuthorisation authorisation;
    private readonly LogTenderEnquiryFromMessageValidation validation;
    private readonly ICommandHandler<LogTenderEnquiryFromMessage, TenderEnquiry> handler;

    public LogTenderEnquiryFromMessageEndpoint(
        SignedInUserResolver users, AuditActor auditActor,
        LogTenderEnquiryFromMessageAuthorisation authorisation, LogTenderEnquiryFromMessageValidation validation,
        ICommandHandler<LogTenderEnquiryFromMessage, TenderEnquiry> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(LogTenderEnquiryFromMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/log-tender-enquiry")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<LogTenderEnquiryFromMessage>(cancellationToken);
        if (posted is null || string.IsNullOrWhiteSpace(posted.MessageId))
            return new BadRequestObjectResult("messageId is required.");

        // The logger is always the signed-in user — never trusted from the client body.
        var command = posted with { LoggedByEmail = signedInUser.Email };
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
            // Guards (cross-pathway confirm, vanished attachment, missing project) are answers
            // the triager acts on — a bodiless 500 would hide them.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
