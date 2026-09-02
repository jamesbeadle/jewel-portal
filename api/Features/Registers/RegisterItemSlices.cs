using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Registers;

namespace Jewel.JPMS.Api.Features.Registers;

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
