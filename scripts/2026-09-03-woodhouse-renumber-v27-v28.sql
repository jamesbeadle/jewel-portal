-- =====================================================================================
-- Woodhouse (JBB-2026-004) — align portal variation numbers with the client-facing
-- valuation workbook (Nichols Nymet Valuation No. 10, Aug 26).  2026-09-03
--
-- Workbook (authoritative)                     Portal before this script
--   V07  Fluid Glass deposit + balance omits     V7 deposit only; balance on a separate V27
--   V02  loft hatch omit + AI.02.11 add-back     add-back on a separate V28 (Awaiting AI)
--   V26  Kitchen AC                              V26 "Kitchen AC & MF works" (combined)
--   V27  Rear Patio Sub-Base       £13,392       created today as V29
--   V28  Kitchen Floor Slab        £7,446        created today as V30
--   V29  Additional MF works squaring off walls  (inside V26)
--
-- What this does, in one transaction:
--   1. Merges the Fluid Glass balance (portal V27) INTO V7: the valuation line moves to
--      V7 (keeping its ValuationLineItemId, so the Val 8/9/10 claim rows follow it), the
--      V7 order value becomes -32,765.58, the V27 approval accrual is re-labelled as V7's
--      (budget commitment on WDR-SPG is already right and is NOT touched), and the V27 row
--      is deleted.
--   2. Renumbers today's Rear Patio 29 -> 27 and Kitchen Slab 30 -> 28 (drops the
--      "[client-facing ref …]" title suffixes).
--   3. Moves the loft-hatch add-back off 28 to 30 (it is Awaiting AI with £0 approved and
--      its 7 staged lines survive; the workbook carries it inside V02 — delete it in the
--      portal later if you would rather fold it into V02 when AI.02.11 lands).
--   4. Retitles V26 to Kitchen AC only and creates V29 "Additional MF works squaring off
--      kitchen walls" in Quoting, unpriced.
--
-- Deliberately NOT touched: ValuationReportSnapshotLines for Vals 8 and 9 still read
-- "V27" — snapshots are frozen client-facing records (jpms-valuation-cycle). Optional
-- cosmetic relabel at the bottom, commented out.
--
-- Run:  sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin
--              -i 2026-09-03-woodhouse-renumber-v27-v28.sql -b -o renumber.log
-- Every guard RAISERRORs (severity 16) so -b stops and the transaction rolls back.
-- =====================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ProjectId     nvarchar(64) = N'c16a737d8e1347f28917183b77360f1d';
DECLARE @V7Id          nvarchar(64) = N'df7eef3fe661446099bdb317b4caf973'; -- Fluid Glass deposit omit
DECLARE @V27FluidId    nvarchar(64) = N'df40f05fdc504b87a3c37f9818ec5edb'; -- Fluid Glass balance omit (to merge)
DECLARE @V27LineId     nvarchar(64) = N'e0f9d409676742f596571d9b4c3f3764'; -- its valuation line
DECLARE @V28LoftId     nvarchar(64) = N'08e6dde4217b48dca389b11481865d3e'; -- loft hatch add-back (to 30)
DECLARE @V29PatioId    nvarchar(64) = N'00a3a5cbe9ec46c892f6a769b9a7da42'; -- Rear Patio (to 27)
DECLARE @V30SlabId     nvarchar(64) = N'e866b77c58ce4c6f8866cc180b81abb3'; -- Kitchen Slab (to 28)
DECLARE @V26AcId       nvarchar(64) = N'3de8af2d0b0b4fda8287cbd92ce3f063'; -- Kitchen AC & MF (retitle)
DECLARE @NewV29Id      nvarchar(64) = LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''));
DECLARE @Now           datetimeoffset = SYSDATETIMEOFFSET();
DECLARE @By            nvarchar(256) = N'james.beadle@jewelbb.co.uk';

BEGIN TRANSACTION;

-- ---------------------------------------------------------------- guards: rows are what we think
IF NOT EXISTS (SELECT 1 FROM VariationOrderQuotes WHERE VariationOrderQuoteId = @V7Id AND ProjectId = @ProjectId AND Number = 7 AND Status = 2 AND VariationRef = N'V7')
    RAISERROR('Guard: V7 (Fluid Glass deposit) not as expected.', 16, 1);
IF NOT EXISTS (SELECT 1 FROM VariationOrderQuotes WHERE VariationOrderQuoteId = @V27FluidId AND ProjectId = @ProjectId AND Number = 27 AND Status = 2 AND VariationRef = N'V27' AND Value = -28525.44)
    RAISERROR('Guard: V27 (Fluid Glass balance) not as expected.', 16, 1);
