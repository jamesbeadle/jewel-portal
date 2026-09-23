-- ============================================================================
-- AddComplianceDocumentIsFromAForm  (2026-09-23)  — step 1 of 2 (EXPAND)
-- ============================================================================
-- The onboarding forms are Jewel Bespoke Build's alone. AddOnboardingForms put
-- a Company column on seven forms tables and FormCompany on ComplianceDocuments
-- for a second Jewel company that has no place in this portal. This step makes
-- the api that no longer knows them safe to deploy:
--   ComplianceDocuments.IsFromAForm  whether a certificate came in on a form,
--                                    the only certificates the renewal chase
--                                    asks for; set from FormCompany
--   DF_<table>_Company               a default of 0 on the Company column of
--                                    FormPacks, FormInvites, FormSubmissions,
--                                    FormUploads, FormFolders,
--                                    RightToWorkChecks and TrainingRecords, so
--                                    the rows the new api inserts still go in
-- It then prints how many rows were ever marked for the second company
-- (Company = 1); every other row is Jewel Bespoke Build's. Nothing is deleted.
-- Step 2 (drop-form-company-columns.sql) drops FormCompany and the seven
-- Company columns once the new api is deployed.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors
-- api/Migrations/20260923170000_AddComplianceDocumentIsFromAForm.cs.
-- Run BEFORE the api deploys: the new api reads IsFromAForm. Safe to re-run.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-compliance-document-is-from-a-form.sql -b \
--          -o add-compliance-document-is-from-a-form.log
-- ============================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;
GO

IF COL_LENGTH(N'[ComplianceDocuments]', N'IsFromAForm') IS NULL
    ALTER TABLE [ComplianceDocuments] ADD [IsFromAForm] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

IF COL_LENGTH(N'[ComplianceDocuments]', N'FormCompany') IS NOT NULL
    EXEC sp_executesql N'UPDATE [ComplianceDocuments] SET [IsFromAForm] = 1 WHERE [FormCompany] IS NOT NULL AND [IsFromAForm] = 0';
GO

IF COL_LENGTH(N'[FormPacks]', N'Company') IS NOT NULL AND OBJECT_ID(N'[DF_FormPacks_Company]', N'D') IS NULL
    ALTER TABLE [FormPacks] ADD CONSTRAINT [DF_FormPacks_Company] DEFAULT 0 FOR [Company];
IF COL_LENGTH(N'[FormInvites]', N'Company') IS NOT NULL AND OBJECT_ID(N'[DF_FormInvites_Company]', N'D') IS NULL
    ALTER TABLE [FormInvites] ADD CONSTRAINT [DF_FormInvites_Company] DEFAULT 0 FOR [Company];
IF COL_LENGTH(N'[FormSubmissions]', N'Company') IS NOT NULL AND OBJECT_ID(N'[DF_FormSubmissions_Company]', N'D') IS NULL
    ALTER TABLE [FormSubmissions] ADD CONSTRAINT [DF_FormSubmissions_Company] DEFAULT 0 FOR [Company];
IF COL_LENGTH(N'[FormUploads]', N'Company') IS NOT NULL AND OBJECT_ID(N'[DF_FormUploads_Company]', N'D') IS NULL
    ALTER TABLE [FormUploads] ADD CONSTRAINT [DF_FormUploads_Company] DEFAULT 0 FOR [Company];
IF COL_LENGTH(N'[FormFolders]', N'Company') IS NOT NULL AND OBJECT_ID(N'[DF_FormFolders_Company]', N'D') IS NULL
    ALTER TABLE [FormFolders] ADD CONSTRAINT [DF_FormFolders_Company] DEFAULT 0 FOR [Company];
IF COL_LENGTH(N'[RightToWorkChecks]', N'Company') IS NOT NULL AND OBJECT_ID(N'[DF_RightToWorkChecks_Company]', N'D') IS NULL
    ALTER TABLE [RightToWorkChecks] ADD CONSTRAINT [DF_RightToWorkChecks_Company] DEFAULT 0 FOR [Company];
IF COL_LENGTH(N'[TrainingRecords]', N'Company') IS NOT NULL AND OBJECT_ID(N'[DF_TrainingRecords_Company]', N'D') IS NULL
    ALTER TABLE [TrainingRecords] ADD CONSTRAINT [DF_TrainingRecords_Company] DEFAULT 0 FOR [Company];
GO

IF COL_LENGTH(N'[FormPacks]', N'Company') IS NOT NULL AND COL_LENGTH(N'[ComplianceDocuments]', N'FormCompany') IS NOT NULL
    EXEC sp_executesql N'SELECT
        (SELECT COUNT(*) FROM [FormPacks] WHERE [Company] = 1)                    AS [PacksMarkedSecondCompany],
        (SELECT COUNT(*) FROM [FormInvites] WHERE [Company] = 1)                  AS [InvitesMarkedSecondCompany],
        (SELECT COUNT(*) FROM [FormSubmissions] WHERE [Company] = 1)              AS [FormsMarkedSecondCompany],
        (SELECT COUNT(*) FROM [FormUploads] WHERE [Company] = 1)                  AS [UploadsMarkedSecondCompany],
        (SELECT COUNT(*) FROM [FormFolders] WHERE [Company] = 1)                  AS [FoldersMarkedSecondCompany],
        (SELECT COUNT(*) FROM [RightToWorkChecks] WHERE [Company] = 1)            AS [ChecksMarkedSecondCompany],
        (SELECT COUNT(*) FROM [TrainingRecords] WHERE [Company] = 1)              AS [TrainingMarkedSecondCompany],
        (SELECT COUNT(*) FROM [ComplianceDocuments] WHERE [FormCompany] = 1)      AS [CertificatesMarkedSecondCompany],
        (SELECT COUNT(*) FROM [ComplianceDocuments] WHERE [IsFromAForm] = 1)      AS [CertificatesFromAForm]';
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260923170000_AddComplianceDocumentIsFromAForm')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260923170000_AddComplianceDocumentIsFromAForm', N'8.0.10');
END;
GO

COMMIT;
GO
