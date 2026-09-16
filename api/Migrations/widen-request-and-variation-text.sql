-- ============================================================================
-- WidenRequestAndVariationText  (2026-09-16)
-- ============================================================================
-- A pasted RFI longer than 2,048 characters failed to save. Five text columns go
-- to nvarchar(max) so the next long paste finds no new ceiling:
--   Requests.Description, Requests.ResponseText, Requests.ImpactIfLate
--   VariationOrderQuotes.Description          — the variation raised from an RFI copies
--   SubcontractorVariationRequests.Description  the description across
-- Widening only: no data changes; no index or constraint touches these columns.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260916150000_WidenRequestAndVariationText.cs.
-- Safe to apply BEFORE or WITH the deploy.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i widen-request-and-variation-text.sql -b -o widen-request-and-variation-text.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916150000_WidenRequestAndVariationText')
BEGIN
    ALTER TABLE [Requests] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [Requests] ALTER COLUMN [ResponseText] nvarchar(max) NULL;
    ALTER TABLE [Requests] ALTER COLUMN [ImpactIfLate] nvarchar(max) NULL;
    ALTER TABLE [VariationOrderQuotes] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
    ALTER TABLE [SubcontractorVariationRequests] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916150000_WidenRequestAndVariationText')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916150000_WidenRequestAndVariationText', N'8.0.10');
END;
GO

COMMIT;
GO
