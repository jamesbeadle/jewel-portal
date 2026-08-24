-- One-off cleanup for the 2026-08-22 Control Centre apply-retry incident: the apply created the
-- draft work order and THEN failed tagging the email, so every retry of the red error raised
-- another identical draft (the run of £1,800 "Uplift in Change in Render" orders against MGN
-- Drywall). See the header of CreateWorkOrderFromMessageHandler.cs for the code-side fix.
--
-- What it does: finds groups of orders sharing project, subcontractor, title and value, keeps
-- one — the released/complete order if the group has one, else the earliest draft — and sets
-- every OTHER draft in the group created within 48 hours of the keeper to Rejected (status 4),
-- which is terminal and counts nowhere, so the Financials tab's committed figures deflate back
-- to one order per group. Prints the rows first, then updates them. Data only — no schema.
-- Re-runnable: a second run finds nothing left to reject.

SET NOCOUNT ON;

IF OBJECT_ID('tempdb..#duplicates') IS NOT NULL DROP TABLE #duplicates;

;WITH ranked AS (
    SELECT WorkOrderId, ProjectId, SubcontractorId, Title, Value, Status, CreatedAt, AwardedByEmail,
           -- Keeper first: a released or complete order outranks any draft; otherwise the
           -- earliest draft of the group is the one the first apply attempt created.
           ROW_NUMBER() OVER (
               PARTITION BY ProjectId, SubcontractorId, Title, Value
               ORDER BY CASE WHEN Status IN (1, 2) THEN 0 ELSE 1 END, CreatedAt, WorkOrderId) AS Position,
           FIRST_VALUE(CreatedAt) OVER (
               PARTITION BY ProjectId, SubcontractorId, Title, Value
               ORDER BY CASE WHEN Status IN (1, 2) THEN 0 ELSE 1 END, CreatedAt, WorkOrderId
               ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) AS KeeperCreatedAt
    FROM WorkOrders
    WHERE Status IN (0, 1, 2)   -- Draft, Released, Complete; already-rejected/cancelled ignored
)
SELECT WorkOrderId, ProjectId, SubcontractorId, Title, Value, CreatedAt, AwardedByEmail
INTO #duplicates
FROM ranked
WHERE Position > 1
  AND Status = 0   -- only drafts are ever rejected; live orders are never touched
  AND ABS(DATEDIFF(HOUR, KeeperCreatedAt, CreatedAt)) <= 48;   -- retries, not later re-raises

PRINT 'Duplicate draft work orders about to be rejected (keeper of each group is kept):';
SELECT d.WorkOrderId, d.ProjectId, d.SubcontractorId, d.Title, d.Value, d.CreatedAt, d.AwardedByEmail
FROM #duplicates d
ORDER BY d.ProjectId, d.Title, d.CreatedAt;

UPDATE wo
SET wo.Status = 4   -- Rejected: the terminal answer to a draft that shouldn't proceed
FROM WorkOrders wo
JOIN #duplicates d ON d.WorkOrderId = wo.WorkOrderId
WHERE wo.Status = 0;   -- belt-and-braces: only if still a draft at the moment this runs

PRINT CONCAT(CAST(@@ROWCOUNT AS varchar(10)), ' duplicate draft work order(s) rejected.');

DROP TABLE #duplicates;
