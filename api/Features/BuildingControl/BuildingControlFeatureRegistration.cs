using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.BuildingControl.Attachments;
using Jewel.JPMS.Api.Features.BuildingControl.Commands;
using Jewel.JPMS.Api.Features.BuildingControl.Queries;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.BuildingControl;

public static class BuildingControlFeatureRegistration
{
    public static IServiceCollection AddBuildingControlFeature(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterAttachmentStore(services, configuration);

        // The piece the tab's Add inspection and the triage create share: the numbered stage row.
        services.AddScoped<BuildingControlInspectionRegister>();
        services.AddScoped<BuildingControlAttachmentWriter>();

        services.AddScoped<ICommandHandler<CreateBuildingControlCase, BuildingControlCase>, CreateBuildingControlCaseHandler>();
        services.AddScoped<CreateBuildingControlCaseAuthorisation>();
        services.AddScoped<CreateBuildingControlCaseValidation>();

        services.AddScoped<ICommandHandler<UpdateBuildingControlCase, BuildingControlCase>, UpdateBuildingControlCaseHandler>();
        services.AddScoped<UpdateBuildingControlCaseAuthorisation>();
        services.AddScoped<UpdateBuildingControlCaseValidation>();

        services.AddScoped<ICommandHandler<SetBuildingControlCaseStatus, BuildingControlCase>, SetBuildingControlCaseStatusHandler>();
        services.AddScoped<SetBuildingControlCaseStatusAuthorisation>();

        services.AddScoped<ICommandHandler<AddBuildingControlInspection, BuildingControlInspection>, AddBuildingControlInspectionHandler>();
        services.AddScoped<AddBuildingControlInspectionAuthorisation>();
        services.AddScoped<AddBuildingControlInspectionValidation>();

        services.AddScoped<ICommandHandler<UpdateBuildingControlInspection, BuildingControlInspection>, UpdateBuildingControlInspectionHandler>();
        services.AddScoped<UpdateBuildingControlInspectionAuthorisation>();
        services.AddScoped<UpdateBuildingControlInspectionValidation>();

        services.AddScoped<ICommandHandler<SetBuildingControlInspectionStatus, BuildingControlInspection>, SetBuildingControlInspectionStatusHandler>();
        services.AddScoped<SetBuildingControlInspectionStatusAuthorisation>();

        services.AddScoped<ICommandHandler<DeleteBuildingControlInspection, Acknowledgement>, DeleteBuildingControlInspectionHandler>();
        services.AddScoped<DeleteBuildingControlInspectionAuthorisation>();

        // The Control Centre's "create new → Building Control Inspection": raise + link the
        // inspector's email.
        services.AddScoped<ICommandHandler<CreateBuildingControlInspectionFromMessage, BuildingControlInspection>, CreateBuildingControlInspectionFromMessageHandler>();
        services.AddScoped<CreateBuildingControlInspectionFromMessageAuthorisation>();
        services.AddScoped<CreateBuildingControlInspectionFromMessageValidation>();

        services.AddScoped<ICommandHandler<SetBuildingControlAttachmentKind, BuildingControlAttachment>, SetBuildingControlAttachmentKindHandler>();
        services.AddScoped<SetBuildingControlAttachmentKindAuthorisation>();

        services.AddScoped<ICommandHandler<RemoveBuildingControlAttachment, Acknowledgement>, RemoveBuildingControlAttachmentHandler>();
        services.AddScoped<RemoveBuildingControlAttachmentAuthorisation>();

        services.AddScoped<ICommandHandler<CopyEmailAttachmentsToBuildingControlInspection, IReadOnlyList<BuildingControlAttachment>>, CopyEmailAttachmentsToBuildingControlInspectionHandler>();
        services.AddScoped<CopyEmailAttachmentsToBuildingControlInspectionAuthorisation>();

        services.AddScoped<IQueryHandler<GetBuildingControlForProject, BuildingControlProjectView>, GetBuildingControlForProjectHandler>();

        return services;
    }

    // Same connection chain as the other document stores, with its own key first so building
    // control files can be split onto their own account if volume ever warrants it.
    private static void RegisterAttachmentStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["BuildingControlAttachmentsStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IBuildingControlAttachmentStore, NullBuildingControlAttachmentStore>();
            return;
        }
        services.AddSingleton<IBuildingControlAttachmentStore>(_ => new AzureBlobBuildingControlAttachmentStore(connectionString));
    }
}
