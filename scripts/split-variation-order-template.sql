-- ============================================================================
-- TEMPLATE: split one Variation Order's single valuation line into detail lines
-- ----------------------------------------------------------------------------
-- Copy this file per split (e.g. scripts/split-byfrance-v13.sql), fill the two
-- EDIT blocks, run once with sqlcmd:
--
--   sqlcmd -S <server> -d <db> -U <user> -i scripts/split-<project>-vNN.sql -b
--
-- WHY THIS EXISTS: do NOT re-run the full project seed files to apply a split.
-- Their MERGEs reset every field -- including CostCode -- on EVERY valuation
-- line and VO record, wiping cost-centre reassignments made in the app via
-- the Financials tab (SetValuationLineCostCentre). This script touches ONLY
-- the VO being split:
--   * deletes its single net line + that line's claim line
--   * inserts the detail lines (ids <old-id>a, <old-id>b, ...)
--   * shifts later DisplayOrders so report ordering stays sequential
--   * recreates the claim lines per detail line, preserving total claimed
-- It never writes to other valuation lines, VariationOrders / VOQs (the VO
-- record stays a single record at the full net, per the V48/V06/V42/V10
-- precedent), CostCodeBudgets or CostCentreCostProgress -- so changes made in
-- the app elsewhere survive by construction.
--
-- GUARDS: the script aborts (and rolls back -- everything is one transaction)
-- if the DB no longer matches what the split was written against: net value
-- changed, claimed value changed (someone edited the claim %), or a new-line
-- id already exists. If the old line is already gone it no-ops, so an
-- accidental second run is harmless.
--
-- AFTER RUNNING: mirror the same split in the master seed files
-- (seed-<project>-variations.sql / seed-<project>-claim18.sql) so a fresh
-- environment matches prod -- but treat those files as bootstrap-only and
-- never run them against prod again.
-- ============================================================================
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRAN;

------------------------------------------------------------------- EDIT 1 ---
-- The line being split, and its CURRENT net (check the app / DB, not the old
-- workbook -- if the VO was revalued since, this guard is what saves you).
DECLARE @OldLineId   nvarchar(64)  = N'bf-vo-vNN';
DECLARE @ExpectedNet decimal(18,4) = 0.0000;
-------------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM dbo.ValuationLineItems WHERE ValuationLineItemId = @OldLineId)
BEGIN
    PRINT 'Old line already gone -- split appears to have run. Nothing to do.';
    COMMIT TRAN;
    RETURN;
END;

DECLARE @Project      nvarchar(64), @Net decimal(18,4), @Order int,
        @ElementType  int, @VariationRef nvarchar(32),
        @SectionCode  nvarchar(64), @SectionName nvarchar(256);
SELECT @Project = ProjectId, @Net = LineAmount, @Order = DisplayOrder,
       @ElementType = ElementType, @VariationRef = VariationRef,
       @SectionCode = SectionCode, @SectionName = SectionName
FROM dbo.ValuationLineItems
WHERE ValuationLineItemId = @OldLineId;

IF @Net <> @ExpectedNet
    THROW 50001, 'Line net differs from expected -- the VO was revalued since this script was written. Re-check the split.', 1;

