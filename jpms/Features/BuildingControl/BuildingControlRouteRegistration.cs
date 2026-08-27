using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.BuildingControl;

// Client routes for building control. Mirrors the api endpoints in Features/BuildingControl:
// the view and case set-up are project-scoped; case/inspection edits address the record; the
// Control Centre's "Raise Building Control Inspection" goes through the mailbox route. Multipart
// uploads and file downloads live on IBuildingControlAttachmentClient, not here.
public static class BuildingControlRouteRegistration
{
    public static IServiceCollection AddBuildingControlReadModels(this IServiceCollection services)
    {
        services.AddScoped<BuildingControlReadModel>();
        return services;
    }

    public static void RegisterBuildingControlRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<GetBuildingControlForProject, BuildingControlProjectView>(
            new QueryRoute("/api/projects/{projectId}/building-control",
                query => $"/api/projects/{((GetBuildingControlForProject)query).ProjectId}/building-control"));

        commands.Register<CreateBuildingControlCase, BuildingControlCase>(
            new CommandRoute("POST", "/api/projects/{projectId}/building-control/case",
                command => $"/api/projects/{((CreateBuildingControlCase)command).ProjectId}/building-control/case"));

        commands.Register<UpdateBuildingControlCase, BuildingControlCase>(
            new CommandRoute("PUT", "/api/building-control/cases/{caseId}",
                command => $"/api/building-control/cases/{((UpdateBuildingControlCase)command).BuildingControlCaseId}"));

        commands.Register<SetBuildingControlCaseStatus, BuildingControlCase>(
            new CommandRoute("POST", "/api/building-control/cases/{caseId}/status",
                command => $"/api/building-control/cases/{((SetBuildingControlCaseStatus)command).BuildingControlCaseId}/status"));

        commands.Register<AddBuildingControlInspection, BuildingControlInspection>(
            new CommandRoute("POST", "/api/building-control/cases/{caseId}/inspections",
                command => $"/api/building-control/cases/{((AddBuildingControlInspection)command).BuildingControlCaseId}/inspections"));

        commands.Register<UpdateBuildingControlInspection, BuildingControlInspection>(
            new CommandRoute("PUT", "/api/building-control/inspections/{inspectionId}",
                command => $"/api/building-control/inspections/{((UpdateBuildingControlInspection)command).BuildingControlInspectionId}"));

        commands.Register<SetBuildingControlInspectionStatus, BuildingControlInspection>(
            new CommandRoute("POST", "/api/building-control/inspections/{inspectionId}/status",
                command => $"/api/building-control/inspections/{((SetBuildingControlInspectionStatus)command).BuildingControlInspectionId}/status"));

        commands.Register<DeleteBuildingControlInspection, Acknowledgement>(
            new CommandRoute("DELETE", "/api/building-control/inspections/{inspectionId}",
                command => $"/api/building-control/inspections/{((DeleteBuildingControlInspection)command).BuildingControlInspectionId}"));

        commands.Register<SetBuildingControlAttachmentKind, BuildingControlAttachment>(
            new CommandRoute("POST", "/api/building-control/attachments/{attachmentId}/kind",
                command => $"/api/building-control/attachments/{((SetBuildingControlAttachmentKind)command).BuildingControlAttachmentId}/kind"));

        commands.Register<RemoveBuildingControlAttachment, Acknowledgement>(
            new CommandRoute("DELETE", "/api/building-control/attachments/{attachmentId}",
                command => $"/api/building-control/attachments/{((RemoveBuildingControlAttachment)command).BuildingControlAttachmentId}"));

        commands.Register<CopyEmailAttachmentsToBuildingControlInspection, IReadOnlyList<BuildingControlAttachment>>(
            new CommandRoute("POST", "/api/building-control/inspections/{inspectionId}/copy-email-attachments",
                command => $"/api/building-control/inspections/{((CopyEmailAttachmentsToBuildingControlInspection)command).BuildingControlInspectionId}/copy-email-attachments"));

        // The Control Centre's "create new → Building Control Inspection": raise + link the
        // inspector's email.
        commands.Register<CreateBuildingControlInspectionFromMessage, BuildingControlInspection>(
            new CommandRoute("POST", "/api/mailbox/message/create-building-control-inspection",
                _ => "/api/mailbox/message/create-building-control-inspection"));
    }
}
