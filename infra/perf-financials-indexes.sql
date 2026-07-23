/*
  Performance indexes for the project Financials tab (GetProjectFinancialSummary).
  Safe to run repeatedly against Azure SQL (each CREATE is guarded by an existence check).

  Why: the financial-summary query filters/joins three tables on columns that had no
  supporting index, so those access paths were table scans that grow with the data -
  a prime cause of the query drifting past the Static Web Apps managed-functions gateway
  timeout (~45s) and returning a 504.

  These names follow EF's IX_<Table>_<Cols> convention. If you later encode the same
  indexes in an EF migration, use these exact names (and record the migration as applied,
  since the objects already exist) so the two paths don't collide.

  Apply: Azure Portal > SQL database > Query editor, or sqlcmd against the prod database.
*/

-- 1) XeroLineTimesheetCovers.XeroLedgerLineId
--    Probed by the NOT EXISTS "covered-by-timesheets" exclusion on three ledger-line
--    reads (actuals, split actuals, packaged direct costs). Without it every probe scans.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_XeroLineTimesheetCovers_XeroLedgerLineId'
                 AND object_id = OBJECT_ID(N'dbo.XeroLineTimesheetCovers'))
BEGIN
    CREATE INDEX IX_XeroLineTimesheetCovers_XeroLedgerLineId
        ON dbo.XeroLineTimesheetCovers (XeroLedgerLineId);
END;
GO

-- 2) WorkOrderLines.WorkOrderId
--    Joined/filtered by WorkOrderId in the cost apportionment and packaged-WO rollups,
--    which group by CostCode and sum LineTotal. INCLUDE makes those reads covering.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_WorkOrderLines_WorkOrderId'
                 AND object_id = OBJECT_ID(N'dbo.WorkOrderLines'))
BEGIN
    CREATE INDEX IX_WorkOrderLines_WorkOrderId
        ON dbo.WorkOrderLines (WorkOrderId)
        INCLUDE (CostCode, LineTotal);
END;
GO

-- 3) Timesheets(ProjectId, Status)
--    Approved-labour and pending-labour reads filter by ProjectId + Status and group by
--    CostCode. INCLUDE covers both the cost-sum and the pending hours*rate path.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_Timesheets_ProjectId_Status'
                 AND object_id = OBJECT_ID(N'dbo.Timesheets'))
BEGIN
    CREATE INDEX IX_Timesheets_ProjectId_Status
        ON dbo.Timesheets (ProjectId, Status)
        INCLUDE (CostCode, CostAmount, WorkerId, Hours);
END;
GO
