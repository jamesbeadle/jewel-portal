using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Registers.Policies;
using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Contracts.Registers;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Registers;

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
            document.Revision, entity.RecipientEmail, entity.RequestedAt, entity.SignedAt, entity.SignedName,
            entity.RecipientName, entity.CompanyName, entity.Position, entity.FormInviteId, entity.FormSubmissionId,
            document.FileBlobRef.Length > 0);

    public static PolicyDocument ToModel(this PolicyDocumentEntity entity, int signedCount, int outstandingCount) =>
        new(entity.PolicyDocumentId, entity.Title, entity.Summary, entity.Revision, entity.PublishedByEmail,
            entity.PublishedAt, entity.IsActive, signedCount, outstandingCount,
            PolicyDeclarations.Of(entity.Declaration), entity.FileName);
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
        services.AddScoped<ICommandHandler<ChasePolicySignOff, SentFormLink>, ChasePolicySignOffHandler>();
        services.AddScoped<ChasePolicySignOffAuthorisation>();
        services.AddScoped<ChasePolicySignOffValidation>();
        return services;
    }
}
