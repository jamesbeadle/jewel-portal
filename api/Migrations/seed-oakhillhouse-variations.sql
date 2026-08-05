-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed,
-- per JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: Oakhill House Godalming -- Variation Orders (register + report lines)
-- ----------------------------------------------------------------------------
-- Project : Oakhill House, Station Lane, Godalming, Surrey, GU8 5AN
-- ProjectId: resolved at run time by site-name matcher 'oakhillhousegodalming'
--
-- Companion to seed-oakhillhouse-valuation.sql, which seeds ONLY the original
-- contract scope (Contract Sum GBP 102,883.00, incl. inline provisional sums).
-- This file adds the post-contract VARIATION ORDERS from the "Oakhill House
-- Val 7" workbook (Valuation 6 / 6 Month Defects), reconciling to the
-- workbook's variations register:
--
--     Contract Sum            GBP 102,883.00
--     Net Variations          GBP  31,804.00
--     ------------------------------------------
--     Revised Contract Sum    GBP 134,687.00
--
-- MODEL NOTE (unified variation orders, post-20260723120000_UnifyVariationOrders)
-- Each variation order is ONE row in [VariationOrderQuotes]; the separate
-- [VariationOrders] table no longer exists. Workbook VOs that contain several
-- priced rows (omits of contract scope as negatives, new items as positives:
-- V03, V04, V06, V07) are seeded as ONE record whose Value is the NET of that
-- VO's workbook rows, and ONE ValuationLineItem summary line per VO
-- (Quantity 1 x Rate = net). All 12 VOs are claimed in the register and are
-- seeded Approved; there are no declined VOs in this workbook.
--
-- VO nets from the register (sum = 31,804.00 exactly; no penny adjustment
-- was needed):
--   V01    944.00   V02    240.00   V03   -445.00   V04  9,475.00
--   V05    450.00   V06  2,990.00   V07  6,745.00   V08  8,355.00
--   V09    645.00   V10    905.00   V11    880.00   V12    620.00
--
-- The workbook gives no VO dates; CreatedAt is placed just before each VO's
-- first claimed valuation month (Valuation 1..6 assumed monthly from
-- Feb 2024, following the workbook's REV3 07.12.23 scope date), with
-- IssuedAt ~ +7 days and ApprovedAt ~ +21 days, per the Albany pattern.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation (all rows here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net > 0  -> Priced (addition)
--   * net < 0  -> Omit   (net reduction; stored as a negative LineAmount)
-- VOQ Status : unified VariationOrderStatus ladder (Quoting -> Issued ->
--              Awaiting AI -> Approved / Rejected); approved historical
--              VOs are seeded with Status = 2 (Approved), per spec.
--
-- Idempotent: keyed on stable ids (oh-voq-vNN / oh-vo-vNN) via MERGE. The
-- contract lines seeded by seed-oakhillhouse-valuation.sql are left
-- untouched. Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'oakhillhousegodalming'
       OR LOWER(REPLACE(Name, ' ', '')) = 'oakhillhousegodalming'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'oakhillhousegodalming' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  Oakhill House Godalming — no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[VariationOrderQuotes] AS target
    USING (VALUES
        (N'oh-voq-v01', @ProjectId, N'', 1, N'VOQ-0001', N'Structural steel as per engineers design', N'Structural steel as per engineers design', 2, NULL, NULL, 944.0000, N'V01', 944.0000, N'STR-STL', '2024-01-20', N'seed@jewelgroup.co.uk', '2024-01-27', '2024-02-10', N'seed@jewelgroup.co.uk', NULL),
        (N'oh-voq-v02', @ProjectId, N'', 2, N'VOQ-0002', N'New ceiling joists lowered to match existing', N'New ceiling joists lowered to match existing', 2, NULL, NULL, 240.0000, N'V02', 240.0000, N'CARP-1FX', '2024-02-20', N'seed@jewelgroup.co.uk', '2024-02-27', '2024-03-12', N'seed@jewelgroup.co.uk', NULL),
        (N'oh-voq-v03', @ProjectId, N'', 3, N'VOQ-0003', N'Omit part floor slab build-up - hardcore, concrete, mesh & DPM', N'Omit part floor slab build-up - hardcore, concrete, mesh & DPM', 2, NULL, NULL, -445.0000, N'V03', -445.0000, N'SUB-CON', '2024-01-20', N'seed@jewelgroup.co.uk', '2024-01-27', '2024-02-10', N'seed@jewelgroup.co.uk', NULL),
        (N'oh-voq-v04', @ProjectId, N'', 4, N'VOQ-0004', N'Generation Windows - replace provisional glazing allowance & Crittall doors', N'Generation Windows - replace provisional glazing allowance & Crittall doors', 2, NULL, NULL, 9475.0000, N'V04', 9475.0000, N'WDR-ALU', '2024-02-20', N'seed@jewelgroup.co.uk', '2024-02-27', '2024-03-12', N'seed@jewelgroup.co.uk', NULL),
        (N'oh-voq-v05', @ProjectId, N'', 5, N'VOQ-0005', N'M&E - Additional CAT6 cables', N'M&E - Additional CAT6 cables', 2, NULL, NULL, 450.0000, N'V05', 450.0000, N'ELE-STD', '2024-02-20', N'seed@jewelgroup.co.uk', '2024-02-27', '2024-03-12', N'seed@jewelgroup.co.uk', NULL),
        (N'oh-voq-v06', @ProjectId, N'', 6, N'VOQ-0006', N'Howdens utility supply & install - omit kitchen refit', N'Howdens utility supply & install - omit kitchen refit', 2, NULL, NULL, 2990.0000, N'V06', 2990.0000, N'SUP-KIT', '2024-03-20', N'seed@jewelgroup.co.uk', '2024-03-27', '2024-04-10', N'seed@jewelgroup.co.uk', NULL),
        (N'oh-voq-v07', @ProjectId, N'', 7, N'VOQ-0007', N'Supply & install joinery as per drawing L/1895/4/402A - omit shelving to stud', N'Supply & install joinery as per drawing L/1895/4/402A - omit shelving to stud', 2, NULL, NULL, 6745.0000, N'V07', 6745.0000, N'CARP-JNR', '2024-03-20', N'seed@jewelgroup.co.uk', '2024-03-27', '2024-04-10', N'seed@jewelgroup.co.uk', NULL),
        (N'oh-voq-v08', @ProjectId, N'', 8, N'VOQ-0008', N'Internal Heritage Doors', N'Internal Heritage Doors', 2, NULL, NULL, 8355.0000, N'V08', 8355.0000, N'CARP-DOR', '2024-03-20', N'seed@jewelgroup.co.uk', '2024-03-27', '2024-04-10', N'seed@jewelgroup.co.uk', NULL),
        (N'oh-voq-v09', @ProjectId, N'', 9, N'VOQ-0009', N'Pantry Decoration', N'Pantry Decoration', 2, NULL, NULL, 645.0000, N'V09', 645.0000, N'DEC-STD', '2024-04-20', N'seed@jewelgroup.co.uk', '2024-04-27', '2024-05-11', N'seed@jewelgroup.co.uk', NULL),
        (N'oh-voq-v10', @ProjectId, N'', 10, N'VOQ-0010', N'Tile installation & supply - kitchen & utility', N'Tile installation & supply - kitchen & utility', 2, NULL, NULL, 905.0000, N'V10', 905.0000, N'TIL-STD', '2024-04-20', N'seed@jewelgroup.co.uk', '2024-04-27', '2024-05-11', N'seed@jewelgroup.co.uk', NULL),
        (N'oh-voq-v11', @ProjectId, N'', 11, N'VOQ-0011', N'Decoration - additional areas', N'Decoration - additional areas', 2, NULL, NULL, 880.0000, N'V11', 880.0000, N'DEC-STD', '2024-05-20', N'seed@jewelgroup.co.uk', '2024-05-27', '2024-06-10', N'seed@jewelgroup.co.uk', NULL),
        (N'oh-voq-v12', @ProjectId, N'', 12, N'VOQ-0012', N'Oak Bench Seat', N'Oak Bench Seat', 2, NULL, NULL, 620.0000, N'V12', 620.0000, N'CARP-JNR', '2024-06-20', N'seed@jewelgroup.co.uk', '2024-06-27', '2024-07-11', N'seed@jewelgroup.co.uk', NULL)
    ) AS source (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
                 Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
                 VariationRef, Value, CostCode, CreatedAt, CreatedByEmail, IssuedAt,
                 ApprovedAt, ApprovedByEmail, RejectedAt)
    ON target.VariationOrderQuoteId = source.VariationOrderQuoteId
    WHEN MATCHED THEN UPDATE SET
        ProjectId               = source.ProjectId,
        RequestId               = source.RequestId,
        Number                  = source.Number,
        Reference               = source.Reference,
        Title                   = source.Title,
        Description             = source.Description,
        Status                  = source.Status,
        SelectedBidPackageId    = source.SelectedBidPackageId,
        SelectedSubcontractorId = source.SelectedSubcontractorId,
        EstimatedValue          = source.EstimatedValue,
        VariationRef            = source.VariationRef,
        Value                   = source.Value,
        CostCode                = source.CostCode,
        CreatedAt               = source.CreatedAt,
        CreatedByEmail          = source.CreatedByEmail,
        IssuedAt                = source.IssuedAt,
        ApprovedAt              = source.ApprovedAt,
        ApprovedByEmail         = source.ApprovedByEmail,
        RejectedAt              = source.RejectedAt
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (VariationOrderQuoteId, ProjectId, RequestId, Number, Reference, Title, Description,
                Status, SelectedBidPackageId, SelectedSubcontractorId, EstimatedValue,
                VariationRef, Value, CostCode, CreatedAt, CreatedByEmail, IssuedAt,
                ApprovedAt, ApprovedByEmail, RejectedAt)
        VALUES (source.VariationOrderQuoteId, source.ProjectId, source.RequestId, source.Number,
                source.Reference, source.Title, source.Description, source.Status,
                source.SelectedBidPackageId, source.SelectedSubcontractorId, source.EstimatedValue,
                source.VariationRef, source.Value, source.CostCode, source.CreatedAt,
                source.CreatedByEmail, source.IssuedAt, source.ApprovedAt,
                source.ApprovedByEmail, source.RejectedAt);

    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
        (N'oh-vo-v01', @ProjectId, 3, N'', N'', N'V01', N'Structural steel as per engineers design', 0, N'STR-STL', N'', N'item', 1.0000, 944.0000, 944.0000, N'', 1),
        (N'oh-vo-v02', @ProjectId, 3, N'', N'', N'V02', N'New ceiling joists lowered to match existing', 0, N'CARP-1FX', N'', N'item', 1.0000, 240.0000, 240.0000, N'', 2),
        (N'oh-vo-v03', @ProjectId, 3, N'', N'', N'V03', N'Omit part floor slab build-up - hardcore, concrete, mesh & DPM', 2, N'SUB-CON', N'', N'item', 1.0000, -445.0000, -445.0000, N'', 3),
        (N'oh-vo-v04', @ProjectId, 3, N'', N'', N'V04', N'Generation Windows - replace provisional glazing allowance & Crittall doors', 0, N'WDR-ALU', N'', N'item', 1.0000, 9475.0000, 9475.0000, N'', 4),
        (N'oh-vo-v05', @ProjectId, 3, N'', N'', N'V05', N'M&E - Additional CAT6 cables', 0, N'ELE-STD', N'', N'item', 1.0000, 450.0000, 450.0000, N'', 5),
        (N'oh-vo-v06', @ProjectId, 3, N'', N'', N'V06', N'Howdens utility supply & install - omit kitchen refit', 0, N'SUP-KIT', N'', N'item', 1.0000, 2990.0000, 2990.0000, N'', 6),
        (N'oh-vo-v07', @ProjectId, 3, N'', N'', N'V07', N'Supply & install joinery as per drawing L/1895/4/402A - omit shelving to stud', 0, N'CARP-JNR', N'', N'item', 1.0000, 6745.0000, 6745.0000, N'', 7),
        (N'oh-vo-v08', @ProjectId, 3, N'', N'', N'V08', N'Internal Heritage Doors', 0, N'CARP-DOR', N'', N'item', 1.0000, 8355.0000, 8355.0000, N'', 8),
        (N'oh-vo-v09', @ProjectId, 3, N'', N'', N'V09', N'Pantry Decoration', 0, N'DEC-STD', N'', N'item', 1.0000, 645.0000, 645.0000, N'', 9),
        (N'oh-vo-v10', @ProjectId, 3, N'', N'', N'V10', N'Tile installation & supply - kitchen & utility', 0, N'TIL-STD', N'', N'item', 1.0000, 905.0000, 905.0000, N'', 10),
        (N'oh-vo-v11', @ProjectId, 3, N'', N'', N'V11', N'Decoration - additional areas', 0, N'DEC-STD', N'', N'item', 1.0000, 880.0000, 880.0000, N'', 11),
        (N'oh-vo-v12', @ProjectId, 3, N'', N'', N'V12', N'Oak Bench Seat', 0, N'CARP-JNR', N'', N'item', 1.0000, 620.0000, 620.0000, N'', 12)
    ) AS source (ValuationLineItemId, ProjectId, ElementType, SectionCode, SectionName,
                 VariationRef, VariationTitle, LineType, CostCode, Description, Unit,
                 Quantity, Rate, LineAmount, Comments, DisplayOrder)
    ON target.ValuationLineItemId = source.ValuationLineItemId
    WHEN MATCHED THEN UPDATE SET
        ProjectId      = source.ProjectId,
        ElementType    = source.ElementType,
        SectionCode    = source.SectionCode,
        SectionName    = source.SectionName,
        VariationRef   = source.VariationRef,
        VariationTitle = source.VariationTitle,
        LineType       = source.LineType,
        CostCode       = source.CostCode,
        Description    = source.Description,
        Unit           = source.Unit,
        Quantity       = source.Quantity,
        Rate           = source.Rate,
        LineAmount     = source.LineAmount,
        Comments       = source.Comments,
        DisplayOrder   = source.DisplayOrder
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (ValuationLineItemId, ProjectId, ElementType, SectionCode, SectionName,
                VariationRef, VariationTitle, LineType, CostCode, Description, Unit,
                Quantity, Rate, LineAmount, Comments, DisplayOrder)
        VALUES (source.ValuationLineItemId, source.ProjectId, source.ElementType,
                source.SectionCode, source.SectionName, source.VariationRef,
                source.VariationTitle, source.LineType, source.CostCode,
                source.Description, source.Unit, source.Quantity, source.Rate,
                source.LineAmount, source.Comments, source.DisplayOrder);
    PRINT 'Oakhill House Godalming: variation orders & variation report lines merged.';
    COMMIT TRAN;
END
GO

-- Sanity check: variation lines should reconcile to the workbook register.
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'oakhillhousegodalming'
       OR LOWER(REPLACE(Name, ' ', '')) = 'oakhillhousegodalming'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'oakhillhousegodalming' THEN 0 ELSE 1 END);
SELECT
    (SELECT COUNT(*) FROM [dbo].[VariationOrderQuotes]
      WHERE ProjectId = @ProjectId) AS VariationOrders,                                -- 12
    (SELECT SUM(Value) FROM [dbo].[VariationOrderQuotes]
      WHERE ProjectId = @ProjectId AND Status = 2) AS NetVoValue,                      -- 31804.00
    (SELECT COUNT(*) FROM [dbo].[ValuationLineItems]
      WHERE ProjectId = @ProjectId AND ElementType = 3) AS VariationLines,             -- 12
    (SELECT SUM(LineAmount) FROM [dbo].[ValuationLineItems]
      WHERE ProjectId = @ProjectId AND ElementType = 3
        AND LineType NOT IN (3, 4)) AS NetVariations;                                  -- 31804.00

-- Combined check: original Contract Sum + Net Variations = Revised Contract Sum.
SELECT
    SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,    -- 102883.00
    SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations,  --  31804.00
    SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                         -- 134687.00
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = @ProjectId;
GO
