using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Registers;

namespace Jewel.JPMS.Api.Features.Registers;

public sealed class ListPolicyDocumentsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListPolicyDocuments, IReadOnlyList<PolicyDocument>> handler;
    public ListPolicyDocumentsEndpoint(SignedInUserResolver users, IQueryHandler<ListPolicyDocuments, IReadOnlyList<PolicyDocument>> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListPolicyDocuments))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "registers/policies")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RegisterRoleSets.ManageRegisters.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListPolicyDocuments(), request.HttpContext.RequestAborted));
    }
}

public sealed class ListPolicyDocumentsHandler : IQueryHandler<ListPolicyDocuments, IReadOnlyList<PolicyDocument>>
{
    private readonly JpmsContext context;
    public ListPolicyDocumentsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<PolicyDocument>> HandleAsync(ListPolicyDocuments query, CancellationToken cancellationToken)
    {
        var documents = await context.PolicyDocuments
            .OrderByDescending(document => document.PublishedAt).ToListAsync(cancellationToken);
        var counts = await context.PolicySignOffs
            .GroupBy(row => row.PolicyDocumentId)
            .Select(group => new
            {
                PolicyDocumentId = group.Key,
                Signed = group.Count(row => row.SignedAt != null),
                Outstanding = group.Count(row => row.SignedAt == null),
            })
            .ToDictionaryAsync(group => group.PolicyDocumentId, cancellationToken);
        return documents.Select(document =>
        {
            counts.TryGetValue(document.PolicyDocumentId, out var count);
            return new PolicyDocument(document.PolicyDocumentId, document.Title, document.Summary,
                document.Revision, document.PublishedByEmail, document.PublishedAt, document.IsActive,
                count?.Signed ?? 0, count?.Outstanding ?? 0);
        }).ToList();
    }
}

public sealed class ListPolicySignOffsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListPolicySignOffs, IReadOnlyList<PolicySignOff>> handler;
    public ListPolicySignOffsEndpoint(SignedInUserResolver users, IQueryHandler<ListPolicySignOffs, IReadOnlyList<PolicySignOff>> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListPolicySignOffs))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "registers/policies/{policyDocumentId}/sign-offs")] HttpRequest request,
        string policyDocumentId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RegisterRoleSets.ManageRegisters.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListPolicySignOffs(policyDocumentId), request.HttpContext.RequestAborted));
    }
}

public sealed class ListPolicySignOffsHandler : IQueryHandler<ListPolicySignOffs, IReadOnlyList<PolicySignOff>>
{
    private readonly JpmsContext context;
    public ListPolicySignOffsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<PolicySignOff>> HandleAsync(ListPolicySignOffs query, CancellationToken cancellationToken)
    {
        var document = await context.PolicyDocuments.FindAsync(new object[] { query.PolicyDocumentId }, cancellationToken);
        if (document is null) return Array.Empty<PolicySignOff>();
        var rows = await context.PolicySignOffs
            .Where(row => row.PolicyDocumentId == query.PolicyDocumentId)
            .OrderBy(row => row.SignedAt != null).ThenBy(row => row.RecipientEmail)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel(document)).ToList();
    }
}

public sealed class PublishPolicyDocumentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PublishPolicyDocumentHandler handler;
    public PublishPolicyDocumentEndpoint(SignedInUserResolver users, PublishPolicyDocumentHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(PublishPolicyDocument))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "registers/policies")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RegisterRoleSets.ManageRegisters.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<PublishPolicyDocument>();
        if (command is null) return new BadRequestResult();
        if (string.IsNullOrWhiteSpace(command.Title))
            return new BadRequestObjectResult(new[] { "Give the document a title." });
        if (command.RecipientEmails is null || command.RecipientEmails.Count == 0)
            return new BadRequestObjectResult(new[] { "Name at least one recipient." });
        return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
    }
}

public sealed class PublishPolicyDocumentHandler : ICommandHandler<PublishPolicyDocument, PolicyDocument>
{
    private readonly JpmsContext context;
    public PublishPolicyDocumentHandler(JpmsContext context) { this.context = context; }

    public Task<PolicyDocument> HandleAsync(PublishPolicyDocument command, CancellationToken cancellationToken) =>
        HandleAsync(command, publishedByEmail: "", cancellationToken);

    public async Task<PolicyDocument> HandleAsync(PublishPolicyDocument command, string publishedByEmail, CancellationToken cancellationToken)
    {
        // A new revision of an existing title supersedes it: the old row stays (evidence), goes
        // inactive, and everyone signs the new revision afresh.
        var previous = await context.PolicyDocuments
            .Where(document => document.Title == command.Title.Trim() && document.IsActive)
            .OrderByDescending(document => document.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        if (previous is not null) previous.IsActive = false;

        var entity = new PolicyDocumentEntity
        {
            PolicyDocumentId = RegisterIdentifierFactory.NextPolicyDocumentId(),
            Title = command.Title.Trim(),
            Summary = command.Summary ?? "",
            Revision = (previous?.Revision ?? 0) + 1,
            PublishedByEmail = publishedByEmail,
            PublishedAt = DateTimeOffset.UtcNow,
        };
        context.PolicyDocuments.Add(entity);
        foreach (var email in command.RecipientEmails
            .Select(email => email.Trim().ToLowerInvariant())
            .Where(email => email != "").Distinct())
        {
            context.PolicySignOffs.Add(new PolicySignOffEntity
            {
                PolicySignOffId = RegisterIdentifierFactory.NextPolicySignOffId(),
                PolicyDocumentId = entity.PolicyDocumentId,
                RecipientEmail = email,
                RequestedAt = DateTimeOffset.UtcNow,
            });
        }
        await context.SaveChangesAsync(cancellationToken);
        var outstanding = await context.PolicySignOffs
            .CountAsync(row => row.PolicyDocumentId == entity.PolicyDocumentId, cancellationToken);
        return new PolicyDocument(entity.PolicyDocumentId, entity.Title, entity.Summary, entity.Revision,
            entity.PublishedByEmail, entity.PublishedAt, entity.IsActive, 0, outstanding);
    }
}
