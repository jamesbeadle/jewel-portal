using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Registers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Registers;

/// <summary>
/// The Monday replacement (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md §8): company
/// registers (insurances, subscriptions, vans, trade accounts) and staff sign-off forms.
/// Register admin and policy publishing sit with the office/director roles; signing is every
/// user's own surface, resolved by their signed-in email — no impersonation.
/// </summary>
internal static class RegisterRoleSets
{
    public static readonly RoleSet ManageRegisters = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
        JpmsRoles.OfficeAdmin, JpmsRoles.OfficeComplianceCoordinator);
}

internal static class RegisterIdentifierFactory
{
    private const string CompactGuidFormat = "N";
    public static string NextRegisterItemId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextPolicyDocumentId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextPolicySignOffId() => Guid.NewGuid().ToString(CompactGuidFormat);
}

internal static class RegisterMapping
{
    public static RegisterItem ToModel(this CompanyRegisterItemEntity entity) =>
        new(entity.RegisterItemId, (RegisterKind)entity.Kind, entity.Name, entity.Counterparty,
            entity.Reference, entity.OwnerEmail, entity.Cost, entity.BillingCycle,
            entity.KeyDate, entity.SecondaryDate, entity.Notes, entity.IsActive);

    public static PolicySignOff ToModel(this PolicySignOffEntity entity, PolicyDocumentEntity document) =>
        new(entity.PolicySignOffId, entity.PolicyDocumentId, document.Title, document.Summary,
            document.Revision, entity.RecipientEmail, entity.RequestedAt, entity.SignedAt, entity.SignedName);
}

public static class RegistersFeatureRegistration
{
    public static IServiceCollection AddRegistersFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListRegisterItems, IReadOnlyList<RegisterItem>>, ListRegisterItemsHandler>();
        services.AddScoped<SaveRegisterItemHandler>();
        services.AddScoped<ICommandHandler<SaveRegisterItem, RegisterItem>>(
            provider => provider.GetRequiredService<SaveRegisterItemHandler>());
        services.AddScoped<ICommandHandler<DeactivateRegisterItem, Acknowledgement>, DeactivateRegisterItemHandler>();
        services.AddScoped<IQueryHandler<ListPolicyDocuments, IReadOnlyList<PolicyDocument>>, ListPolicyDocumentsHandler>();
        services.AddScoped<IQueryHandler<ListPolicySignOffs, IReadOnlyList<PolicySignOff>>, ListPolicySignOffsHandler>();
        services.AddScoped<PublishPolicyDocumentHandler>();
        services.AddScoped<ICommandHandler<PublishPolicyDocument, PolicyDocument>>(
            provider => provider.GetRequiredService<PublishPolicyDocumentHandler>());
        services.AddScoped<ListMyPolicySignOffsHandler>();
        services.AddScoped<SignPolicyHandler>();
        return services;
    }
}

// ---- Registers ------------------------------------------------------------------------------

public sealed class ListRegisterItemsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRegisterItems, IReadOnlyList<RegisterItem>> handler;
    public ListRegisterItemsEndpoint(SignedInUserResolver users, IQueryHandler<ListRegisterItems, IReadOnlyList<RegisterItem>> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListRegisterItems))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "registers/items")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RegisterRoleSets.ManageRegisters.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListRegisterItems(), request.HttpContext.RequestAborted));
    }
}

public sealed class ListRegisterItemsHandler : IQueryHandler<ListRegisterItems, IReadOnlyList<RegisterItem>>
{
    private readonly JpmsContext context;
    public ListRegisterItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<RegisterItem>> HandleAsync(ListRegisterItems query, CancellationToken cancellationToken)
    {
        var items = await context.CompanyRegisterItems
            .OrderByDescending(item => item.IsActive).ThenBy(item => item.Kind).ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return items.Select(item => item.ToModel()).ToList();
    }
}

public sealed class SaveRegisterItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SaveRegisterItemHandler handler;
    public SaveRegisterItemEndpoint(SignedInUserResolver users, SaveRegisterItemHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SaveRegisterItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "registers/items")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RegisterRoleSets.ManageRegisters.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<SaveRegisterItem>();
        if (command?.Item is null) return new BadRequestResult();
        if (string.IsNullOrWhiteSpace(command.Item.Name))
            return new BadRequestObjectResult(new[] { "Give the item a name." });
        return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
    }
}

public sealed class SaveRegisterItemHandler : ICommandHandler<SaveRegisterItem, RegisterItem>
{
    private readonly JpmsContext context;
    public SaveRegisterItemHandler(JpmsContext context) { this.context = context; }

    public Task<RegisterItem> HandleAsync(SaveRegisterItem command, CancellationToken cancellationToken) =>
        HandleAsync(command, createdByEmail: "", cancellationToken);

    public async Task<RegisterItem> HandleAsync(SaveRegisterItem command, string createdByEmail, CancellationToken cancellationToken)
    {
        var item = command.Item;
        var entity = string.IsNullOrEmpty(item.RegisterItemId)
            ? null
            : await context.CompanyRegisterItems.FindAsync(new object[] { item.RegisterItemId }, cancellationToken);
        if (entity is null)
        {
            entity = new CompanyRegisterItemEntity
            {
                RegisterItemId = RegisterIdentifierFactory.NextRegisterItemId(),
                CreatedByEmail = createdByEmail,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            context.CompanyRegisterItems.Add(entity);
        }
        entity.Kind = (int)item.Kind;
        entity.Name = item.Name.Trim();
        entity.Counterparty = item.Counterparty ?? "";
        entity.Reference = item.Reference ?? "";
        entity.OwnerEmail = item.OwnerEmail ?? "";
        entity.Cost = item.Cost;
        entity.BillingCycle = item.BillingCycle ?? "";
        entity.KeyDate = item.KeyDate;
        entity.SecondaryDate = item.SecondaryDate;
        entity.Notes = item.Notes ?? "";
        entity.IsActive = item.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class DeactivateRegisterItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<DeactivateRegisterItem, Acknowledgement> handler;
    public DeactivateRegisterItemEndpoint(SignedInUserResolver users, ICommandHandler<DeactivateRegisterItem, Acknowledgement> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(DeactivateRegisterItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "registers/items/deactivate")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RegisterRoleSets.ManageRegisters.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<DeactivateRegisterItem>();
        if (command is null) return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

public sealed class DeactivateRegisterItemHandler : ICommandHandler<DeactivateRegisterItem, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeactivateRegisterItemHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeactivateRegisterItem command, CancellationToken cancellationToken)
    {
        var entity = await context.CompanyRegisterItems.FindAsync(new object[] { command.RegisterItemId }, cancellationToken);
        if (entity is not null)
        {
            entity.IsActive = false;
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.RegisterItemId);
    }
}

// ---- Policies & sign-off --------------------------------------------------------------------

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
