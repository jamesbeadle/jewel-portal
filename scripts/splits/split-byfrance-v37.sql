-- ============================================================================
-- Split By France V37: one net valuation line -> 3 trade/detail lines
-- Generated from split-variation-order-template.sql -- touches ONLY V37.
-- Single transaction; guards abort+rollback on any drift (net changed, claim
-- edited, ids exist); no-ops if already split. Never touches other lines,
-- VO/VOQ records, CostCodeBudgets or CostCentreCostProgress.
-- Run: sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i scripts/splits/split-byfrance-v37.sql -b
-- ============================================================================
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRAN;

DECLARE @OldLineId   nvarchar(64)  = N'bf-vo-v37';
DECLARE @ExpectedNet decimal(18,4) = 4245.0000;

IF NOT EXISTS (SELECT 1 FROM dbo.ValuationLineItems WHERE ValuationLineItemId = @OldLineId)
BEGIN
    PRINT 'V37: old line already gone -- split appears to have run. Nothing to do.';
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
    THROW 50001, 'V37: line net differs from expected -- revalued since this script was written. Re-check before splitting.', 1;

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
    (1, N'bf-vo-v37a', N'bf-cl18-vo-v37a', N'GF - ZZ Bathroom - On the Level Shower Former (specialist supply)', 0, N'SUP-SAN', 3150.0000, 3150.00),
    (2, N'bf-vo-v37b', N'bf-cl18-vo-v37b', N'Plumber - Underground drainage alterations - P-156 Rev E GF', 0, N'MEC-PLM', 645.0000, 645.00),
    (3, N'bf-vo-v37c', N'bf-cl18-vo-v37c', N'JewelBB - Remove & Reinstate Block & Beam', 0, N'MASON-BRK', 450.0000, 450.00);

IF (SELECT SUM(Amount) FROM @NewLines) <> @Net
    THROW 50002, 'V37: new line amounts do not sum to the old line''s net.', 1;

IF EXISTS (SELECT 1 FROM dbo.ValuationLineItems
           WHERE ValuationLineItemId IN (SELECT LineId FROM @NewLines))
    THROW 50003, 'V37: one of the new line ids already exists.', 1;

IF (SELECT COUNT(DISTINCT ValuationClaimId) FROM dbo.ClaimLines
    WHERE ValuationLineItemId = @OldLineId) > 1
    THROW 50004, 'V37: more than one claim references this line -- extend the script before running.', 1;

DECLARE @ClaimedTotal decimal(18,2) =
    (SELECT COALESCE(SUM(CumulativeClaimed), 0) FROM dbo.ClaimLines
     WHERE ValuationLineItemId = @OldLineId);
IF (SELECT SUM(ClaimedNow) FROM @NewLines) <> @ClaimedTotal
    THROW 50005, 'V37: per-line claimed values do not sum to what is currently claimed in the DB (claim % edited since?). Re-check.', 1;

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
    N'V37' AS SplitApplied,
    (SELECT COUNT(*) FROM dbo.ValuationLineItems
      WHERE ProjectId = @Project AND ElementType = @ElementType)                    AS VariationLines,
    (SELECT SUM(LineAmount) FROM dbo.ValuationLineItems
      WHERE ProjectId = @Project AND ElementType = @ElementType
        AND LineType NOT IN (3, 4))                                                 AS NetVariations,
    (SELECT COALESCE(SUM(cl.CumulativeClaimed), 0)
       FROM dbo.ClaimLines cl
       JOIN dbo.ValuationLineItems li ON li.ValuationLineItemId = cl.ValuationLineItemId
      WHERE li.ProjectId = @Project AND li.ElementType = @ElementType)              AS VariationsClaimed;