------------------------------------------------------------------- EDIT 2 ---
-- The detail lines. Conventions (matching the V48/V06/V42/V10 splits):
--   * ids           : <old-id>a, <old-id>b, ...
--   * claim ids     : follow the claim file naming, e.g. bf-cl18-vo-vNNa
--   * LineType      : 0=Priced  1=ProvisionalSum  2=Omit  3=Declined  4=Tbc
--                     (net > 0 -> 0, net < 0 -> 2)
--   * CostCode      : CHECK THE APP FIRST -- if the VO was recoded via the
--                     Financials modal, use the CURRENT centre, not the seed's
--   * Amount        : the line nets; they must sum to @ExpectedNet
--   * ClaimedNow    : what is claimed against each new line; must sum to the
--                     amount currently claimed in the DB (usually the split of
--                     the old line's claimed value per the workbook)
DECLARE @NewLines TABLE (
    Seq         int            NOT NULL,
    LineId      nvarchar(64)   NOT NULL,
    ClaimLineId nvarchar(64)   NOT NULL,
    Title       nvarchar(1000) NOT NULL,
    LineType    int            NOT NULL,
    CostCode    nvarchar(32)   NOT NULL,
    Amount      decimal(18,4)  NOT NULL,
    ClaimedNow  decimal(18,2)  NOT NULL);
INSERT INTO @NewLines VALUES
    (1, N'bf-vo-vNNa', N'bf-cl18-vo-vNNa', N'First detail line description',  0, N'CODE-A', 0.0000, 0.00),
    (2, N'bf-vo-vNNb', N'bf-cl18-vo-vNNb', N'Second detail line description', 2, N'CODE-B', 0.0000, 0.00);
-------------------------------------------------------------------------------

-- Guards --------------------------------------------------------------------
IF (SELECT SUM(Amount) FROM @NewLines) <> @Net
    THROW 50002, 'New line amounts do not sum to the old line''s net.', 1;

IF EXISTS (SELECT 1 FROM dbo.ValuationLineItems
           WHERE ValuationLineItemId IN (SELECT LineId FROM @NewLines))
    THROW 50003, 'One of the new line ids already exists.', 1;

IF (SELECT COUNT(DISTINCT ValuationClaimId) FROM dbo.ClaimLines
    WHERE ValuationLineItemId = @OldLineId) > 1
    THROW 50004, 'More than one claim references this line -- extend this script for multi-claim splits before running.', 1;

DECLARE @ClaimedTotal decimal(18,2) =
    (SELECT COALESCE(SUM(CumulativeClaimed), 0) FROM dbo.ClaimLines
     WHERE ValuationLineItemId = @OldLineId);
IF (SELECT SUM(ClaimedNow) FROM @NewLines) <> @ClaimedTotal
    THROW 50005, 'Per-line claimed values do not sum to what is currently claimed in the DB -- the claim % was edited since this script was written. Re-check.', 1;

DECLARE @ClaimId nvarchar(64) =
    (SELECT TOP 1 ValuationClaimId FROM dbo.ClaimLines
     WHERE ValuationLineItemId = @OldLineId);

-- Apply ---------------------------------------------------------------------
-- Make room in the display order (only lines AFTER the split point move).
UPDATE dbo.ValuationLineItems
SET DisplayOrder = DisplayOrder + (SELECT COUNT(*) - 1 FROM @NewLines)
WHERE ProjectId = @Project AND ElementType = @ElementType AND DisplayOrder > @Order;

-- Retire the single net line (claim line first -- it references the line).
DELETE FROM dbo.ClaimLines         WHERE ValuationLineItemId = @OldLineId;
DELETE FROM dbo.ValuationLineItems WHERE ValuationLineItemId = @OldLineId;

-- Detail lines: qty 1 x rate = net, unit 'item' (per the split precedent).
INSERT INTO dbo.ValuationLineItems
    (ValuationLineItemId, ProjectId, ElementType, SectionCode, SectionName,
     VariationRef, VariationTitle, LineType, CostCode, Description, Unit,
     Quantity, Rate, LineAmount, Comments, DisplayOrder)
SELECT LineId, @Project, @ElementType, @SectionCode, @SectionName,
       @VariationRef, Title, LineType, CostCode, N'', N'item',
       1.0000, Amount, Amount, N'', @Order + Seq - 1
FROM @NewLines;

-- Claim lines: % is derived from the claimed value so % x net reproduces it
-- (weighted lines may legitimately fall outside 0-100, as elsewhere).
IF @ClaimId IS NOT NULL
    INSERT INTO dbo.ClaimLines
        (ClaimLineId, ValuationClaimId, ValuationLineItemId,
         PercentComplete, CumulativeClaimed, PeriodIncrement)
    SELECT ClaimLineId, @ClaimId, LineId,
           CASE WHEN Amount = 0 THEN 0
                ELSE ROUND(ClaimedNow / Amount * 100.0, 4) END,
           ClaimedNow, ClaimedNow
    FROM @NewLines;

COMMIT TRAN;

-- Post-split sanity: project-wide variation net and claimed totals must be
-- exactly what they were before the split.
SELECT
    (SELECT COUNT(*) FROM dbo.ValuationLineItems
      WHERE ProjectId = @Project AND ElementType = @ElementType)                    AS VariationLines,
    (SELECT SUM(LineAmount) FROM dbo.ValuationLineItems
      WHERE ProjectId = @Project AND ElementType = @ElementType
        AND LineType NOT IN (3, 4))                                                 AS NetVariations,
    (SELECT COALESCE(SUM(cl.CumulativeClaimed), 0)
       FROM dbo.ClaimLines cl
       JOIN dbo.ValuationLineItems li ON li.ValuationLineItemId = cl.ValuationLineItemId
      WHERE li.ProjectId = @Project AND li.ElementType = @ElementType)              AS VariationsClaimed;
