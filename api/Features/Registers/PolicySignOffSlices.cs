using Jewel.JPMS.Contracts.Registers;

namespace Jewel.JPMS.Api.Features.Registers;

public sealed class ListMyPolicySignOffsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ListMyPolicySignOffsHandler handler;
    public ListMyPolicySignOffsEndpoint(SignedInUserResolver users, ListMyPolicySignOffsHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListMyPolicySignOffs))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "my/policy-sign-offs")] HttpRequest request)
    {
        // Any signed-in user: this is their own list, resolved by their own email.
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(signedInUser.Email, request.HttpContext.RequestAborted));
    }
}

public sealed class ListMyPolicySignOffsHandler
{
    private readonly JpmsContext context;
    public ListMyPolicySignOffsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<PolicySignOff>> HandleAsync(string email, CancellationToken cancellationToken)
    {
        var normalised = email.Trim().ToLowerInvariant();
        var rows = await context.PolicySignOffs
            .Where(row => row.RecipientEmail == normalised)
            .OrderBy(row => row.SignedAt != null).ThenByDescending(row => row.RequestedAt)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0) return Array.Empty<PolicySignOff>();
        var documentIds = rows.Select(row => row.PolicyDocumentId).Distinct().ToList();
        var documents = await context.PolicyDocuments
            .Where(document => documentIds.Contains(document.PolicyDocumentId))
            .ToDictionaryAsync(document => document.PolicyDocumentId, cancellationToken);
        return rows
            .Where(row => documents.ContainsKey(row.PolicyDocumentId))
            .Select(row => row.ToModel(documents[row.PolicyDocumentId]))
            .ToList();
    }
}

public sealed class SignPolicyEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SignPolicyHandler handler;
    public SignPolicyEndpoint(SignedInUserResolver users, SignPolicyHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SignPolicy))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "my/policy-sign-offs/sign")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<SignPolicy>();
        if (command is null) return new BadRequestResult();
        if (string.IsNullOrWhiteSpace(command.TypedName))
            return new BadRequestObjectResult(new[] { "Type your name to sign — that is the record." });
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException refusal)
        {
            return new BadRequestObjectResult(new[] { refusal.Message });
        }
    }
}

public sealed class SignPolicyHandler
{
    private readonly JpmsContext context;
    public SignPolicyHandler(JpmsContext context) { this.context = context; }

    public async Task<PolicySignOff> HandleAsync(SignPolicy command, string email, CancellationToken cancellationToken)
    {
        var row = await context.PolicySignOffs.FindAsync(new object[] { command.PolicySignOffId }, cancellationToken);
        if (row is null) throw new InvalidOperationException("That sign-off no longer exists.");
        if (!string.Equals(row.RecipientEmail, email.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This acknowledgement belongs to a different user.");
        var document = await context.PolicyDocuments.FindAsync(new object[] { row.PolicyDocumentId }, cancellationToken)
            ?? throw new InvalidOperationException("The document behind this sign-off no longer exists.");
        if (row.SignedAt is null)
        {
            row.SignedAt = DateTimeOffset.UtcNow;
            row.SignedName = command.TypedName.Trim();
            await context.SaveChangesAsync(cancellationToken);
        }
        return row.ToModel(document);
    }
}
