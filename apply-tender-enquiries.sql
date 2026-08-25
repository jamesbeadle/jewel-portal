-- Applies 20260825120000_AddTenderEnquiries directly. Identical to what the scoped idempotent EF
-- script would emit for this migration: guarded tables + indexes, then the history row. Safe to
-- re-run. Until this runs, the Tender Enquiries tab and "Log Tender Enquiry" in the Control
-- Centre 500 on the missing tables ("Invalid object name 'TenderEnquiries'").
--
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i apply-tender-enquiries.sql -b -o apply-tender-enquiries.log

IF OBJECT_ID(N'[dbo].[TenderEnquiries]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TenderEnquiries] (
        [TenderEnquiryId]        nvarchar(64)   NOT NULL,
        [ProjectId]              nvarchar(64)   NOT NULL,
        -- Global sequence behind the TEQ-#### reference (the mailbox tag stem).
        [Number]                 int            NOT NULL,
        [Title]                  nvarchar(256)  NOT NULL,
        [ArchitectPracticeName]  nvarchar(256)  NOT NULL,
        [ArchitectContactName]   nvarchar(256)  NOT NULL,
        [ArchitectContactEmail]  nvarchar(256)  NOT NULL,
        [ScopeSummary]           nvarchar(4000) NOT NULL,
        [ContractForm]           nvarchar(256)  NOT NULL,
        -- TenderEnquiryStatus: 0 Received, 1 Accepted, 2 Declined, 3 PqqSubmitted, 4 Shortlisted,
        -- 5 NotShortlisted, 6 TenderSubmitted, 7 Won, 8 Lost.
        [Status]                 int            NOT NULL,
        [ReceivedAt]             datetimeoffset NOT NULL,
        [PqqDueAt]               datetimeoffset NULL,
        [TenderDueAt]            datetimeoffset NULL,
        [PqqSubmittedAt]         datetimeoffset NULL,
        [TenderSubmittedAt]      datetimeoffset NULL,
        [DecidedAt]              datetimeoffset NULL,
        [DecisionNote]           nvarchar(2048) NOT NULL,
        [OwnerEmail]             nvarchar(256)  NOT NULL,
        [CreatedAt]              datetimeoffset NOT NULL,
        [CreatedByEmail]         nvarchar(256)  NOT NULL,
        CONSTRAINT [PK_TenderEnquiries] PRIMARY KEY ([TenderEnquiryId])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TenderEnquiries_ProjectId'
               AND object_id = OBJECT_ID(N'[dbo].[TenderEnquiries]'))
    CREATE INDEX [IX_TenderEnquiries_ProjectId] ON [dbo].[TenderEnquiries] ([ProjectId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TenderEnquiries_Number'
               AND object_id = OBJECT_ID(N'[dbo].[TenderEnquiries]'))
    CREATE INDEX [IX_TenderEnquiries_Number] ON [dbo].[TenderEnquiries] ([Number]);
GO

IF OBJECT_ID(N'[dbo].[TenderEnquiryAnswers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TenderEnquiryAnswers] (
        [TenderEnquiryAnswerId]  nvarchar(64)   NOT NULL,
        [TenderEnquiryId]        nvarchar(64)   NOT NULL,
        [Position]               int            NOT NULL,
        [Question]               nvarchar(2048) NOT NULL,
        [Answer]                 nvarchar(max)  NOT NULL,
        CONSTRAINT [PK_TenderEnquiryAnswers] PRIMARY KEY ([TenderEnquiryAnswerId])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TenderEnquiryAnswers_TenderEnquiryId'
               AND object_id = OBJECT_ID(N'[dbo].[TenderEnquiryAnswers]'))
    CREATE INDEX [IX_TenderEnquiryAnswers_TenderEnquiryId] ON [dbo].[TenderEnquiryAnswers] ([TenderEnquiryId]);
GO

IF OBJECT_ID(N'[dbo].[TenderEnquiryAttachments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TenderEnquiryAttachments] (
        [TenderEnquiryAttachmentId] nvarchar(64)   NOT NULL,
        [TenderEnquiryId]           nvarchar(64)   NOT NULL,
        [ProjectId]                 nvarchar(64)   NOT NULL,
        [FileName]                  nvarchar(256)  NOT NULL,
        [ContentType]               nvarchar(128)  NOT NULL,
        [FileSizeBytes]             bigint         NOT NULL,
        [BlobRef]                   nvarchar(1024) NOT NULL,
        -- TenderEnquiryAttachmentSource: 0 Upload, 1 Email (copied off the enquiry email).
        [Source]                    int            NOT NULL,
        [AddedAt]                   datetimeoffset NOT NULL,
        [AddedByEmail]              nvarchar(256)  NOT NULL,
        CONSTRAINT [PK_TenderEnquiryAttachments] PRIMARY KEY ([TenderEnquiryAttachmentId])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TenderEnquiryAttachments_TenderEnquiryId'
               AND object_id = OBJECT_ID(N'[dbo].[TenderEnquiryAttachments]'))
    CREATE INDEX [IX_TenderEnquiryAttachments_TenderEnquiryId] ON [dbo].[TenderEnquiryAttachments] ([TenderEnquiryId]);
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260825120000_AddTenderEnquiries')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825120000_AddTenderEnquiries', N'8.0.10');
GO
