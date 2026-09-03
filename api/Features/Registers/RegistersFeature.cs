using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Registers;
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
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing, JpmsRoles.OfficeComplianceCoordinator);
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
