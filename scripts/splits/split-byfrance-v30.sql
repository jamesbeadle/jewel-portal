-- ============================================================================
-- Split By France V30: one net valuation line -> 4 trade/detail lines
-- Generated from split-variation-order-template.sql -- touches ONLY V30.
-- Single transaction; guards abort+rollback on any drift (net changed, claim
-- edited, ids exist); no-ops if already split. Never touches other lines,
-- VO/VOQ records, CostCodeBudgets or CostCentreCostProgress.
-- Run: sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i scripts/splits/split-byfrance-v30.sql -b
-- ============================================================================
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRAN;

DECLARE @OldLineId   nvarchar(64)  = N'bf-vo-v30';
DECLARE @ExpectedNet decimal(18,4) = 29185.0000;

IF NOT EXISTS (SELECT 1 FROM dbo.ValuationLineItems WHERE ValuationLineItemId = @OldLineId)
BEGIN
    PRINT 'V30: old line already gone -- split appears to have run. Nothing to do.';
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
    THROW 50001, 'V30: line net differs from expected -- revalued since this script was written. Re-check before splitting.', 1;

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
    (1, N'bf-vo-v30a', N'bf-cl18-vo-v30a', N'MGN - Render & plasterboard omits; dot & dab boarding, MF ceilings & coffer detail - P-706 Rev A GF RCP', 0, N'INT-RDR', 16295.0000, 16295.00),
    (2, N'bf-vo-v30b', N'bf-cl18-vo-v30b', N'JewelBB - Curtain track openings, stud cantilever, shelf cover & kitchen island plinth', 0, N'CARP-2FX', 3920.0000, 3279.00),
    (3, N'bf-vo-v30c', N'bf-cl18-vo-v30c', N'Blinds and Curtains - Double curtain track system', 0, N'WIN-BLD', 5220.0000, 2610.00),
    (4, N'bf-vo-v30d', N'bf-cl18-vo-v30d', N'Electrician - LED aluminium profile strip lighting', 0, N'ELE-STD', 3750.0000, 0.00);

IF (SELECT SUM(Amount) FROM @NewLines) <> @Net
    THROW 50002, 'V30: new line amounts do not sum to the old line''s net.', 1;

IF EXISTS (SELECT 1 FROM dbo.ValuationLineItems
           WHERE ValuationLineItemId IN (SELECT LineId FROM @NewLines))
    THROW 50003, 'V30: one of the new line ids already exists.', 1;

IF (SELECT COUNT(DISTINCT ValuationClaimId) FROM dbo.ClaimLines
    WHERE ValuationLineItemId = @OldLineId) > 1
    THROW 50004, 'V30: more than one claim references this line -- extend the script before running.', 1;

DECLARE @ClaimedTotal decimal(18,2) =
    (SELECT COALESCE(SUM(CumulativeClaimed), 0) FROM dbo.ClaimLines
     WHERE ValuationLineItemId = @OldLineId);
IF (SELECT SUM(ClaimedNow) FROM @NewLines) <> @ClaimedTotal
    THROW 50005, 'V30: per-line claimed values do not sum to what is currently claimed in the DB (claim % edited since?). Re-check.', 1;

DECLARE @ClaimId nvarchar(64) =
    (SELECT TOP 1 ValuationClaimId FROM dbo.ClaimLines
     WHERE ValuationLineItemId = @OldLineId);

UPDATE dbo.ValuationLineItems
SET DisplayOrder = DisplayOrder + (SELECT COUNT(*) - 1 FROM @NewLines)
WHERE ProjectId = @Project AND ElementType = @ElementType AND DisplayOrder > @Order;

DELETE FROM dbo.ClaimLines         WHERE ValuationLineItemId = @OldLineId;
DELETE FROM dbo.ValuationLineItems WHERE ValuationLineItemId = @OldLineId;

INSERT INTO dbo.ValuationLineItems
    (ValuationLineItemId, ProjectId, ElementType, SectionCode, SectionName,
     VariationRef, VariationTitle, LineType, CostCode, Description, Unit,
     Quantity, Rate, LineAmount, Comments, DisplayOrder)
SELECT LineId, @Project, @ElementType, @SectionCode, @SectionName,
       @VariationRef, Title, LineType, CostCode, N'', N'item',
       1.0000, Amount, Amount, N'', @Order + Seq - 1
FROM @NewLines;

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

SELECT
    N'V30' AS SplitApplied,
    (SELECT COUNT(*) FROM dbo.ValuationLineItems
      WHERE ProjectId = @Project AND ElementType = @ElementType)                    AS VariationLines,
    (SELECT SUM(LineAmount) FROM dbo.ValuationLineItems
      WHERE ProjectId = @Project AND ElementType = @ElementType
        AND LineType NOT IN (3, 4))                                                 AS NetVariations,
    (SELECT COALESCE(SUM(cl.CumulativeClaimed), 0)
       FROM dbo.ClaimLines cl
       JOIN dbo.ValuationLineItems li ON li.ValuationLineItemId = cl.ValuationLineItemId
      WHERE li.ProjectId = @Project AND li.ElementType = @ElementType)              AS VariationsClaimed;
