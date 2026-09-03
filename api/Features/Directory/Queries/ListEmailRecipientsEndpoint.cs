using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Queries;

public sealed class ListEmailRecipientsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListEmailRecipients, IReadOnlyList<EmailRecipient>> handler;

    public ListEmailRecipientsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListEmailRecipients, IReadOnlyList<EmailRecipient>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // The address book feeds the email composers, which are internal-only surfaces (every send
    // goes out from the projects mailbox). External logins never see the whole directory's contact
    // details — same principle as ListSubcontractors. Administrators pass via the resolver.
    private static readonly RoleSet RolesThatMayReadAddressBook = JpmsRoleSets.AllInternal;

    [Function(nameof(ListEmailRecipients))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "email-recipients")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadAddressBook.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var recipients = await handler.HandleAsync(new ListEmailRecipients(), request.HttpContext.RequestAborted);
        return new OkObjectResult(recipients);
    }
}
