-- ============================================================================
-- AddOnboardingForms  (2026-09-23)
-- ============================================================================
-- The forms new starters and sub-contractors fill in, moved from the JPS
-- Dashboard into the portal for both Jewel companies:
--   FormInvites, FormPacks   one-time links and new starter packs (only the
--                            SHA-256 of a link's secret is stored)
--   FormSubmissions          every form sent, answers as JSON
--   FormUploads              the files sent with them (the bytes live in the
--                            form-uploads, form-right-to-work and
--                            form-payroll-starters containers, never here)
--   FormFolders              the person or company everything is filed under,
--                            with the dates the retention clocks run from
--   RightToWorkChecks, TrainingRecords, WorkstationActions and
--   DrivingLicenceChecks     the registers the forms feed
-- and ComplianceDocuments gains LastChasedAt / ChaseCount for the insurance
-- renewal chase and FormCompany (the Jewel company whose form a certificate
-- came in on; only those are chased). Additive only; no FKs, as everywhere else.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260923120000_AddOnboardingForms.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the tables.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-onboarding-forms.sql -b -o add-onboarding-forms.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260923120000_AddOnboardingForms')
BEGIN
    IF OBJECT_ID(N'[FormPacks]', N'U') IS NULL
    BEGIN
        CREATE TABLE [FormPacks] (
            [FormPackId] nvarchar(64) NOT NULL,
            [Company] int NOT NULL,
            [PersonName] nvarchar(256) NOT NULL,
            [Email] nvarchar(256) NOT NULL,
            [EngagedAs] int NOT NULL,
            [HasP45] bit NOT NULL,
            [IsWorkingAtAScreen] bit NOT NULL,
            [IsGettingAVehicle] bit NOT NULL,
            [MustHoldATicket] bit NOT NULL,
            [TokenHash] nvarchar(64) NOT NULL,
            [ExpiresAt] datetimeoffset NOT NULL,
            [SentByEmail] nvarchar(256) NOT NULL,
            [SentByName] nvarchar(256) NOT NULL,
            [SentAt] datetimeoffset NOT NULL,
            [OpenedAt] datetimeoffset NULL,
            [CompletedAt] datetimeoffset NULL,
            [LastChasedAt] datetimeoffset NULL,
            [ChaseCount] int NOT NULL,
            [CancelledAt] datetimeoffset NULL,
            CONSTRAINT [PK_FormPacks] PRIMARY KEY ([FormPackId])
        );
        CREATE UNIQUE INDEX [IX_FormPacks_TokenHash] ON [FormPacks] ([TokenHash]);
    END;

    IF OBJECT_ID(N'[FormInvites]', N'U') IS NULL
    BEGIN
        CREATE TABLE [FormInvites] (
            [FormInviteId] nvarchar(64) NOT NULL,
            [FormPackId] nvarchar(64) NULL,
            [FormSlug] nvarchar(64) NOT NULL,
            [Company] int NOT NULL,
            [PersonName] nvarchar(256) NOT NULL,
            [CompanyName] nvarchar(256) NOT NULL,
            [Email] nvarchar(256) NOT NULL,
            [TokenHash] nvarchar(64) NOT NULL,
            [ExpiresAt] datetimeoffset NOT NULL,
            [SentByEmail] nvarchar(256) NOT NULL,
            [SentByName] nvarchar(256) NOT NULL,
            [SentAt] datetimeoffset NOT NULL,
            [OpenedAt] datetimeoffset NULL,
            [UsedAt] datetimeoffset NULL,
            [FormSubmissionId] nvarchar(64) NULL,
            [CancelledAt] datetimeoffset NULL,
            [Reason] nvarchar(512) NOT NULL,
            CONSTRAINT [PK_FormInvites] PRIMARY KEY ([FormInviteId])
        );
        CREATE UNIQUE INDEX [IX_FormInvites_TokenHash] ON [FormInvites] ([TokenHash]);
        CREATE INDEX [IX_FormInvites_FormPackId] ON [FormInvites] ([FormPackId]);
    END;

    IF OBJECT_ID(N'[FormSubmissions]', N'U') IS NULL
    BEGIN
        CREATE TABLE [FormSubmissions] (
            [FormSubmissionId] nvarchar(64) NOT NULL,
            [FormSlug] nvarchar(64) NOT NULL,
            [Company] int NOT NULL,
            [SessionId] nvarchar(64) NOT NULL,
            [FormInviteId] nvarchar(64) NULL,
            [FormPackId] nvarchar(64) NULL,
            [FormFolderId] nvarchar(64) NULL,
            [SubmitterName] nvarchar(256) NOT NULL,
            [FilingName] nvarchar(256) NOT NULL,
            [IsVerifiedLink] bit NOT NULL,
            [SentToEmail] nvarchar(256) NOT NULL,
            [SentByName] nvarchar(256) NOT NULL,
            [AnswersJson] nvarchar(max) NOT NULL,
            [Status] int NOT NULL,
            [SubmittedAt] datetimeoffset NOT NULL,
            [HandledByEmail] nvarchar(256) NOT NULL,
            [HandledAt] datetimeoffset NULL,
            [RedactedAt] datetimeoffset NULL,
            [DestroyedAt] datetimeoffset NULL,
            [ClientHash] nvarchar(64) NOT NULL,
            CONSTRAINT [PK_FormSubmissions] PRIMARY KEY ([FormSubmissionId])
        );
        CREATE INDEX [IX_FormSubmissions_SessionId] ON [FormSubmissions] ([SessionId]);
        CREATE INDEX [IX_FormSubmissions_SubmittedAt] ON [FormSubmissions] ([SubmittedAt]);
    END;

    IF OBJECT_ID(N'[FormUploads]', N'U') IS NULL
    BEGIN
        CREATE TABLE [FormUploads] (
            [FormUploadId] nvarchar(64) NOT NULL,
            [SessionId] nvarchar(64) NOT NULL,
            [FormSlug] nvarchar(64) NOT NULL,
            [Company] int NOT NULL,
            [QuestionKey] nvarchar(64) NOT NULL,
            [Store] int NOT NULL,
            [BlobRef] nvarchar(512) NOT NULL,
            [FileName] nvarchar(256) NOT NULL,
            [ContentType] nvarchar(128) NOT NULL,
            [Size] bigint NOT NULL,
            [UploadedAt] datetimeoffset NOT NULL,
            [FormSubmissionId] nvarchar(64) NULL,
            [ClientHash] nvarchar(64) NOT NULL,
            [DeletedAt] datetimeoffset NULL,
            [DeletionReason] nvarchar(256) NOT NULL,
            CONSTRAINT [PK_FormUploads] PRIMARY KEY ([FormUploadId])
        );
        CREATE INDEX [IX_FormUploads_SessionId] ON [FormUploads] ([SessionId]);
        CREATE INDEX [IX_FormUploads_FormSubmissionId] ON [FormUploads] ([FormSubmissionId]);
    END;

    IF OBJECT_ID(N'[FormFolders]', N'U') IS NULL
    BEGIN
        CREATE TABLE [FormFolders] (
            [FormFolderId] nvarchar(64) NOT NULL,
            [Name] nvarchar(256) NOT NULL,
            [NormalizedName] nvarchar(256) NOT NULL,
            [Kind] int NOT NULL,
            [Company] int NOT NULL,
            [EngagementEndedOn] date NULL,
            [VehicleReturnedOn] date NULL,
            [CreatedAt] datetimeoffset NOT NULL,
            [LastSubmittedAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_FormFolders] PRIMARY KEY ([FormFolderId])
        );
        CREATE INDEX [IX_FormFolders_NormalizedName] ON [FormFolders] ([NormalizedName]);
    END;

    IF OBJECT_ID(N'[RightToWorkChecks]', N'U') IS NULL
    BEGIN
        CREATE TABLE [RightToWorkChecks] (
            [RightToWorkCheckId] nvarchar(64) NOT NULL,
            [FormSubmissionId] nvarchar(64) NULL,
            [PersonName] nvarchar(256) NOT NULL,
            [Email] nvarchar(256) NOT NULL,
            [Company] int NOT NULL,
            [JobRole] nvarchar(256) NOT NULL,
            [EngagedAs] int NOT NULL,
            [EngagedSince] date NULL,
            [Route] int NOT NULL,
            [Reference] nvarchar(64) NOT NULL,
            [IdspProvider] nvarchar(128) NOT NULL,
            [SeenVia] int NOT NULL,
            [DocumentReference] nvarchar(256) NOT NULL,
            [CheckedByName] nvarchar(256) NOT NULL,
            [CheckedOn] date NOT NULL,
            [IsDocumentGenuine] bit NOT NULL,
            [IsLikenessConfirmed] bit NOT NULL,
            [IsPermittedToDoTheWork] bit NOT NULL,
            [IsEvidenceFiled] bit NOT NULL,
            [IsTimeLimited] bit NOT NULL,
            [PermissionExpiresOn] date NULL,
            [FollowUpOn] date NULL,
            [Outcome] int NOT NULL,
            [Notes] nvarchar(2000) NOT NULL,
            [EvidenceUploadId] nvarchar(64) NULL,
            [RecordedByEmail] nvarchar(256) NOT NULL,
            [RecordedAt] datetimeoffset NOT NULL,
            [EngagementEndedOn] date NULL,
            [ConfirmedOn] date NULL,
            CONSTRAINT [PK_RightToWorkChecks] PRIMARY KEY ([RightToWorkCheckId])
        );
    END;

    IF OBJECT_ID(N'[TrainingRecords]', N'U') IS NULL
    BEGIN
        CREATE TABLE [TrainingRecords] (
            [TrainingRecordId] nvarchar(64) NOT NULL,
            [FormSubmissionId] nvarchar(64) NULL,
            [Company] int NOT NULL,
            [PersonName] nvarchar(256) NOT NULL,
            [Email] nvarchar(256) NOT NULL,
            [Course] nvarchar(256) NOT NULL,
            [Provider] nvarchar(256) NOT NULL,
            [CertificateNumber] nvarchar(128) NOT NULL,
            [CompletedOn] date NOT NULL,
            [ExpiresOn] date NULL,
            [CertificateUploadId] nvarchar(64) NULL,
            [AcceptedByEmail] nvarchar(256) NOT NULL,
            [AcceptedAt] datetimeoffset NOT NULL,
            [LastChasedAt] datetimeoffset NULL,
            [ChaseCount] int NOT NULL,
            [EndedOn] date NULL,
            CONSTRAINT [PK_TrainingRecords] PRIMARY KEY ([TrainingRecordId])
        );
    END;

    IF OBJECT_ID(N'[WorkstationActions]', N'U') IS NULL
    BEGIN
        CREATE TABLE [WorkstationActions] (
            [WorkstationActionId] nvarchar(64) NOT NULL,
            [FormSubmissionId] nvarchar(64) NOT NULL,
            [PersonName] nvarchar(256) NOT NULL,
            [Workstation] nvarchar(128) NOT NULL,
            [QuestionKey] nvarchar(64) NOT NULL,
            [Action] nvarchar(1024) NOT NULL,
            [State] int NOT NULL,
            [Note] nvarchar(1000) NOT NULL,
            [ResolvedByEmail] nvarchar(256) NOT NULL,
            [ResolvedAt] datetimeoffset NULL,
            [RaisedAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_WorkstationActions] PRIMARY KEY ([WorkstationActionId])
        );
        CREATE INDEX [IX_WorkstationActions_FormSubmissionId] ON [WorkstationActions] ([FormSubmissionId]);
    END;

    IF OBJECT_ID(N'[DrivingLicenceChecks]', N'U') IS NULL
    BEGIN
        CREATE TABLE [DrivingLicenceChecks] (
            [DrivingLicenceCheckId] nvarchar(64) NOT NULL,
            [FormSubmissionId] nvarchar(64) NOT NULL,
            [DvlaCheckedOn] date NOT NULL,
            [IsWithinInsuranceCriteria] bit NOT NULL,
            [Note] nvarchar(1000) NOT NULL,
            [CheckedByEmail] nvarchar(256) NOT NULL,
            [CheckedAt] datetimeoffset NOT NULL,
            [PhotosDeleted] int NOT NULL,
            CONSTRAINT [PK_DrivingLicenceChecks] PRIMARY KEY ([DrivingLicenceCheckId])
        );
        CREATE UNIQUE INDEX [IX_DrivingLicenceChecks_FormSubmissionId] ON [DrivingLicenceChecks] ([FormSubmissionId]);
    END;

    IF COL_LENGTH(N'[ComplianceDocuments]', N'LastChasedAt') IS NULL
        ALTER TABLE [ComplianceDocuments] ADD [LastChasedAt] datetimeoffset NULL;

    IF COL_LENGTH(N'[ComplianceDocuments]', N'ChaseCount') IS NULL
        ALTER TABLE [ComplianceDocuments] ADD [ChaseCount] int NOT NULL DEFAULT 0;

    IF COL_LENGTH(N'[ComplianceDocuments]', N'FormCompany') IS NULL
        ALTER TABLE [ComplianceDocuments] ADD [FormCompany] int NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260923120000_AddOnboardingForms')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260923120000_AddOnboardingForms', N'8.0.10');
END;
GO

COMMIT;
GO
