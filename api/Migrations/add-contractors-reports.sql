-- ============================================================================
-- AddContractorsReports  (2026-09-16)
-- ============================================================================
-- The weekly Contractor's Report (the FD's spec of 2026-09-15, change 4): one
-- table holding what a person ENTERS per report — header fields, Look Ahead,
-- Neighbours, H&S, the Building Control liaison line, the subcontractors'
-- attendance and the chosen progress updates. Every section read from the
-- register is composed at build time and never stored. One report per project
-- per period. Additive only; no FKs, as everywhere else.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260916160000_AddContractorsReports.cs.
-- Safe to apply BEFORE or WITH the deploy; must be applied before the deployed
-- api reads the table.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i add-contractors-reports.sql -b -o add-contractors-reports.log
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916160000_AddContractorsReports')
BEGIN
    IF OBJECT_ID(N'[ContractorsReports]', N'U') IS NULL
    BEGIN
        CREATE TABLE [ContractorsReports] (
            [ContractorsReportId] nvarchar(64) NOT NULL,
            [ProjectId] nvarchar(64) NOT NULL,
            [Number] int NOT NULL,
            [PeriodStart] date NOT NULL,
            [PeriodEnd] date NOT NULL,
            [ValuationNumber] nvarchar(32) NOT NULL,
            [ProgrammeReference] nvarchar(256) NOT NULL,
            [PreparedByName] nvarchar(256) NOT NULL,
            [IssuedTo] nvarchar(256) NOT NULL,
            [DateOfIssue] date NOT NULL,
            [LookAheadJson] nvarchar(max) NOT NULL,
            [Neighbours] nvarchar(4000) NOT NULL,
            [HealthAndSafety] nvarchar(4000) NOT NULL,
            [BuildingControlLiaison] nvarchar(2000) NOT NULL,
            [AttendanceJson] nvarchar(max) NOT NULL,
            [SelectedUpdateIdsJson] nvarchar(max) NOT NULL,
            [CreatedByEmail] nvarchar(256) NOT NULL,
            [CreatedAt] datetimeoffset NOT NULL,
            [UpdatedAt] datetimeoffset NOT NULL,
            CONSTRAINT [PK_ContractorsReports] PRIMARY KEY ([ContractorsReportId])
        );
        CREATE UNIQUE INDEX [IX_ContractorsReports_ProjectId_PeriodEnd] ON [ContractorsReports] ([ProjectId], [PeriodEnd]);
    END;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260916160000_AddContractorsReports')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916160000_AddContractorsReports', N'8.0.10');
END;
GO

COMMIT;
GO
