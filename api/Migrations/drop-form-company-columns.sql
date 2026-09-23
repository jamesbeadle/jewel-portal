-- ============================================================================
-- DropFormCompanyColumns  (2026-09-23)  — step 2 of 2 (CONTRACT)
-- ============================================================================
-- Removes the second-company columns the onboarding forms were built with,
-- once step 1 (add-compliance-document-is-from-a-form.sql) has run and the api
-- that no longer reads them is deployed:
--   ComplianceDocuments.FormCompany   after carrying any certificate filed from
--                                     a form since step 1 onto IsFromAForm
--   [Company] on FormPacks, FormInvites, FormSubmissions, FormUploads,
--   FormFolders, RightToWorkChecks and TrainingRecords, with its default
-- Refuses to run before step 1 (IsFromAForm missing). No rows are deleted.
--
-- Run this only AFTER the new api is deployed: the api before it writes these
-- columns.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i drop-form-company-columns.sql -b -o drop-form-company-columns.log
-- ============================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;
GO

IF COL_LENGTH(N'[ComplianceDocuments]', N'IsFromAForm') IS NULL
BEGIN
    RAISERROR (N'ComplianceDocuments.IsFromAForm is missing — run add-compliance-document-is-from-a-form.sql first.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;
GO

IF COL_LENGTH(N'[ComplianceDocuments]', N'FormCompany') IS NOT NULL
BEGIN
    EXEC sp_executesql N'UPDATE [ComplianceDocuments] SET [IsFromAForm] = 1 WHERE [FormCompany] IS NOT NULL AND [IsFromAForm] = 0';
    ALTER TABLE [ComplianceDocuments] DROP COLUMN [FormCompany];
END;
GO

IF COL_LENGTH(N'[FormPacks]', N'Company') IS NOT NULL
BEGIN
    DECLARE @df sysname = (SELECT dc.[name] FROM sys.default_constraints dc
                           JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
                           WHERE dc.[parent_object_id] = OBJECT_ID(N'[FormPacks]') AND c.[name] = N'Company');
    IF @df IS NOT NULL EXEC (N'ALTER TABLE [FormPacks] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [FormPacks] DROP COLUMN [Company];
END;
GO

IF COL_LENGTH(N'[FormInvites]', N'Company') IS NOT NULL
BEGIN
    DECLARE @df sysname = (SELECT dc.[name] FROM sys.default_constraints dc
                           JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
                           WHERE dc.[parent_object_id] = OBJECT_ID(N'[FormInvites]') AND c.[name] = N'Company');
    IF @df IS NOT NULL EXEC (N'ALTER TABLE [FormInvites] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [FormInvites] DROP COLUMN [Company];
END;
GO

IF COL_LENGTH(N'[FormSubmissions]', N'Company') IS NOT NULL
BEGIN
    DECLARE @df sysname = (SELECT dc.[name] FROM sys.default_constraints dc
                           JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
                           WHERE dc.[parent_object_id] = OBJECT_ID(N'[FormSubmissions]') AND c.[name] = N'Company');
    IF @df IS NOT NULL EXEC (N'ALTER TABLE [FormSubmissions] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [FormSubmissions] DROP COLUMN [Company];
END;
GO

IF COL_LENGTH(N'[FormUploads]', N'Company') IS NOT NULL
BEGIN
    DECLARE @df sysname = (SELECT dc.[name] FROM sys.default_constraints dc
                           JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
                           WHERE dc.[parent_object_id] = OBJECT_ID(N'[FormUploads]') AND c.[name] = N'Company');
    IF @df IS NOT NULL EXEC (N'ALTER TABLE [FormUploads] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [FormUploads] DROP COLUMN [Company];
END;
GO

IF COL_LENGTH(N'[FormFolders]', N'Company') IS NOT NULL
BEGIN
    DECLARE @df sysname = (SELECT dc.[name] FROM sys.default_constraints dc
                           JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
                           WHERE dc.[parent_object_id] = OBJECT_ID(N'[FormFolders]') AND c.[name] = N'Company');
    IF @df IS NOT NULL EXEC (N'ALTER TABLE [FormFolders] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [FormFolders] DROP COLUMN [Company];
END;
GO

IF COL_LENGTH(N'[RightToWorkChecks]', N'Company') IS NOT NULL
BEGIN
    DECLARE @df sysname = (SELECT dc.[name] FROM sys.default_constraints dc
                           JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
                           WHERE dc.[parent_object_id] = OBJECT_ID(N'[RightToWorkChecks]') AND c.[name] = N'Company');
    IF @df IS NOT NULL EXEC (N'ALTER TABLE [RightToWorkChecks] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [RightToWorkChecks] DROP COLUMN [Company];
END;
GO

IF COL_LENGTH(N'[TrainingRecords]', N'Company') IS NOT NULL
BEGIN
    DECLARE @df sysname = (SELECT dc.[name] FROM sys.default_constraints dc
                           JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
                           WHERE dc.[parent_object_id] = OBJECT_ID(N'[TrainingRecords]') AND c.[name] = N'Company');
    IF @df IS NOT NULL EXEC (N'ALTER TABLE [TrainingRecords] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [TrainingRecords] DROP COLUMN [Company];
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260923180000_DropFormCompanyColumns')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260923180000_DropFormCompanyColumns', N'8.0.10');
END;
GO

COMMIT;
GO
