-- ============================================================================
-- DEDUPE the cost-code master and lock code uniqueness in
-- ----------------------------------------------------------------------------
-- The CostCenters table carries rows from three seed generations (JBB-*
-- buckets, the numeric 00001..00137 list, the trade-prefixed master). The
-- first two generations used DIFFERENT CostCenterIds for the same Code
-- (cc-0010 vs cc-00001 for 00001, ...), and MERGE matches on id, so re-seeding
-- inserted second rows instead of updating -- at least codes 00001, 00002,
-- 00003 and 00005 exist twice (and the first generation itself seeded
-- 00006-12 twice). Any code->name dictionary over the table then throws
-- "An item with the same key has already been added" -- this is what broke
-- the assistant's get_cost_code_budgets read on 2026-08-29.
--
-- This script:
--   1. reports the duplicated codes
--   2. for each duplicated code KEEPS one row (active first, then the row
--      whose id follows the current cc-<code> convention) and renames the
--      superseded rows' Code to <code>-dup1, -dup2, ... (rows are never
--      deleted -- CostCentreLinkProvider record links reference CostCenterId,
--      so ids must survive; renamed rows are forced inactive)
--   3. adds a unique index on Code so a future reseed cannot reintroduce this
--
-- Idempotent and transactional; verifies zero duplicates remain before commit.
--
-- Run:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--     -i dedupe-cost-centers.sql -b -o dedupe-cost-centers.log
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

SELECT N'Duplicated codes before' AS [Report], [Code], COUNT(*) AS [Rows]
FROM [dbo].[CostCenters]
GROUP BY [Code]
HAVING COUNT(*) > 1;

BEGIN TRANSACTION;

WITH ranked AS (
    SELECT [CostCenterId], [Code], [IsActive],
           ROW_NUMBER() OVER (
               PARTITION BY [Code]
               ORDER BY [IsActive] DESC,
                        CASE WHEN [CostCenterId] = N'cc-' + [Code] THEN 0 ELSE 1 END,
                        [CostCenterId]
           ) AS [DuplicateRank]
    FROM [dbo].[CostCenters]
)
UPDATE ranked
SET [Code] = LEFT([Code], 32 - LEN(N'-dup' + CAST([DuplicateRank] - 1 AS nvarchar(4))))
             + N'-dup' + CAST([DuplicateRank] - 1 AS nvarchar(4)),
    [IsActive] = 0
WHERE [DuplicateRank] > 1;

IF EXISTS (SELECT 1 FROM [dbo].[CostCenters] GROUP BY [Code] HAVING COUNT(*) > 1)
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, 'Duplicate codes remain after the rename -- rolled back.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE [name] = N'UX_CostCenters_Code'
                 AND [object_id] = OBJECT_ID(N'[dbo].[CostCenters]'))
    CREATE UNIQUE INDEX [UX_CostCenters_Code] ON [dbo].[CostCenters]([Code]);

COMMIT TRANSACTION;

SELECT N'Superseded rows renamed' AS [Report], [CostCenterId], [Code], [Name], [IsActive]
FROM [dbo].[CostCenters]
WHERE [Code] LIKE N'%-dup[0-9]'
ORDER BY [Code];

SELECT N'Duplicated codes after (must be none)' AS [Report], [Code], COUNT(*) AS [Rows]
FROM [dbo].[CostCenters]
GROUP BY [Code]
HAVING COUNT(*) > 1;
