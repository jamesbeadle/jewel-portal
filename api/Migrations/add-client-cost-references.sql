-- ============================================================================
-- AddClientCostReferences  (2026-08-25)
-- ============================================================================
-- The client's schedule-of-works references: a ClientCostReferences table (one
-- row per project + cost centre, holding the client's item number for it) and a
-- ClientReference column on ValuationReportSnapshotLines (the frozen copy the
-- client PDF prints). Additive only, so apply it BEFORE the deploy: the deployed
-- API maps the new column and every snapshot read would 500 until it exists.
--
-- Mirrors api/Migrations/20260825150000_AddClientCostReferences.cs. Each object
-- is guarded individually on top of the history guard, and the migration id is
-- recorded in __EFMigrationsHistory so EF never re-applies it.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i api/Migrations/add-client-cost-references.sql -b -o migrate.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825150000_AddClientCostReferences'
)
BEGIN
    IF COL_LENGTH('ValuationReportSnapshotLines', 'ClientReference') IS NULL
        ALTER TABLE [ValuationReportSnapshotLines]
            ADD [ClientReference] nvarchar(64) NOT NULL CONSTRAINT [DF_ValuationReportSnapshotLines_ClientReference] DEFAULT N'';

    IF OBJECT_ID(N'[ClientCostReferences]', N'U') IS NULL
    BEGIN
        CREATE TABLE [ClientCostReferences] (
            [ClientCostReferenceId] nvarchar(64) NOT NULL,
            [ProjectId] nvarchar(64) NOT NULL,
            [CostCode] nvarchar(32) NOT NULL,
            [ClientReference] nvarchar(64) NOT NULL,
            CONSTRAINT [PK_ClientCostReferences] PRIMARY KEY ([ClientCostReferenceId])
        );
        CREATE UNIQUE INDEX [IX_ClientCostReferences_ProjectId_CostCode]
            ON [ClientCostReferences] ([ProjectId], [CostCode]);
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825150000_AddClientCostReferences'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825150000_AddClientCostReferences', N'8.0.10');
END;
GO

COMMIT;
GO
