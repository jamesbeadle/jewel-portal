using Jewel.JPMS.Api.Features.Forms.Office.Links;
using Jewel.JPMS.Api.Features.Forms.Office.Submissions;
using Jewel.JPMS.Api.Features.Forms.Registers;
using Jewel.JPMS.Contracts.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Forms;

/// <summary>The office's side of the forms: every command with its gates, and every query.</summary>
internal static class FormsOfficeRegistration
{
    public static IServiceCollection AddFormLinkCommands(this IServiceCollection services)
    {
        services.AddCommand<SendFormInvite, SentFormLink, SendFormInviteHandler, SendFormInviteAuthorisation, SendFormInviteValidation>();
        services.AddCommand<ResendFormInvite, SentFormLink, ResendFormInviteHandler, ResendFormInviteAuthorisation, ResendFormInviteValidation>();
        services.AddCommand<CancelFormInvite, FormInvite, CancelFormInviteHandler, CancelFormInviteAuthorisation, CancelFormInviteValidation>();
        services.AddCommand<SendFormPack, SentFormPack, SendFormPackHandler, SendFormPackAuthorisation, SendFormPackValidation>();
        services.AddCommand<ChaseFormPack, SentFormPack, ChaseFormPackHandler, ChaseFormPackAuthorisation, ChaseFormPackValidation>();
        services.AddCommand<CancelFormPack, FormPack, CancelFormPackHandler, CancelFormPackAuthorisation, CancelFormPackValidation>();
        services.AddScoped<IQueryHandler<ListFormPacks, IReadOnlyList<FormPack>>, ListFormPacksHandler>();
        services.AddScoped<IQueryHandler<ListFormInvites, IReadOnlyList<FormInvite>>, ListFormInvitesHandler>();
        return services;
    }

    public static IServiceCollection AddFormSubmissionCommands(this IServiceCollection services)
    {
        services.AddCommand<SetFormSubmissionStatus, FormSubmission, SetFormSubmissionStatusHandler,
            SetFormSubmissionStatusAuthorisation, SetFormSubmissionStatusValidation>();
        services.AddCommand<RecordFormFolderDates, FormFolder, RecordFormFolderDatesHandler,
            RecordFormFolderDatesAuthorisation, RecordFormFolderDatesValidation>();
        services.AddCommand<FileFormToDirectory, FormDirectoryFiling, FileFormToDirectoryHandler,
            FileFormToDirectoryAuthorisation, FileFormToDirectoryValidation>();
        services.AddCommand<FileQuizToDirectory, FormDirectoryFiling, FileQuizToDirectoryHandler,
            FileQuizToDirectoryAuthorisation, FileQuizToDirectoryValidation>();
        services.AddScoped<IQueryHandler<ListFormSubmissions, IReadOnlyList<FormSubmission>>, ListFormSubmissionsHandler>();
        services.AddScoped<IQueryHandler<OpenFormSubmission, FormSubmissionView>, OpenFormSubmissionHandler>();
        services.AddScoped<IQueryHandler<RevealHealthAnswers, IReadOnlyDictionary<string, string>>, RevealHealthAnswersHandler>();
        services.AddScoped<IQueryHandler<ListFormFolders, IReadOnlyList<FormFolder>>, ListFormFoldersHandler>();
        services.AddScoped<IQueryHandler<ListEmergencyContacts, IReadOnlyList<EmergencyContactCard>>, ListEmergencyContactsHandler>();
        return services;
    }

    public static IServiceCollection AddFormRegisterCommands(this IServiceCollection services)
    {
        services.AddCommand<SaveRightToWorkCheck, RightToWorkCheck, SaveRightToWorkCheckHandler,
            SaveRightToWorkCheckAuthorisation, SaveRightToWorkCheckValidation>();
        services.AddCommand<SendRightToWorkConfirmation, RightToWorkCheck, SendRightToWorkConfirmationHandler,
            SendRightToWorkConfirmationAuthorisation, SendRightToWorkConfirmationValidation>();
        services.AddCommand<AcceptTrainingCertificate, TrainingRecord, AcceptTrainingCertificateHandler,
            AcceptTrainingCertificateAuthorisation, AcceptTrainingCertificateValidation>();
        services.AddCommand<SetTrainingRecordDetails, TrainingRecord, SetTrainingRecordDetailsHandler,
            SetTrainingRecordDetailsAuthorisation, SetTrainingRecordDetailsValidation>();
        services.AddCommand<ResolveWorkstationAction, WorkstationAction, ResolveWorkstationActionHandler,
            ResolveWorkstationActionAuthorisation, ResolveWorkstationActionValidation>();
        services.AddCommand<RecordDrivingLicenceCheck, DrivingLicenceCheck, RecordDrivingLicenceCheckHandler,
            RecordDrivingLicenceCheckAuthorisation, RecordDrivingLicenceCheckValidation>();
        services.AddScoped<IQueryHandler<ListRightToWorkChecks, IReadOnlyList<RightToWorkCheck>>, ListRightToWorkChecksHandler>();
        services.AddScoped<IQueryHandler<ListTrainingRecords, IReadOnlyList<TrainingRecord>>, ListTrainingRecordsHandler>();
        services.AddScoped<IQueryHandler<ListWorkstationActions, IReadOnlyList<WorkstationAction>>, ListWorkstationActionsHandler>();
        services.AddScoped<IQueryHandler<ListDrivingLicenceChecks, IReadOnlyList<DrivingLicenceCheck>>, ListDrivingLicenceChecksHandler>();
        return services;
    }

    private static void AddCommand<TCommand, TResult, THandler, TAuthorisation, TValidation>(this IServiceCollection services)
        where TCommand : ICommand<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
        where TAuthorisation : class
        where TValidation : class
    {
        services.AddScoped<ICommandHandler<TCommand, TResult>, THandler>();
        services.AddScoped<TAuthorisation>();
        services.AddScoped<TValidation>();
    }
}
