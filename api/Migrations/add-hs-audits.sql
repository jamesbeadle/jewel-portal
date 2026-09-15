-- ============================================================================
-- AddHsAudits  (2026-09-15)
-- ============================================================================
-- H&S site audits: Katy-Louise's inspection workbook brought into the portal.
-- HsAudits (per-project HSA-#### number, Draft → Issued → Closed, the header
-- fields, the spreadsheet's score) and HsAuditItems (one row per framework
-- item per audit with what the officer found and the corrective action Issue
-- minted for it). HsRecords gains AssignedToName so a corrective action can be
-- owned by a person with no portal login. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260915150000_AddHsAudits.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the new tables.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-hs-audits.sql -b -o add-hs-audits.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260915150000_AddHsAudits')
BEGIN
    IF COL_LENGTH(N'[HsRecords]', N'AssignedToName') IS NULL
    BEGIN
        ALTER TABLE [HsRecords] ADD [AssignedToName] nvarchar(256) NOT NULL DEFAULT N'';
    END;

    IF OBJECT_ID(N'[HsAudits]', N'U') IS NULL
    BEGIN
        CREATE TABLE [HsAudits] (
            [HsAuditId] nvarchar(64) NOT NULL,
            [ProjectId] nvarchar(64) NOT NULL,
            [Number] int NOT NULL,
            [Status] int NOT NULL,
            [Type] int NOT NULL,
            [InspectionDate] datetimeoffset NOT NULL,
            [SiteManagerName] nvarchar(256) NOT NULL,
            [SafetyOfficerName] nvarchar(256) NOT NULL,
            [SummaryOfWorkActivities] nvarchar(4000) NOT NULL,
            [SiteOperativeCount] int NULL,
            [FurtherComments] nvarchar(4000) NOT NULL,
            [Score] decimal(18,4) NULL,
            [PreviousScore] decimal(18,4) NULL,
            [TemplateVersion] nvarchar(32) NOT NULL,
            [ManagerName] nvarchar(256) NOT NULL,
            [IssuedAt] datetimeoffset NULL,
            [ClosedAt] datetimeoffset NULL,
            [CreatedByEmail] nvarchar(256) NOT NULL,
            [CreatedAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_HsAudits] PRIMARY KEY ([HsAuditId])
        );
        CREATE INDEX [IX_HsAudits_ProjectId] ON [HsAudits] ([ProjectId]);
    END;

    IF OBJECT_ID(N'[HsAuditItems]', N'U') IS NULL
    BEGIN
        CREATE TABLE [HsAuditItems] (
            [HsAuditItemId] nvarchar(64) NOT NULL,
            [HsAuditId] nvarchar(64) NOT NULL,
            [Code] nvarchar(16) NOT NULL,
            [Section] int NOT NULL,
            [Name] nvarchar(256) NOT NULL,
            [DisplayOrder] int NOT NULL,
            [Comment] int NULL,
            [Rate] int NULL,
            [Class] int NULL,
            [Minus] int NOT NULL,
            [TimeScale] int NULL,
            [Findings] nvarchar(2000) NOT NULL,
            [OwnerName] nvarchar(256) NOT NULL,
            [DateRectified] datetimeoffset NULL,
            [HsRecordId] nvarchar(64) NULL,
            CONSTRAINT [PK_HsAuditItems] PRIMARY KEY ([HsAuditItemId])
        );
        CREATE INDEX [IX_HsAuditItems_HsAuditId] ON [HsAuditItems] ([HsAuditId]);
        CREATE INDEX [IX_HsAuditItems_HsRecordId] ON [HsAuditItems] ([HsRecordId]);
    END;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260915150000_AddHsAudits')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915150000_AddHsAudits', N'8.0.10');
END;
GO

COMMIT;
GO
