-- ============================================================================
-- WidenWrittenTextColumns  (2026-09-24)
-- ============================================================================
-- Nigel could not approve V33 on Abbot Road: line A10's description was 550
-- characters and ValuationLineItems.Description was nvarchar(512). The same
-- ceiling sat on every column people write sentences into, so 82 columns
-- across 63 tables go to nvarchar(max), and
-- ReconciliationPackages.Name goes from 128 to 256 to take a work order's title.
-- Widening only: no data changes; no index touches these columns.
--
-- A column added with a default ("") carries a default constraint, and SQL Server
-- refuses to change the type of a column a constraint depends on (Msg 5074 — the
-- first run on 2026-09-24 stopped at DF__BidPackag__Speci…, and rolled back whole).
-- So each column's default is dropped, the column widened, and the SAME default put
-- back under the SAME name; auto-created statistics on it (which block the change the
-- same way) are dropped and SQL Server recreates them when next needed. XACT_ABORT
-- makes any failure roll back everything: the database is either fully migrated or
-- untouched.
--
-- House-style scoped script (see CLAUDE.md "Database migrations"): applies the
-- migration directly and records its id in __EFMigrationsHistory so EF never
-- re-applies it. Mirrors api/Migrations/20260924160000_WidenWrittenTextColumns.cs.
-- Safe to apply BEFORE or WITH the deploy.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--          -i widen-written-text-columns.sql -b -o widen-written-text-columns.log
-- ============================================================================

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924160000_WidenWrittenTextColumns')
BEGIN
    DECLARE @columns TABLE (TableName sysname, ColumnName sysname, NewType nvarchar(32), Nullability nvarchar(8));
    INSERT INTO @columns (TableName, ColumnName, NewType, Nullability) VALUES
    (N'ArchitectInstructions', N'Notes', N'nvarchar(max)', N'NULL'),
    (N'BidPackageLineItems', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'BidPackages', N'SpecificationSummary', N'nvarchar(max)', N'NOT NULL'),
    (N'BoqLineItems', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'ClaimLines', N'Comments', N'nvarchar(max)', N'NOT NULL'),
    (N'ClaimLines', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'CompanyRegisterItems', N'Notes', N'nvarchar(max)', N'NOT NULL'),
    (N'ContraCharges', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'Dayworks', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'Defects', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'DrawingIssueRecords', N'Notes', N'nvarchar(max)', N'NOT NULL'),
    (N'DrivingLicenceChecks', N'Note', N'nvarchar(max)', N'NOT NULL'),
    (N'Eots', N'Reason', N'nvarchar(max)', N'NOT NULL'),
    (N'HsAuditItems', N'Findings', N'nvarchar(max)', N'NOT NULL'),
    (N'HsAudits', N'FurtherComments', N'nvarchar(max)', N'NOT NULL'),
    (N'HsRecords', N'Summary', N'nvarchar(max)', N'NOT NULL'),
    (N'ImagineRounds', N'Brief', N'nvarchar(max)', N'NOT NULL'),
    (N'InventoryItems', N'LocationDetails', N'nvarchar(max)', N'NOT NULL'),
    (N'InventoryItems', N'ProductDetails', N'nvarchar(max)', N'NOT NULL'),
    (N'KpiEmails', N'Note', N'nvarchar(max)', N'NOT NULL'),
    (N'LabourChaseDismissals', N'Reason', N'nvarchar(max)', N'NOT NULL'),
    (N'LabourSettlementVariances', N'Reason', N'nvarchar(max)', N'NOT NULL'),
    (N'LadClaims', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'LeadActivities', N'Summary', N'nvarchar(max)', N'NOT NULL'),
    (N'LeadEstimateLines', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'LeadEstimates', N'Exclusions', N'nvarchar(max)', N'NOT NULL'),
    (N'LeadEstimates', N'Notes', N'nvarchar(max)', N'NOT NULL'),
    (N'LeadEstimates', N'Scope', N'nvarchar(max)', N'NOT NULL'),
    (N'Leads', N'LostReason', N'nvarchar(max)', N'NULL'),
    (N'Leads', N'Notes', N'nvarchar(max)', N'NOT NULL'),
    (N'Leads', N'Summary', N'nvarchar(max)', N'NOT NULL'),
    (N'MobilisationItems', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'PolicyDocuments', N'Summary', N'nvarchar(max)', N'NOT NULL'),
    (N'ProgrammeVariationEffects', N'Note', N'nvarchar(max)', N'NOT NULL'),
    (N'ProgressReports', N'Introduction', N'nvarchar(max)', N'NOT NULL'),
    (N'ProgressReports', N'UpcomingWorks', N'nvarchar(max)', N'NOT NULL'),
    (N'ProgressReports', N'WorkCompleted', N'nvarchar(max)', N'NOT NULL'),
    (N'ProjectContractAmendments', N'Notes', N'nvarchar(max)', N'NULL'),
    (N'ProjectContracts', N'BespokeDeviations', N'nvarchar(max)', N'NULL'),
    (N'QsAccruals', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'QuoteLineItems', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'Quotes', N'Notes', N'nvarchar(max)', N'NOT NULL'),
    (N'RequestItems', N'Response', N'nvarchar(max)', N'NULL'),
    (N'RequestMessages', N'Body', N'nvarchar(max)', N'NOT NULL'),
    (N'Requests', N'ClientNotes', N'nvarchar(max)', N'NULL'),
    (N'Requests', N'InternalNotes', N'nvarchar(max)', N'NULL'),
    (N'Requests', N'RelatedDrawingSpec', N'nvarchar(max)', N'NULL'),
    (N'RightToWorkChecks', N'Notes', N'nvarchar(max)', N'NOT NULL'),
    (N'SalesProposals', N'DeclineReason', N'nvarchar(max)', N'NULL'),
    (N'SalesStrategies', N'Brief', N'nvarchar(max)', N'NOT NULL'),
    (N'SalesStrategies', N'Evidence', N'nvarchar(max)', N'NOT NULL'),
    (N'SalesStrategies', N'Hypothesis', N'nvarchar(max)', N'NOT NULL'),
    (N'SalesStrategies', N'Proposition', N'nvarchar(max)', N'NOT NULL'),
    (N'SiteInstructions', N'Instruction', N'nvarchar(max)', N'NOT NULL'),
    (N'SitePhotos', N'ArchiveNote', N'nvarchar(max)', N'NOT NULL'),
    (N'SiteReports', N'Narrative', N'nvarchar(max)', N'NOT NULL'),
    (N'SubcontractorVariationRequests', N'RejectionReason', N'nvarchar(max)', N'NOT NULL'),
    (N'TenderEnquiries', N'DecisionNote', N'nvarchar(max)', N'NOT NULL'),
    (N'TenderEnquiries', N'ScopeSummary', N'nvarchar(max)', N'NOT NULL'),
    (N'Timesheets', N'RejectionReason', N'nvarchar(max)', N'NOT NULL'),
    (N'TodoItems', N'Notes', N'nvarchar(max)', N'NOT NULL'),
    (N'UsefulInformationNotes', N'Body', N'nvarchar(max)', N'NOT NULL'),
    (N'ValuationInvoiceEvents', N'Note', N'nvarchar(max)', N'NOT NULL'),
    (N'ValuationInvoices', N'RejectionReason', N'nvarchar(max)', N'NULL'),
    (N'ValuationLineItems', N'Comments', N'nvarchar(max)', N'NOT NULL'),
    (N'ValuationLineItems', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'VariationOrderMessages', N'Body', N'nvarchar(max)', N'NOT NULL'),
    (N'VariationOrderQuotes', N'CommercialBasis', N'nvarchar(max)', N'NULL'),
    (N'VariationOrderQuotes', N'Exclusions', N'nvarchar(max)', N'NULL'),
    (N'VariationOrderQuotes', N'ProgrammeImpact', N'nvarchar(max)', N'NULL'),
    (N'VatAnalyses', N'Notes', N'nvarchar(max)', N'NOT NULL'),
    (N'WalkRoundNotes', N'Notes', N'nvarchar(max)', N'NOT NULL'),
    (N'WeeklyCashflowItems', N'Notes', N'nvarchar(max)', N'NULL'),
    (N'WorkOrderLines', N'Description', N'nvarchar(max)', N'NOT NULL'),
    (N'WorkOrders', N'ProgrammeNotes', N'nvarchar(max)', N'NOT NULL'),
    (N'WorkOrders', N'Scope', N'nvarchar(max)', N'NOT NULL'),
    (N'WorkerAbsences', N'Note', N'nvarchar(max)', N'NOT NULL'),
    (N'WorkerSettlementLines', N'Note', N'nvarchar(max)', N'NOT NULL'),
    (N'WorkstationActions', N'Note', N'nvarchar(max)', N'NOT NULL'),
    (N'XeroDisputeMessages', N'Body', N'nvarchar(max)', N'NOT NULL'),
    (N'ReconciliationPackages', N'Name', N'nvarchar(256)', N'NOT NULL'),
    (N'XeroLedgerLines', N'Description', N'nvarchar(max)', N'NULL'),
    (N'XeroLedgerLines', N'Note', N'nvarchar(max)', N'NULL');

    DECLARE @table sysname, @column sysname, @newType nvarchar(32), @nullability nvarchar(8);
    DECLARE @defaultName sysname, @defaultDefinition nvarchar(max), @statisticName sysname, @sql nvarchar(max);

    DECLARE widening CURSOR LOCAL FAST_FORWARD FOR
        SELECT TableName, ColumnName, NewType, Nullability FROM @columns;
    OPEN widening;
    FETCH NEXT FROM widening INTO @table, @column, @newType, @nullability;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @defaultName = NULL;
        SET @defaultDefinition = NULL;
        SELECT @defaultName = [d].[name], @defaultDefinition = [d].[definition]
        FROM [sys].[default_constraints] [d]
        JOIN [sys].[columns] [c] ON [c].[object_id] = [d].[parent_object_id] AND [c].[column_id] = [d].[parent_column_id]
        WHERE [d].[parent_object_id] = OBJECT_ID(QUOTENAME(@table)) AND [c].[name] = @column;

        IF @defaultName IS NOT NULL
        BEGIN
            SET @sql = N'ALTER TABLE ' + QUOTENAME(@table) + N' DROP CONSTRAINT ' + QUOTENAME(@defaultName) + N';';
            EXEC sp_executesql @sql;
        END;

        DECLARE statistics_on_column CURSOR LOCAL FAST_FORWARD FOR
            SELECT [s].[name]
            FROM [sys].[stats] [s]
            JOIN [sys].[stats_columns] [sc] ON [sc].[object_id] = [s].[object_id] AND [sc].[stats_id] = [s].[stats_id]
            JOIN [sys].[columns] [c] ON [c].[object_id] = [sc].[object_id] AND [c].[column_id] = [sc].[column_id]
            WHERE [s].[object_id] = OBJECT_ID(QUOTENAME(@table)) AND [c].[name] = @column AND [s].[auto_created] = 1;
        OPEN statistics_on_column;
        FETCH NEXT FROM statistics_on_column INTO @statisticName;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @sql = N'DROP STATISTICS ' + QUOTENAME(@table) + N'.' + QUOTENAME(@statisticName) + N';';
            EXEC sp_executesql @sql;
            FETCH NEXT FROM statistics_on_column INTO @statisticName;
        END;
        CLOSE statistics_on_column;
        DEALLOCATE statistics_on_column;

        SET @sql = N'ALTER TABLE ' + QUOTENAME(@table) + N' ALTER COLUMN ' + QUOTENAME(@column) + N' ' + @newType + N' ' + @nullability + N';';
        EXEC sp_executesql @sql;

        IF @defaultName IS NOT NULL
        BEGIN
            SET @sql = N'ALTER TABLE ' + QUOTENAME(@table) + N' ADD CONSTRAINT ' + QUOTENAME(@defaultName)
                     + N' DEFAULT ' + @defaultDefinition + N' FOR ' + QUOTENAME(@column) + N';';
            EXEC sp_executesql @sql;
        END;

        PRINT N'Widened ' + @table + N'.' + @column + N' to ' + @newType + ISNULL(N' (default ' + @defaultName + N' kept)', N'');
        FETCH NEXT FROM widening INTO @table, @column, @newType, @nullability;
    END;
    CLOSE widening;
    DEALLOCATE widening;
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260924160000_WidenWrittenTextColumns')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924160000_WidenWrittenTextColumns', N'8.0.10');
END;
GO

COMMIT;
GO

PRINT N'WidenWrittenTextColumns applied.';
GO