IF NOT EXISTS (SELECT 1 FROM ValuationLineItems WHERE ValuationLineItemId = @V27LineId AND ProjectId = @ProjectId AND VariationRef = N'V27' AND LineAmount = -28525.44)
    RAISERROR('Guard: V27 valuation line not as expected.', 16, 1);
IF (SELECT COUNT(*) FROM ValuationLineItems WHERE ProjectId = @ProjectId AND VariationRef = N'V27') <> 1
    RAISERROR('Guard: expected exactly one V27 valuation line.', 16, 1);
IF NOT EXISTS (SELECT 1 FROM VariationOrderQuotes WHERE VariationOrderQuoteId = @V28LoftId AND ProjectId = @ProjectId AND Number = 28 AND Status = 4 AND VariationRef IS NULL)
    RAISERROR('Guard: V28 (loft hatch, Awaiting AI) not as expected.', 16, 1);
IF NOT EXISTS (SELECT 1 FROM VariationOrderQuotes WHERE VariationOrderQuoteId = @V29PatioId AND ProjectId = @ProjectId AND Number = 29 AND Status = 0 AND VariationRef IS NULL)
    RAISERROR('Guard: V29 (Rear Patio, Quoting) not as expected.', 16, 1);
IF NOT EXISTS (SELECT 1 FROM VariationOrderQuotes WHERE VariationOrderQuoteId = @V30SlabId AND ProjectId = @ProjectId AND Number = 30 AND Status = 0 AND VariationRef IS NULL)
    RAISERROR('Guard: V30 (Kitchen Slab, Quoting) not as expected.', 16, 1);
IF NOT EXISTS (SELECT 1 FROM VariationOrderQuotes WHERE VariationOrderQuoteId = @V26AcId AND ProjectId = @ProjectId AND Number = 26 AND Status = 0)
    RAISERROR('Guard: V26 not as expected.', 16, 1);
IF EXISTS (SELECT 1 FROM WorkOrders WHERE VariationOrderId IN (@V27FluidId, @V28LoftId, @V29PatioId, @V30SlabId))
    RAISERROR('Guard: a work order instructs one of the variations being moved.', 16, 1);
IF EXISTS (SELECT 1 FROM VariationOrderQuotes WHERE ProjectId = @ProjectId AND Number IN (29, 30) AND VariationOrderQuoteId NOT IN (@V29PatioId, @V30SlabId))
    RAISERROR('Guard: numbers 29/30 are held by other records.', 16, 1);

-- ---------------------------------------------------------------- 1. merge Fluid Glass balance into V7
UPDATE ValuationLineItems
   SET VariationRef   = N'V7',
       VariationTitle = N'Fluid Glass Glazing — Deposit & Balance Omissions',
       Comments       = N'Variation order V7 (from VOQ-0007) — balance line merged from VOQ-0027 on 2026-09-03; presented under V07 in the valuation workbook'
 WHERE ValuationLineItemId = @V27LineId;

UPDATE ValuationLineItems
   SET VariationTitle = N'Fluid Glass Glazing — Deposit & Balance Omissions'
 WHERE ProjectId = @ProjectId AND VariationRef = N'V7' AND ValuationLineItemId <> @V27LineId;

-- the V27 approval accrual becomes a second V7 accrual (same OmitAmount; budget already committed on WDR-SPG)
UPDATE QsAccruals
   SET Description = N'V7 — Fluid Glass Glazing — balance of cost paid direct by Client (merged from V27, 2026-09-03)'
 WHERE ProjectId = @ProjectId AND Category = N'Variation' AND Description LIKE N'V27 — %';

UPDATE VariationOrderQuotes
   SET Title          = N'Fluid Glass Glazing — Deposit & Balance Omissions (paid direct by Client)',
       Description    = N'Omission from the Contract Sum of the Fluid Glass windows and doors package paid direct by the Client: deposit £4,240.14 (omitted at Valuation 4) and balance £28,525.44 (omitted at Valuation 8). Presented as V07 in the valuation workbook. Balance line merged from the former portal V27 on 2026-09-03.',
       Value          = -32765.58,
       EstimatedValue = -32765.58,
       CommercialBasis = N'Omission of client-direct payment. Contract-basis value only; no OH&P adjustment applied.'
 WHERE VariationOrderQuoteId = @V7Id;

