-- ============================================================================
-- AddSiteLogRaisedRecords  (2026-09-28)
-- ============================================================================
-- The records a worker's daily log raises beside its note: a Site Instruction
-- carries who gave it on site, on which working day and whether it was verbal;
-- both a Site Instruction and a defect carry the day's note (progress update)
-- they came off and whose log it was. Additive only: nullable or defaulted
-- columns and two indexes, no data changes.
--
-- House-style scoped script, as add-site-drawing-links.sql: applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260928220252_AddSiteLogRaisedRecords.cs.
-- Apply BEFORE or WITH the deploy. Safe to run twice.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-site-log-raised-records.sql -b -o add-site-log-raised-records.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928220252_AddSiteLogRaisedRecords'
)
BEGIN
    IF COL_LENGTH(N'SiteInstructions', N'GivenBy') IS NULL
        ALTER TABLE [SiteInstructions] ADD [GivenBy] nvarchar(128) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'SiteInstructions', N'GivenOn') IS NULL
        ALTER TABLE [SiteInstructions] ADD [GivenOn] datetimeoffset NULL;
    IF COL_LENGTH(N'SiteInstructions', N'IsVerbal') IS NULL
        ALTER TABLE [SiteInstructions] ADD [IsVerbal] bit NOT NULL DEFAULT CAST(0 AS bit);
    IF COL_LENGTH(N'SiteInstructions', N'ProgressUpdateId') IS NULL
        ALTER TABLE [SiteInstructions] ADD [ProgressUpdateId] nvarchar(64) NULL;
    IF COL_LENGTH(N'SiteInstructions', N'RaisedByEmail') IS NULL
        ALTER TABLE [SiteInstructions] ADD [RaisedByEmail] nvarchar(256) NOT NULL DEFAULT N'';

    IF COL_LENGTH(N'Defects', N'ProgressUpdateId') IS NULL
        ALTER TABLE [Defects] ADD [ProgressUpdateId] nvarchar(64) NULL;
    IF COL_LENGTH(N'Defects', N'RaisedByEmail') IS NULL
        ALTER TABLE [Defects] ADD [RaisedByEmail] nvarchar(256) NOT NULL DEFAULT N'';

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_SiteInstructions_ProgressUpdateId' AND object_id = OBJECT_ID(N'SiteInstructions'))
        CREATE INDEX [IX_SiteInstructions_ProgressUpdateId] ON [SiteInstructions] ([ProgressUpdateId]);
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_Defects_ProgressUpdateId' AND object_id = OBJECT_ID(N'Defects'))
        CREATE INDEX [IX_Defects_ProgressUpdateId] ON [Defects] ([ProgressUpdateId]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260928220252_AddSiteLogRaisedRecords', N'8.0.10');
END;
GO

COMMIT;
GO
