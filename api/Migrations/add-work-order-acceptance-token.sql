-- ============================================================================
-- AddWorkOrderAcceptanceToken  (2026-09-23)
-- ============================================================================
-- The secret behind a work order's acceptance link: WorkOrders.AcceptanceToken
-- (unique where set) and AcceptanceTokenIssuedAt. Minted by the api the first
-- time the purchase order is emailed; NULL for every existing order until it is
-- next emailed. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260923120000_AddWorkOrderAcceptanceToken.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the columns.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-work-order-acceptance-token.sql -b -o add-work-order-acceptance-token.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260923120000_AddWorkOrderAcceptanceToken')
BEGIN
    IF COL_LENGTH(N'[WorkOrders]', N'AcceptanceToken') IS NULL
        ALTER TABLE [WorkOrders] ADD [AcceptanceToken] nvarchar(64) NULL;
    IF COL_LENGTH(N'[WorkOrders]', N'AcceptanceTokenIssuedAt') IS NULL
        ALTER TABLE [WorkOrders] ADD [AcceptanceTokenIssuedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260923120000_AddWorkOrderAcceptanceToken')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WorkOrders_AcceptanceToken' AND object_id = OBJECT_ID(N'[WorkOrders]'))
        CREATE UNIQUE INDEX [IX_WorkOrders_AcceptanceToken] ON [WorkOrders] ([AcceptanceToken]) WHERE [AcceptanceToken] IS NOT NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260923120000_AddWorkOrderAcceptanceToken')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260923120000_AddWorkOrderAcceptanceToken', N'8.0.10');
END;
GO

COMMIT;
GO
