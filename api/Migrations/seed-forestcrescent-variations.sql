-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per
-- JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: 6 Forest Crescent -- Variation Orders (unified) + variation report lines
-- ----------------------------------------------------------------------------
-- Project : Forest Crescent, Ashtead KT21 1JU
-- ProjectId: resolved at run time by site-name matcher '6forestcrescent'
--
-- Companion to seed-forestcrescent-valuation.sql, which seeds ONLY the original
-- contract scope (Contract Sum GBP 174,706.00). This file adds the
-- post-contract VARIATION ORDERS from the "Valuation 10 - Retention Release"
-- workbook, reconciling to the workbook's variations register:
--
--     Contract Sum            GBP 174,706.00
--     Net Variations          GBP  44,434.50
--     ----------------------------------------
--     Revised Contract Sum    GBP 219,140.50
--
-- UNIFIED MODEL (post 20260723120000_UnifyVariationOrders): each variation is
-- ONE row in [VariationOrderQuotes] -- there is no separate [VariationOrders]
-- table any more. Approved variations are Status 2 with VariationRef/Value set;
-- wholly declined variations are Status 3 with Value 0, RejectedAt set and NO
-- valuation line.
--
-- Each workbook VO is split into several priced rows (omits of contract scope
-- as negatives, new items as positives). On the JPMS valuation report a VO
-- shows as a SINGLE summary line, so ONE ValuationLineItem is seeded per
-- APPROVED VO whose LineAmount is the NET of that VO's counted workbook rows
-- (Quantity 1 x Rate = net).
--
-- Wholly DECLINED VOs (Status 3, quoted value kept as EstimatedValue, no
-- valuation line): V05, V06, V07, V09, V18, V29, V34, V35, V36, V39. Where the
-- declined rows carry only qty x rate with blank amounts, EstimatedValue is the
-- best-effort qty x rate sum (V06/V09/V18/V34/V39).
-- Partially-declined VO: V30's "Fix only tiles to the utility splash back" row
-- (80 m2 x 1.80, Declined) is excluded from V30's net (Albany V40 model).
-- SKIPPED: V19 "Boundary wall remedial works" -- the workbook row has no
-- priced rows and no amounts at all, so no record is seeded for it (the VOQ
-- Number sequence therefore has no 19).
-- Judgement call: V25's "Remove existing cabinets and wine rack" prices
-- 1 x 240.00 but the workbook amount is 220.00 -- the workbook AMOUNT is kept
-- as the truth (register reconciles to the stated total only with 220.00).
--
-- Approved nets reconcile to the stated Net Variations EXACTLY
-- (GBP 44,434.50); no rounding adjustment was needed.
--
-- Dates: the workbook gives no VO dates, so CreatedAt is set just before each
-- VO's first claimed valuation month (Aug-24..Apr-25 claim columns), with
-- IssuedAt ~ CreatedAt + 7 days and ApprovedAt ~ CreatedAt + 21 days
-- (RejectedAt ~ IssuedAt + 7 days for declined VOs).
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation (all rows here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net > 0  -> Priced (addition)
--   * net < 0  -> Omit   (net reduction; stored as a negative LineAmount)
--
-- Idempotent: keyed on stable ids (fc-voq-vNN / fc-vo-vNN) via MERGE. The
-- contract lines seeded by seed-forestcrescent-valuation.sql are left
-- untouched. Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '6forestcrescent'
       OR LOWER(REPLACE(Name, ' ', '')) = '6forestcrescent'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '6forestcrescent' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  6 Forest Crescent -- no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[VariationOrderQuotes] AS target
    USING (VALUES
        (N'fc-voq-v01', @ProjectId, N'', 1, N'VOQ-0001', N'Engineers Beam Schedule', N'Engineers Beam Schedule', 2, NULL, NULL, 825.0000, N'V01', 825.0000, N'STR-STL', '2024-09-05', N'seed@jewelgroup.co.uk', '2024-09-12', '2024-09-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v02', @ProjectId, N'', 2, N'VOQ-0002', N'Extension steel to B1 above the garage and site welder', N'Extension steel to B1 above the garage and site welder', 2, NULL, NULL, 465.0000, N'V02', 465.0000, N'STR-STL', '2024-09-05', N'seed@jewelgroup.co.uk', '2024-09-12', '2024-09-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v03', @ProjectId, N'', 3, N'VOQ-0003', N'Face Brickwork Garage Flank', N'Face Brickwork Garage Flank', 2, NULL, NULL, -300.0000, N'V03', -300.0000, N'MASON-BRK', '2024-09-05', N'seed@jewelgroup.co.uk', '2024-09-12', '2024-09-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v04', @ProjectId, N'', 4, N'VOQ-0004', N'Aluminium glazing omit & Generation Windows Quote 8588', N'Aluminium glazing omit & Generation Windows Quote 8588', 2, NULL, NULL, 3438.0000, N'V04', 3438.0000, N'WDR-ALU', '2024-10-05', N'seed@jewelgroup.co.uk', '2024-10-12', '2024-10-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v05', @ProjectId, N'', 5, N'VOQ-0005', N'Skip & Materials Handling', N'Skip & Materials Handling', 3, NULL, NULL, 620.0000, NULL, 0.0000, NULL, '2024-10-12', N'seed@jewelgroup.co.uk', '2024-10-19', NULL, NULL, '2024-10-26'),
        (N'fc-voq-v06', @ProjectId, N'', 6, N'VOQ-0006', N'Main roof, fascia & soffit and rockwool insulation', N'Main roof, fascia & soffit and rockwool insulation', 3, NULL, NULL, 50157.5000, NULL, 0.0000, NULL, '2024-10-12', N'seed@jewelgroup.co.uk', '2024-10-19', NULL, NULL, '2024-10-26'),
        (N'fc-voq-v07', @ProjectId, N'', 7, N'VOQ-0007', N'Scaffolding entire property including tin roof', N'Scaffolding entire property including tin roof', 3, NULL, NULL, 10950.0000, NULL, 0.0000, NULL, '2024-10-12', N'seed@jewelgroup.co.uk', '2024-10-19', NULL, NULL, '2024-10-26'),
        (N'fc-voq-v08', @ProjectId, N'', 8, N'VOQ-0008', N'Ring doorbell hardwired & consumer unit', N'Ring doorbell hardwired & consumer unit', 2, NULL, NULL, 1250.0000, N'V08', 1250.0000, N'ELE-STD', '2024-12-05', N'seed@jewelgroup.co.uk', '2024-12-12', '2024-12-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v09', @ProjectId, N'', 9, N'VOQ-0009', N'Extended preliminaries - site manager, rubbish removal, toilet & H&S', N'Extended preliminaries - site manager, rubbish removal, toilet & H&S', 3, NULL, NULL, 3655.0000, NULL, 0.0000, NULL, '2024-11-12', N'seed@jewelgroup.co.uk', '2024-11-19', NULL, NULL, '2024-11-26'),
        (N'fc-voq-v10', @ProjectId, N'', 10, N'VOQ-0010', N'Naylor lintels D02 / W-08, site welder for staircase & masonry brick cobbling', N'Naylor lintels D02 / W-08, site welder for staircase & masonry brick cobbling', 2, NULL, NULL, 2525.0000, N'V10', 2525.0000, N'MASON-BRK', '2024-10-05', N'seed@jewelgroup.co.uk', '2024-10-12', '2024-10-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v11', @ProjectId, N'', 11, N'VOQ-0011', N'Demolition - wallpaper stripping', N'Demolition - wallpaper stripping', 2, NULL, NULL, 600.0000, N'V11', 600.0000, N'ENABLE-DEM', '2024-11-05', N'seed@jewelgroup.co.uk', '2024-11-12', '2024-11-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v12', @ProjectId, N'', 12, N'VOQ-0012', N'Additional radiators & towel rails with TRVs', N'Additional radiators & towel rails with TRVs', 2, NULL, NULL, 1200.0000, N'V12', 1200.0000, N'MEC-PLM', '2024-11-05', N'seed@jewelgroup.co.uk', '2024-11-12', '2024-11-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v13', @ProjectId, N'', 13, N'VOQ-0013', N'Remove existing ceilings, insulation & plasterboard', N'Remove existing ceilings, insulation & plasterboard', 2, NULL, NULL, 3136.0000, N'V13', 3136.0000, N'INT-PLB', '2024-11-05', N'seed@jewelgroup.co.uk', '2024-11-12', '2024-11-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v14', @ProjectId, N'', 14, N'VOQ-0014', N'Oak cladding omit - MF wall track, plasterboard & decoration to entrance hallway', N'Oak cladding omit - MF wall track, plasterboard & decoration to entrance hallway', 2, NULL, NULL, 1205.0000, N'V14', 1205.0000, N'INT-PLB', '2024-11-05', N'seed@jewelgroup.co.uk', '2024-11-12', '2024-11-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v15', @ProjectId, N'', 15, N'VOQ-0015', N'Double door & skirting revisions - glazed double door, new skirting throughout', N'Double door & skirting revisions - glazed double door, new skirting throughout', 2, NULL, NULL, 7007.0000, N'V15', 7007.0000, N'CARP-2FX', '2024-12-05', N'seed@jewelgroup.co.uk', '2024-12-12', '2024-12-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v16', @ProjectId, N'', 16, N'VOQ-0016', N'Utility stud walls, recessed units, LED strips & making good', N'Utility stud walls, recessed units, LED strips & making good', 2, NULL, NULL, 2626.0000, N'V16', 2626.0000, N'CARP-1FX', '2025-01-05', N'seed@jewelgroup.co.uk', '2025-01-12', '2025-01-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v17', @ProjectId, N'', 17, N'VOQ-0017', N'FF ensuite recess unit & LED strip', N'FF ensuite recess unit & LED strip', 2, NULL, NULL, 740.0000, N'V17', 740.0000, N'CARP-1FX', '2024-12-05', N'seed@jewelgroup.co.uk', '2024-12-12', '2024-12-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v18', @ProjectId, N'', 18, N'VOQ-0018', N'Extended preliminaries - site manager, toilet & H&S', N'Extended preliminaries - site manager, toilet & H&S', 3, NULL, NULL, 3860.0000, NULL, 0.0000, NULL, '2024-12-12', N'seed@jewelgroup.co.uk', '2024-12-19', NULL, NULL, '2024-12-26'),
        (N'fc-voq-v20', @ProjectId, N'', 20, N'VOQ-0020', N'Garage Doors - D-03', N'Garage Doors - D-03', 2, NULL, NULL, 4235.0000, N'V20', 4235.0000, N'WDR-GAR', '2024-12-05', N'seed@jewelgroup.co.uk', '2024-12-12', '2024-12-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v21', @ProjectId, N'', 21, N'VOQ-0021', N'Radiator supply revision - Milano Aruba', N'Radiator supply revision - Milano Aruba', 2, NULL, NULL, 2460.0000, N'V21', 2460.0000, N'MEC-PLM', '2024-12-05', N'seed@jewelgroup.co.uk', '2024-12-12', '2024-12-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v22', @ProjectId, N'', 22, N'VOQ-0022', N'Wetroom kit & linear drain - ensuite', N'Wetroom kit & linear drain - ensuite', 2, NULL, NULL, 675.0000, N'V22', 675.0000, N'SUP-SAN', '2024-12-05', N'seed@jewelgroup.co.uk', '2024-12-12', '2024-12-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v23', @ProjectId, N'', 23, N'VOQ-0023', N'Glazed balustrade to first floor landing - omit', N'Glazed balustrade to first floor landing - omit', 2, NULL, NULL, -1200.0000, N'V23', -1200.0000, N'STR-GRL', '2024-12-05', N'seed@jewelgroup.co.uk', '2024-12-12', '2024-12-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v24', @ProjectId, N'', 24, N'VOQ-0024', N'Under stairs cupboard rework - fire lining, flooring & decoration', N'Under stairs cupboard rework - fire lining, flooring & decoration', 2, NULL, NULL, 1571.0000, N'V24', 1571.0000, N'CARP-JNR', '2024-12-05', N'seed@jewelgroup.co.uk', '2024-12-12', '2024-12-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v25', @ProjectId, N'', 25, N'VOQ-0025', N'Utility refit - Howdens quote F04/0362934, installation & finishes', N'Utility refit - Howdens quote F04/0362934, installation & finishes', 2, NULL, NULL, 6469.0000, N'V25', 6469.0000, N'SUP-KIT', '2025-01-05', N'seed@jewelgroup.co.uk', '2025-01-12', '2025-01-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v26', @ProjectId, N'', 26, N'VOQ-0026', N'Prepare & decorate existing staircase - omit', N'Prepare & decorate existing staircase - omit', 2, NULL, NULL, -850.0000, N'V26', -850.0000, N'DEC-STD', '2025-01-05', N'seed@jewelgroup.co.uk', '2025-01-12', '2025-01-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v27', @ProjectId, N'', 27, N'VOQ-0027', N'MDF window boards - additional allowance', N'MDF window boards - additional allowance', 2, NULL, NULL, 308.0000, N'V27', 308.0000, N'CARP-2FX', '2025-01-05', N'seed@jewelgroup.co.uk', '2025-01-12', '2025-01-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v28', @ProjectId, N'', 28, N'VOQ-0028', N'Supply of Tile Trims', N'Supply of Tile Trims', 2, NULL, NULL, 95.0000, N'V28', 95.0000, N'SUP-TIL', '2025-01-05', N'seed@jewelgroup.co.uk', '2025-01-12', '2025-01-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v29', @ProjectId, N'', 29, N'VOQ-0029', N'Mitre Tiles to GF WC', N'Mitre Tiles to GF WC', 3, NULL, NULL, 350.0000, NULL, 0.0000, NULL, '2025-01-12', N'seed@jewelgroup.co.uk', '2025-01-19', NULL, NULL, '2025-01-26'),
        (N'fc-voq-v30', @ProjectId, N'', 30, N'VOQ-0030', N'Front entrance porch tiling - primer & fix only', N'Front entrance porch tiling - primer & fix only', 2, NULL, NULL, 276.0000, N'V30', 276.0000, N'TIL-STD', '2025-02-05', N'seed@jewelgroup.co.uk', '2025-02-12', '2025-02-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v31', @ProjectId, N'', 31, N'VOQ-0031', N'Study electrical alterations & redecoration', N'Study electrical alterations & redecoration', 2, NULL, NULL, 1904.0000, N'V31', 1904.0000, N'ELE-STD', '2025-01-05', N'seed@jewelgroup.co.uk', '2025-01-12', '2025-01-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v32', @ProjectId, N'', 32, N'VOQ-0032', N'Remedial Works - Leaks FF', N'Remedial Works - Leaks FF', 2, NULL, NULL, 930.0000, N'V32', 930.0000, N'MEC-PLM', '2025-02-05', N'seed@jewelgroup.co.uk', '2025-02-12', '2025-02-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v33', @ProjectId, N'', 33, N'VOQ-0033', N'Howdens additional units - supply & installation', N'Howdens additional units - supply & installation', 2, NULL, NULL, 1635.0000, N'V33', 1635.0000, N'SUP-KIT', '2025-02-05', N'seed@jewelgroup.co.uk', '2025-02-12', '2025-02-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v34', @ProjectId, N'', 34, N'VOQ-0034', N'Remove and replace Oak doors on the FF', N'Remove and replace Oak doors on the FF', 3, NULL, NULL, 1200.0000, NULL, 0.0000, NULL, '2025-02-12', N'seed@jewelgroup.co.uk', '2025-02-19', NULL, NULL, '2025-02-26'),
        (N'fc-voq-v35', @ProjectId, N'', 35, N'VOQ-0035', N'Crazy Paving - Remedial Work', N'Crazy Paving - Remedial Work', 3, NULL, NULL, 450.0000, NULL, 0.0000, NULL, '2025-02-12', N'seed@jewelgroup.co.uk', '2025-02-19', NULL, NULL, '2025-02-26'),
        (N'fc-voq-v36', @ProjectId, N'', 36, N'VOQ-0036', N'Decoration of the Airing Cupboard', N'Decoration of the Airing Cupboard', 3, NULL, NULL, 220.0000, NULL, 0.0000, NULL, '2025-02-12', N'seed@jewelgroup.co.uk', '2025-02-19', NULL, NULL, '2025-02-26'),
        (N'fc-voq-v37', @ProjectId, N'', 37, N'VOQ-0037', N'Decoration of the staircase', N'Decoration of the staircase', 2, NULL, NULL, 830.0000, N'V37', 830.0000, N'DEC-STD', '2025-02-05', N'seed@jewelgroup.co.uk', '2025-02-12', '2025-02-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v38', @ProjectId, N'', 38, N'VOQ-0038', N'Letterbox - Supply & Installation', N'Letterbox - Supply & Installation', 2, NULL, NULL, 379.5000, N'V38', 379.5000, N'SUP-IRO', '2025-02-05', N'seed@jewelgroup.co.uk', '2025-02-12', '2025-02-26', N'seed@jewelgroup.co.uk', NULL),
        (N'fc-voq-v39', @ProjectId, N'', 39, N'VOQ-0039', N'Dressing room radiator & decoration', N'Dressing room radiator & decoration', 3, NULL, NULL, 790.0000, NULL, 0.0000, NULL, '2025-02-12', N'seed@jewelgroup.co.uk', '2025-02-19', NULL, NULL, '2025-02-26')
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
        (N'fc-vo-v01', @ProjectId, 3, N'', N'', N'V01', N'Engineers Beam Schedule', 0, N'STR-STL', N'', N'item', 1.0000, 825.0000, 825.0000, N'', 1),
        (N'fc-vo-v02', @ProjectId, 3, N'', N'', N'V02', N'Extension steel to B1 above the garage and site welder', 0, N'STR-STL', N'', N'item', 1.0000, 465.0000, 465.0000, N'', 2),
        (N'fc-vo-v03', @ProjectId, 3, N'', N'', N'V03', N'Face Brickwork Garage Flank', 2, N'MASON-BRK', N'', N'item', 1.0000, -300.0000, -300.0000, N'', 3),
        (N'fc-vo-v04', @ProjectId, 3, N'', N'', N'V04', N'Aluminium glazing omit & Generation Windows Quote 8588', 0, N'WDR-ALU', N'', N'item', 1.0000, 3438.0000, 3438.0000, N'', 4),
        (N'fc-vo-v08', @ProjectId, 3, N'', N'', N'V08', N'Ring doorbell hardwired & consumer unit', 0, N'ELE-STD', N'', N'item', 1.0000, 1250.0000, 1250.0000, N'', 5),
        (N'fc-vo-v10', @ProjectId, 3, N'', N'', N'V10', N'Naylor lintels D02 / W-08, site welder for staircase & masonry brick cobbling', 0, N'MASON-BRK', N'', N'item', 1.0000, 2525.0000, 2525.0000, N'', 6),
        (N'fc-vo-v11', @ProjectId, 3, N'', N'', N'V11', N'Demolition - wallpaper stripping', 0, N'ENABLE-DEM', N'', N'item', 1.0000, 600.0000, 600.0000, N'', 7),
        (N'fc-vo-v12', @ProjectId, 3, N'', N'', N'V12', N'Additional radiators & towel rails with TRVs', 0, N'MEC-PLM', N'', N'item', 1.0000, 1200.0000, 1200.0000, N'', 8),
        (N'fc-vo-v13', @ProjectId, 3, N'', N'', N'V13', N'Remove existing ceilings, insulation & plasterboard', 0, N'INT-PLB', N'', N'item', 1.0000, 3136.0000, 3136.0000, N'', 9),
        (N'fc-vo-v14', @ProjectId, 3, N'', N'', N'V14', N'Oak cladding omit - MF wall track, plasterboard & decoration to entrance hallway', 0, N'INT-PLB', N'', N'item', 1.0000, 1205.0000, 1205.0000, N'', 10),
        (N'fc-vo-v15', @ProjectId, 3, N'', N'', N'V15', N'Double door & skirting revisions - glazed double door, new skirting throughout', 0, N'CARP-2FX', N'', N'item', 1.0000, 7007.0000, 7007.0000, N'', 11),
        (N'fc-vo-v16', @ProjectId, 3, N'', N'', N'V16', N'Utility stud walls, recessed units, LED strips & making good', 0, N'CARP-1FX', N'', N'item', 1.0000, 2626.0000, 2626.0000, N'', 12),
        (N'fc-vo-v17', @ProjectId, 3, N'', N'', N'V17', N'FF ensuite recess unit & LED strip', 0, N'CARP-1FX', N'', N'item', 1.0000, 740.0000, 740.0000, N'', 13),
        (N'fc-vo-v20', @ProjectId, 3, N'', N'', N'V20', N'Garage Doors - D-03', 0, N'WDR-GAR', N'', N'item', 1.0000, 4235.0000, 4235.0000, N'', 14),
        (N'fc-vo-v21', @ProjectId, 3, N'', N'', N'V21', N'Radiator supply revision - Milano Aruba', 0, N'MEC-PLM', N'', N'item', 1.0000, 2460.0000, 2460.0000, N'', 15),
        (N'fc-vo-v22', @ProjectId, 3, N'', N'', N'V22', N'Wetroom kit & linear drain - ensuite', 0, N'SUP-SAN', N'', N'item', 1.0000, 675.0000, 675.0000, N'', 16),
        (N'fc-vo-v23', @ProjectId, 3, N'', N'', N'V23', N'Glazed balustrade to first floor landing - omit', 2, N'STR-GRL', N'', N'item', 1.0000, -1200.0000, -1200.0000, N'', 17),
        (N'fc-vo-v24', @ProjectId, 3, N'', N'', N'V24', N'Under stairs cupboard rework - fire lining, flooring & decoration', 0, N'CARP-JNR', N'', N'item', 1.0000, 1571.0000, 1571.0000, N'', 18),
        (N'fc-vo-v25', @ProjectId, 3, N'', N'', N'V25', N'Utility refit - Howdens quote F04/0362934, installation & finishes', 0, N'SUP-KIT', N'', N'item', 1.0000, 6469.0000, 6469.0000, N'', 19),
        (N'fc-vo-v26', @ProjectId, 3, N'', N'', N'V26', N'Prepare & decorate existing staircase - omit', 2, N'DEC-STD', N'', N'item', 1.0000, -850.0000, -850.0000, N'', 20),
        (N'fc-vo-v27', @ProjectId, 3, N'', N'', N'V27', N'MDF window boards - additional allowance', 0, N'CARP-2FX', N'', N'item', 1.0000, 308.0000, 308.0000, N'', 21),
        (N'fc-vo-v28', @ProjectId, 3, N'', N'', N'V28', N'Supply of Tile Trims', 0, N'SUP-TIL', N'', N'item', 1.0000, 95.0000, 95.0000, N'', 22),
        (N'fc-vo-v30', @ProjectId, 3, N'', N'', N'V30', N'Front entrance porch tiling - primer & fix only', 0, N'TIL-STD', N'', N'item', 1.0000, 276.0000, 276.0000, N'', 23),
        (N'fc-vo-v31', @ProjectId, 3, N'', N'', N'V31', N'Study electrical alterations & redecoration', 0, N'ELE-STD', N'', N'item', 1.0000, 1904.0000, 1904.0000, N'', 24),
        (N'fc-vo-v32', @ProjectId, 3, N'', N'', N'V32', N'Remedial Works - Leaks FF', 0, N'MEC-PLM', N'', N'item', 1.0000, 930.0000, 930.0000, N'', 25),
        (N'fc-vo-v33', @ProjectId, 3, N'', N'', N'V33', N'Howdens additional units - supply & installation', 0, N'SUP-KIT', N'', N'item', 1.0000, 1635.0000, 1635.0000, N'', 26),
        (N'fc-vo-v37', @ProjectId, 3, N'', N'', N'V37', N'Decoration of the staircase', 0, N'DEC-STD', N'', N'item', 1.0000, 830.0000, 830.0000, N'', 27),
        (N'fc-vo-v38', @ProjectId, 3, N'', N'', N'V38', N'Letterbox - Supply & Installation', 0, N'SUP-IRO', N'', N'item', 1.0000, 379.5000, 379.5000, N'', 28)
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

    PRINT '6 Forest Crescent: variation orders & variation report lines merged.';
    COMMIT TRAN;
END
GO

-- Sanity check: variation lines should reconcile to the workbook register.
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '6forestcrescent'
       OR LOWER(REPLACE(Name, ' ', '')) = '6forestcrescent'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '6forestcrescent' THEN 0 ELSE 1 END);
SELECT
    (SELECT COUNT(*) FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = @ProjectId)                                    AS VariationOrders,     -- 38 (V01..V39, no V19)
    (SELECT COUNT(*) FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = @ProjectId AND Status = 2)                     AS ApprovedVos,         -- 28
    (SELECT COUNT(*) FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = @ProjectId AND Status = 3)                     AS DeclinedVos,         -- 10
    (SELECT COUNT(*)         FROM [dbo].[ValuationLineItems] WHERE ProjectId = @ProjectId AND ElementType = 3)          AS VariationLines,      -- 28
    (SELECT SUM(LineAmount)  FROM [dbo].[ValuationLineItems]
      WHERE ProjectId = @ProjectId AND ElementType = 3 AND LineType NOT IN (3, 4))                                      AS NetVariations;       -- 44434.50
-- Combined check: original Contract Sum + Net Variations = Revised Contract Sum.
SELECT
    SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,    -- 174706.00
    SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations,  --  44434.50
    SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                         -- 219140.50
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = @ProjectId;
GO
