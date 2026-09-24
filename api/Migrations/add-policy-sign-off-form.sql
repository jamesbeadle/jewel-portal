-- ============================================================================
-- AddPolicySignOffForm  (2026-09-24)
-- ============================================================================
-- The Policy sign-off form: a published policy revision carries its
-- declaration and the PDF people read; a form link carries the revision it
-- was sent for; a sign-off made through the form records the person's name,
-- company, position, the link and the form. Additive only.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260924200000_AddPolicySignOffForm.cs.
-- Apply BEFORE or WITH the deploy.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-policy-sign-off-form.sql -b -o add-policy-sign-off-form.log
-- ============================================================================

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924200000_AddPolicySignOffForm')
BEGIN
    IF COL_LENGTH(N'PolicyDocuments', N'Declaration') IS NULL
        ALTER TABLE [PolicyDocuments] ADD [Declaration] nvarchar(max) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'PolicyDocuments', N'FileName') IS NULL
        ALTER TABLE [PolicyDocuments] ADD [FileName] nvarchar(256) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'PolicyDocuments', N'FileBlobRef') IS NULL
        ALTER TABLE [PolicyDocuments] ADD [FileBlobRef] nvarchar(512) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'PolicySignOffs', N'RecipientName') IS NULL
        ALTER TABLE [PolicySignOffs] ADD [RecipientName] nvarchar(256) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'PolicySignOffs', N'CompanyName') IS NULL
        ALTER TABLE [PolicySignOffs] ADD [CompanyName] nvarchar(256) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'PolicySignOffs', N'Position') IS NULL
        ALTER TABLE [PolicySignOffs] ADD [Position] nvarchar(256) NOT NULL DEFAULT N'';
    IF COL_LENGTH(N'PolicySignOffs', N'FormInviteId') IS NULL
        ALTER TABLE [PolicySignOffs] ADD [FormInviteId] nvarchar(64) NULL;
    IF COL_LENGTH(N'PolicySignOffs', N'FormSubmissionId') IS NULL
        ALTER TABLE [PolicySignOffs] ADD [FormSubmissionId] nvarchar(64) NULL;
    IF COL_LENGTH(N'FormInvites', N'PolicyDocumentId') IS NULL
        ALTER TABLE [FormInvites] ADD [PolicyDocumentId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924200000_AddPolicySignOffForm')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924200000_AddPolicySignOffForm', N'8.0.10');
END;
GO

COMMIT;
GO
