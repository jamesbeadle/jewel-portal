-- ============================================================================
-- AddEstimateHouseModel  (2026-09-16)
-- ============================================================================
-- The estimate's 3D model: three additive columns on LeadEstimates —
-- HouseModelJson (the definition the lead page's viewer builds, JSON stored
-- whole; NULL until the assistant drafts one from the enquiry's drawings),
-- HouseModelSource (which sheets and revision it was read from) and
-- HouseModelSetAt. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260916170000_AddEstimateHouseModel.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the new columns.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-estimate-house-model.sql -b -o add-estimate-house-model.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916170000_AddEstimateHouseModel')
BEGIN
    IF COL_LENGTH(N'[LeadEstimates]', N'HouseModelJson') IS NULL
        ALTER TABLE [LeadEstimates] ADD [HouseModelJson] nvarchar(max) NULL;
    IF COL_LENGTH(N'[LeadEstimates]', N'HouseModelSource') IS NULL
        ALTER TABLE [LeadEstimates] ADD [HouseModelSource] nvarchar(1024) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'[LeadEstimates]', N'HouseModelSetAt') IS NULL
        ALTER TABLE [LeadEstimates] ADD [HouseModelSetAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916170000_AddEstimateHouseModel')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916170000_AddEstimateHouseModel', N'8.0.10');
END;
GO

COMMIT;
GO