-- retire the V27 row and anything hanging off it
DELETE FROM VariationOrderMessages         WHERE VariationOrderId = @V27FluidId;
DELETE FROM ArchitectInstructionVariations WHERE VariationOrderId = @V27FluidId;
UPDATE BidPackages         SET VariationOrderQuoteId = NULL WHERE VariationOrderQuoteId = @V27FluidId;
UPDATE BidPackageLineItems SET Coverage = 0, VariationOrderQuoteId = NULL WHERE VariationOrderQuoteId = @V27FluidId;
UPDATE SubcontractorVariationRequests SET VariationOrderQuoteId = NULL WHERE VariationOrderQuoteId = @V27FluidId;
DELETE FROM VariationOrderQuotes WHERE VariationOrderQuoteId = @V27FluidId;

-- ---------------------------------------------------------------- 2/3. renumber (28 must be freed before 30 -> 28)
UPDATE VariationOrderQuotes
   SET Number = 30, Reference = N'VOQ-0030',
       Title = N'Loft Hatch — Add-back & New Enlarged Hatch/Ladder (AI.02.11, cost to be approved) — carried inside V02 in the valuation workbook'
 WHERE VariationOrderQuoteId = @V28LoftId;

UPDATE VariationOrderQuotes
   SET Number = 27, Reference = N'VOQ-0027',
       Title = N'Rear Patio Sub-Base — reinstatement of omitted hardcore & concrete (spec Q.4)'
 WHERE VariationOrderQuoteId = @V29PatioId;

UPDATE VariationOrderQuotes
   SET Number = 28, Reference = N'VOQ-0028',
       Title = N'Kitchen Floor Slab, Existing Kitchen Area — replacement build-up behind ground beam (disclosed exclusion)'
 WHERE VariationOrderQuoteId = @V30SlabId;

-- ---------------------------------------------------------------- 4. V26 = AC only; V29 = MF works
UPDATE VariationOrderQuotes
   SET Title       = N'Kitchen AC — Potential Addition of Air Conditioning to Kitchen',
       Description = N'Potential addition of air conditioning to the Kitchen — awaiting AC quotation before proceeding. Price to follow. Unpriced. (The MF works squaring off the kitchen walls, previously bundled here, are V29.)'
 WHERE VariationOrderQuoteId = @V26AcId;

INSERT INTO VariationOrderQuotes
    (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description, Status,
     SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue, VariationRef, Value, CostCode,
     CommercialBasis, ProgrammeImpact, Exclusions, DraftLinesJson,
     CreatedAt, CreatedByEmail, IssuedAt, ApprovedAt, ApprovedByEmail, RejectedAt)
VALUES
    (@NewV29Id, @ProjectId, N'', 29, N'VOQ-0029',
     N'Additional MF Works to Square Off Kitchen Walls',
     N'Additional MF works squaring off kitchen walls — NK building costs to be established. Unpriced. Previously bundled with the Kitchen AC item (V26).',
     0, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL,
     @Now, @By, NULL, NULL, NULL, NULL);

-- ---------------------------------------------------------------- post-checks
IF (SELECT COUNT(*) FROM VariationOrderQuotes WHERE ProjectId = @ProjectId AND Number IN (7, 26, 27, 28, 29, 30)) <> 6
    RAISERROR('Post-check: expected exactly one row on each of 7, 26, 27, 28, 29, 30.', 16, 1);
IF EXISTS (SELECT Number FROM VariationOrderQuotes WHERE ProjectId = @ProjectId GROUP BY Number HAVING COUNT(*) > 1)
    RAISERROR('Post-check: duplicate variation numbers on the project.', 16, 1);
IF (SELECT SUM(LineAmount) FROM ValuationLineItems WHERE ProjectId = @ProjectId AND VariationRef = N'V7') <> -32765.58
    RAISERROR('Post-check: V7 valuation lines do not sum to -32,765.58.', 16, 1);
IF EXISTS (SELECT 1 FROM ValuationLineItems WHERE ProjectId = @ProjectId AND VariationRef = N'V27')
    RAISERROR('Post-check: a V27 valuation line still exists.', 16, 1);

COMMIT TRANSACTION;

SELECT Number, Reference, VariationRef, Status, Value, EstimatedValue, LEFT(Title, 90) AS Title
  FROM VariationOrderQuotes
 WHERE ProjectId = @ProjectId AND Number IN (2, 7, 26, 27, 28, 29, 30)
 ORDER BY Number;

PRINT 'Woodhouse variation renumber completed.';

-- ---------------------------------------------------------------- optional, cosmetic, NOT run
-- Frozen Val 8/9 snapshot lines still read V27. Only if you want the history pages to match the paper:
-- UPDATE ValuationReportSnapshotLines SET VariationRef = N'V7'
--  WHERE SourceValuationLineItemId = N'e0f9d409676742f596571d9b4c3f3764' AND VariationRef = N'V27';
