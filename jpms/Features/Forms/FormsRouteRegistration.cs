using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Features.Forms.Office;
using Jewel.JPMS.Features.Forms.Public;

namespace Jewel.JPMS.Features.Forms;

/// <summary>
/// The forms' client routes, mirroring api/Features/Forms: the office's reads and writes over the
/// forms that came in, the links sent out and the three registers they feed. The public pages talk
/// to the public API with a raw HttpClient and have no routes here.
/// </summary>
public static class FormsRouteRegistration
{
    private const string SubmissionsPath = "/api/form-submissions";

    public static IServiceCollection AddFormsServices(this IServiceCollection services)
    {
        services.AddScoped<FormDraftStorage>();
        services.AddScoped<FormsPageEntry>();
        return services;
    }

    public static void RegisterFormsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        RegisterSubmissionRoutes(queries, commands);
        RegisterLinkRoutes(queries, commands);
        RegisterRegisterRoutes(queries, commands);
    }

    private static void RegisterSubmissionRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListFormSubmissions, IReadOnlyList<FormSubmission>>(new QueryRoute(SubmissionsPath,
            query => ((ListFormSubmissions)query).FormFolderId is { } folder ? $"{SubmissionsPath}?folder={Uri.EscapeDataString(folder)}" : SubmissionsPath));
        queries.Register<OpenFormSubmission, FormSubmissionView>(new QueryRoute("/api/form-submissions/{formSubmissionId}",
            query => $"/api/form-submissions/{((OpenFormSubmission)query).FormSubmissionId}"));
        queries.Register<RevealHealthAnswers, IReadOnlyDictionary<string, string>>(new QueryRoute("/api/form-submissions/{formSubmissionId}/health",
            query => $"/api/form-submissions/{((RevealHealthAnswers)query).FormSubmissionId}/health"));
        queries.Register<ListFormFolders, IReadOnlyList<FormFolder>>(QueryRoute.Static("/api/form-folders"));
        queries.Register<ListEmergencyContacts, IReadOnlyList<EmergencyContactCard>>(QueryRoute.Static("/api/emergency-contacts"));
        commands.Register<SetFormSubmissionStatus, FormSubmission>(new CommandRoute("PUT", "/api/form-submissions/{formSubmissionId}/status",
            command => $"/api/form-submissions/{((SetFormSubmissionStatus)command).FormSubmissionId}/status"));
        commands.Register<FileFormToDirectory, FormDirectoryFiling>(new CommandRoute("POST", "/api/form-submissions/{formSubmissionId}/file-to-directory",
            command => $"/api/form-submissions/{((FileFormToDirectory)command).FormSubmissionId}/file-to-directory"));
        commands.Register<FileQuizToDirectory, FormDirectoryFiling>(new CommandRoute("POST", "/api/form-submissions/{formSubmissionId}/file-quiz-to-directory",
            command => $"/api/form-submissions/{((FileQuizToDirectory)command).FormSubmissionId}/file-quiz-to-directory"));
        commands.Register<RecordFormFolderDates, FormFolder>(new CommandRoute("PUT", "/api/form-folders/{formFolderId}/dates",
            command => $"/api/form-folders/{((RecordFormFolderDates)command).FormFolderId}/dates"));
    }

    private static void RegisterLinkRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListFormPacks, IReadOnlyList<FormPack>>(QueryRoute.Static("/api/form-packs"));
        queries.Register<ListFormInvites, IReadOnlyList<FormInvite>>(QueryRoute.Static("/api/form-invites"));
        commands.Register<SendFormInvite, SentFormLink>(CommandRoute.Post("/api/form-invites"));
        commands.Register<ResendFormInvite, SentFormLink>(new CommandRoute("POST", "/api/form-invites/{formInviteId}/resend",
            command => $"/api/form-invites/{((ResendFormInvite)command).FormInviteId}/resend"));
        commands.Register<CancelFormInvite, FormInvite>(new CommandRoute("POST", "/api/form-invites/{formInviteId}/cancel",
            command => $"/api/form-invites/{((CancelFormInvite)command).FormInviteId}/cancel"));
        commands.Register<SendFormPack, SentFormPack>(CommandRoute.Post("/api/form-packs"));
        commands.Register<ChaseFormPack, SentFormPack>(new CommandRoute("POST", "/api/form-packs/{formPackId}/chase",
            command => $"/api/form-packs/{((ChaseFormPack)command).FormPackId}/chase"));
        commands.Register<CancelFormPack, FormPack>(new CommandRoute("POST", "/api/form-packs/{formPackId}/cancel",
            command => $"/api/form-packs/{((CancelFormPack)command).FormPackId}/cancel"));
    }

    private static void RegisterRegisterRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListRightToWorkChecks, IReadOnlyList<RightToWorkCheck>>(QueryRoute.Static("/api/right-to-work-checks"));
        queries.Register<ListTrainingRecords, IReadOnlyList<TrainingRecord>>(QueryRoute.Static("/api/training-records"));
        queries.Register<ListWorkstationActions, IReadOnlyList<WorkstationAction>>(QueryRoute.Static("/api/workstation-actions"));
        queries.Register<ListDrivingLicenceChecks, IReadOnlyList<DrivingLicenceCheck>>(QueryRoute.Static("/api/driving-licence-checks"));
        commands.Register<SaveRightToWorkCheck, RightToWorkCheck>(CommandRoute.Post("/api/right-to-work-checks"));
        commands.Register<SendRightToWorkConfirmation, RightToWorkCheck>(new CommandRoute("POST", "/api/right-to-work-checks/{rightToWorkCheckId}/confirmation",
            command => $"/api/right-to-work-checks/{((SendRightToWorkConfirmation)command).RightToWorkCheckId}/confirmation"));
        commands.Register<AcceptTrainingCertificate, TrainingRecord>(CommandRoute.Post("/api/training-records"));
        commands.Register<SetTrainingRecordDetails, TrainingRecord>(new CommandRoute("PUT", "/api/training-records/{trainingRecordId}",
            command => $"/api/training-records/{((SetTrainingRecordDetails)command).TrainingRecordId}"));
        commands.Register<ResolveWorkstationAction, WorkstationAction>(new CommandRoute("POST", "/api/workstation-actions/{workstationActionId}/resolve",
            command => $"/api/workstation-actions/{((ResolveWorkstationAction)command).WorkstationActionId}/resolve"));
        commands.Register<RecordDrivingLicenceCheck, DrivingLicenceCheck>(CommandRoute.Post("/api/driving-licence-checks"));
    }
}
