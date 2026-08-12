-- Backfill BPI numbers for bid packages that never had one minted.
--
-- Packages created under a variation order (AddBidPackageToVoq, removed 2026-08-12 when bid
-- packages were separated from the VO quoting process) were created with Number = 0, so they
-- render an id-derived reference instead of BPI-nnnn and their emails can't be tagged with a
-- stable stem. This assigns each un-numbered package the next sequential number, in CreatedAt
-- order, above the current maximum — matching how new packages take MAX(Number)+1 at insert.
--
-- Data fix only (no schema change) — run once via sqlcmd, per CLAUDE.md:
--   sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin \
--     -i scripts/backfill-bidpackage-numbers.sql -b -o backfill-bidpackage-numbers.log
--
-- Idempotent: a second run finds no Number = 0 rows and changes nothing.

SET NOCOUNT ON;

DECLARE @maxNumber int = (SELECT ISNULL(MAX(Number), 0) FROM BidPackages);

WITH unnumbered AS (
    SELECT BidPackageId,
           ROW_NUMBER() OVER (ORDER BY CreatedAt, BidPackageId) AS rn
    FROM BidPackages
    WHERE Number = 0
)
UPDATE bp
SET bp.Number = @maxNumber + u.rn
FROM BidPackages bp
INNER JOIN unnumbered u ON u.BidPackageId = bp.BidPackageId;

PRINT CONCAT('Backfilled ', @@ROWCOUNT, ' bid package number(s).');
