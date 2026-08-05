-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql. If a code is not in that master it is NOT a cost
-- code.
-- Seed: Metropolitan Crescent -- Variation Orders (Valuation Report variation lines)
-- ----------------------------------------------------------------------------
-- Project : Flat 1, 3 Metropolitan Crescent, Crescent Lane, London, SW4 9BF
-- ProjectId: resolved at run time by site-name matcher 'metropolitancrescent'
--
-- Companion to seed-metropolitancrescent-valuation.sql, which seeds ONLY the
-- original contract scope (Contract works / PS / Contingency = Contract Sum
-- GBP 138,014.00). This file adds the post-contract VARIATION ORDERS from the
-- "Valuation 7 - 12 Month Retention" workbook, reconciling to the register:
--
--     Contract Sum            GBP 138,014.00
--     Net Variations          GBP  25,264.20
--     ----------------------------------------
--     Revised Contract Sum    GBP 163,278.20   (the workbook's "Live Build Sum")
--
-- MODEL NOTE
-- Each workbook VO is split into multiple priced rows (omits of contract/PS
-- scope as negatives, new items as positives). On the JPMS valuation report a
-- VO shows as a SINGLE summary line, so we seed ONE ValuationLineItem per
-- APPROVED VO whose LineAmount is the NET of that VO's workbook rows
-- (Quantity 1 x Rate = net). VariationRef (V01..V35) is the code shown on the
-- report; VariationTitle is a headline for the VO's scope.
--
-- Post 20260723120000_UnifyVariationOrders each variation order is ONE row in
-- [VariationOrderQuotes] (the [VariationOrders] table was dropped): approved
-- VOs carry Status 2 with VariationRef/Value; declined VOs carry Status 3.
--
-- V09 (Pressalit powered basin, GBP 3,346.57), V36 (main bathroom shelving,
-- GBP 1,405.00) and V37 (fold down seat installation, GBP 270.00) are wholly
-- DECLINED in the workbook: seeded as Status 3 VOQ rows with the quoted
-- EstimatedValue, no VariationRef, Value 0 and NO valuation line -- they are
-- excluded from the register net. V29 nets to exactly 0.00 (omit -465.00 /
-- new +465.00) and is seeded approved with a 0.00 line, per the register.
--
-- The register's own footer "Total Works Complete GBP 19,759.30" omits the
-- Claim 6 column (GBP 5,504.90); the stated Net Variations GBP 25,264.20 is
-- the sum of the claims row AND of the 34 approved VO nets, so this file
-- reconciles to GBP 25,264.20 exactly with no penny adjustment.
--
-- Approval dates are seeded per each VO's first claimed valuation month
-- (workbook gives no dates; valuations assumed monthly Mar-Aug 2025).
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation  (all lines here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net >= 0 -> Priced (addition)
--   * net <  0 -> Omit   (net reduction; stored as a negative LineAmount)
--
-- VOQ Status : 0=Quoting 1=Issued ... 2=Approved 3=Rejected (unified model:
--              Status 2 = approved variation, Status 3 = declined)
--
-- Idempotent: keyed on stable ids (mc-voq-vNN / mc-vo-vNN) via MERGE. The
-- contract/PS/contingency lines seeded by the valuation file are left
-- untouched. Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'metropolitancrescent'
       OR LOWER(REPLACE(Name, ' ', '')) = 'metropolitancrescent'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'metropolitancrescent' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  Metropolitan Crescent -- no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[VariationOrderQuotes] AS target
    USING (VALUES
        (N'mc-voq-v01', @ProjectId, N'', 1, N'VOQ-0001', N'En-suite bathroom & bedroom ceilings - remove & renew', N'En-suite bathroom & bedroom ceilings - remove & renew', 2, NULL, NULL, 1800.0000, N'V01', 1800.0000, N'INT-PLB', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v02', @ProjectId, N'', 2, N'VOQ-0002', N'Drainage survey & jetting in lieu of CCTV provisional sum', N'Drainage survey & jetting in lieu of CCTV provisional sum', 2, NULL, NULL, -4355.0000, N'V02', -4355.0000, N'MEC-DRN', '2025-02-18', N'seed@jewelgroup.co.uk', '2025-02-25', '2025-03-11', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v03', @ProjectId, N'', 3, N'VOQ-0003', N'Aluminium bifold door D02 - supply in lieu of provisional sum', N'Aluminium bifold door D02 - supply in lieu of provisional sum', 2, NULL, NULL, 464.6900, N'V03', 464.6900, N'WDR-ALU', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v04', @ProjectId, N'', 4, N'VOQ-0004', N'Howdens kitchen - supply, appliances & installation', N'Howdens kitchen - supply, appliances & installation', 2, NULL, NULL, 28435.8200, N'V04', 28435.8200, N'SUP-KIT', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v05', @ProjectId, N'', 5, N'VOQ-0005', N'On the Level wetroom & sanitary ware in lieu of shower seat PS', N'On the Level wetroom & sanitary ware in lieu of shower seat PS', 2, NULL, NULL, 4498.3800, N'V05', 4498.3800, N'SUP-SAN', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v06', @ProjectId, N'', 6, N'VOQ-0006', N'Kitchen fused spurs & double sockets', N'Kitchen fused spurs & double sockets', 2, NULL, NULL, 1490.0000, N'V06', 1490.0000, N'ELE-STD', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v07', @ProjectId, N'', 7, N'VOQ-0007', N'Fix only grab rails', N'Fix only grab rails', 2, NULL, NULL, 450.0000, N'V07', 450.0000, N'SUP-SAN', '2025-04-18', N'seed@jewelgroup.co.uk', '2025-04-25', '2025-05-09', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v08', @ProjectId, N'', 8, N'VOQ-0008', N'Supply of tiles in lieu of wall tiling provisional sum', N'Supply of tiles in lieu of wall tiling provisional sum', 2, NULL, NULL, -534.3000, N'V08', -534.3000, N'SUP-TIL', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v09', @ProjectId, N'', 9, N'VOQ-0009', N'Pressalit Powered Basin', N'Pressalit Powered Basin', 3, NULL, NULL, 3346.5700, NULL, 0.0000, NULL, '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', NULL, NULL, '2025-04-01'),
        (N'mc-voq-v10', @ProjectId, N'', 10, N'VOQ-0010', N'Lounge nib wall', N'Lounge nib wall', 2, NULL, NULL, 584.0000, N'V10', 584.0000, N'CARP-1FX', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v11', @ProjectId, N'', 11, N'VOQ-0011', N'Shower waste & SVP connections', N'Shower waste & SVP connections', 2, NULL, NULL, 1140.0000, N'V11', 1140.0000, N'MEC-DRN', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v12', @ProjectId, N'', 12, N'VOQ-0012', N'Kitchen walls - insulation, ply & plasterboard', N'Kitchen walls - insulation, ply & plasterboard', 2, NULL, NULL, 1500.0000, N'V12', 1500.0000, N'INT-PLB', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v13', @ProjectId, N'', 13, N'VOQ-0013', N'Wetroom - studwork, insulation, ply & plasterboard', N'Wetroom - studwork, insulation, ply & plasterboard', 2, NULL, NULL, 1964.0000, N'V13', 1964.0000, N'CARP-1FX', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v14', @ProjectId, N'', 14, N'VOQ-0014', N'Family bathroom - studwork, insulation & plaster', N'Family bathroom - studwork, insulation & plaster', 2, NULL, NULL, 944.0000, N'V14', 944.0000, N'CARP-1FX', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v15', @ProjectId, N'', 15, N'VOQ-0015', N'Wood flooring to hallway & kitchen in lieu of Karndean', N'Wood flooring to hallway & kitchen in lieu of Karndean', 2, NULL, NULL, 1393.0000, N'V15', 1393.0000, N'FLR-WD', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v16', @ProjectId, N'', 16, N'VOQ-0016', N'Additional feed for oven', N'Additional feed for oven', 2, NULL, NULL, 225.0000, N'V16', 225.0000, N'ELE-STD', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v17', @ProjectId, N'', 17, N'VOQ-0017', N'300mm upstand and edging strips', N'300mm upstand and edging strips', 2, NULL, NULL, 293.4800, N'V17', 293.4800, N'SUP-KIT', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v18', @ProjectId, N'', 18, N'VOQ-0018', N'FD60S apartment doorset & ramped hardwood threshold', N'FD60S apartment doorset & ramped hardwood threshold', 2, NULL, NULL, 3133.0000, N'V18', 3133.0000, N'CARP-DOR', '2025-04-18', N'seed@jewelgroup.co.uk', '2025-04-25', '2025-05-09', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v19', @ProjectId, N'', 19, N'VOQ-0019', N'Radiators relocation provisional sum - omit', N'Radiators relocation provisional sum - omit', 2, NULL, NULL, -1000.0000, N'V19', -1000.0000, N'MEC-PLM', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v20', @ProjectId, N'', 20, N'VOQ-0020', N'Built-in wardrobes, kitchen & utility provisional sum - omit', N'Built-in wardrobes, kitchen & utility provisional sum - omit', 2, NULL, NULL, -5000.0000, N'V20', -5000.0000, N'CARP-WRD', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v21', @ProjectId, N'', 21, N'VOQ-0021', N'Utility cupboard provisional sum - omit', N'Utility cupboard provisional sum - omit', 2, NULL, NULL, -2500.0000, N'V21', -2500.0000, N'CARP-JNR', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v22', @ProjectId, N'', 22, N'VOQ-0022', N'Utility cupboard bifolding doors - omit', N'Utility cupboard bifolding doors - omit', 2, NULL, NULL, -760.0000, N'V22', -760.0000, N'CARP-DOR', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v23', @ProjectId, N'', 23, N'VOQ-0023', N'Block entrance doorway cill works - omit', N'Block entrance doorway cill works - omit', 2, NULL, NULL, -375.0000, N'V23', -375.0000, N'CARP-2FX', '2025-03-18', N'seed@jewelgroup.co.uk', '2025-03-25', '2025-04-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v24', @ProjectId, N'', 24, N'VOQ-0024', N'Whole house water filter in lieu of water softener', N'Whole house water filter in lieu of water softener', 2, NULL, NULL, 380.0000, N'V24', 380.0000, N'MEC-PLM', '2025-04-18', N'seed@jewelgroup.co.uk', '2025-04-25', '2025-05-09', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v25', @ProjectId, N'', 25, N'VOQ-0025', N'Door ironmongery per finishes schedule in lieu of PS', N'Door ironmongery per finishes schedule in lieu of PS', 2, NULL, NULL, -825.0000, N'V25', -825.0000, N'SUP-IRO', '2025-04-18', N'seed@jewelgroup.co.uk', '2025-04-25', '2025-05-09', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v26', @ProjectId, N'', 26, N'VOQ-0026', N'Soft landscaping with turf & Aco drain', N'Soft landscaping with turf & Aco drain', 2, NULL, NULL, 1540.0000, N'V26', 1540.0000, N'EXTW-LND', '2025-04-18', N'seed@jewelgroup.co.uk', '2025-04-25', '2025-05-09', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v27', @ProjectId, N'', 27, N'VOQ-0027', N'Hillarys blinds & curtains in lieu of provisional sum', N'Hillarys blinds & curtains in lieu of provisional sum', 2, NULL, NULL, -4745.3200, N'V27', -4745.3200, N'WIN-BLD', '2025-06-18', N'seed@jewelgroup.co.uk', '2025-06-25', '2025-07-09', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v28', @ProjectId, N'', 28, N'VOQ-0028', N'Contingency Budget - omit', N'Contingency Budget - omit', 2, NULL, NULL, -15000.0000, N'V28', -15000.0000, N'HAND-MSC', '2025-04-18', N'seed@jewelgroup.co.uk', '2025-04-25', '2025-05-09', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v29', @ProjectId, N'', 29, N'VOQ-0029', N'Removal of external doors in lieu of bifold removal PS', N'Removal of external doors in lieu of bifold removal PS', 2, NULL, NULL, 0.0000, N'V29', 0.0000, N'ENABLE-DEM', '2025-05-18', N'seed@jewelgroup.co.uk', '2025-05-25', '2025-06-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v30', @ProjectId, N'', 30, N'VOQ-0030', N'Grab rails & shower bracket - supply & fix', N'Grab rails & shower bracket - supply & fix', 2, NULL, NULL, 624.5500, N'V30', 624.5500, N'SUP-SAN', '2025-05-18', N'seed@jewelgroup.co.uk', '2025-05-25', '2025-06-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v31', @ProjectId, N'', 31, N'VOQ-0031', N'Self levelling - bathrooms 25mm', N'Self levelling - bathrooms 25mm', 2, NULL, NULL, 840.0000, N'V31', 840.0000, N'FLR-SLF', '2025-05-18', N'seed@jewelgroup.co.uk', '2025-05-25', '2025-06-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v32', @ProjectId, N'', 32, N'VOQ-0032', N'Pull down hanging rails - Bedroom 1', N'Pull down hanging rails - Bedroom 1', 2, NULL, NULL, 280.0000, N'V32', 280.0000, N'CARP-WRD', '2025-06-18', N'seed@jewelgroup.co.uk', '2025-06-25', '2025-07-09', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v33', @ProjectId, N'', 33, N'VOQ-0033', N'Kitchen additional works', N'Kitchen additional works', 2, NULL, NULL, 1209.0000, N'V33', 1209.0000, N'CARP-KIT', '2025-06-18', N'seed@jewelgroup.co.uk', '2025-06-25', '2025-07-09', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v34', @ProjectId, N'', 34, N'VOQ-0034', N'Rear garden wall decoration', N'Rear garden wall decoration', 2, NULL, NULL, 1665.0000, N'V34', 1665.0000, N'DEC-STD', '2025-06-18', N'seed@jewelgroup.co.uk', '2025-06-25', '2025-07-09', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v35', @ProjectId, N'', 35, N'VOQ-0035', N'12 month retention - remedial & additional works', N'12 month retention - remedial & additional works', 2, NULL, NULL, 5504.9000, N'V35', 5504.9000, N'SUP-SAN', '2025-07-18', N'seed@jewelgroup.co.uk', '2025-07-25', '2025-08-08', N'seed@jewelgroup.co.uk', NULL),
        (N'mc-voq-v36', @ProjectId, N'', 36, N'VOQ-0036', N'Shelving in the main bathroom', N'Shelving in the main bathroom', 3, NULL, NULL, 1405.0000, NULL, 0.0000, NULL, '2025-07-18', N'seed@jewelgroup.co.uk', '2025-07-25', NULL, NULL, '2025-08-01'),
        (N'mc-voq-v37', @ProjectId, N'', 37, N'VOQ-0037', N'Installation of fold down seat', N'Installation of fold down seat', 3, NULL, NULL, 270.0000, NULL, 0.0000, NULL, '2025-07-18', N'seed@jewelgroup.co.uk', '2025-07-25', NULL, NULL, '2025-08-01')
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
        (N'mc-vo-v01', @ProjectId, 3, N'', N'', N'V01', N'En-suite bathroom & bedroom ceilings - remove & renew', 0, N'INT-PLB', N'', N'item', 1.0000, 1800.0000, 1800.0000, N'', 1),
        (N'mc-vo-v02', @ProjectId, 3, N'', N'', N'V02', N'Drainage survey & jetting in lieu of CCTV provisional sum', 2, N'MEC-DRN', N'', N'item', 1.0000, -4355.0000, -4355.0000, N'', 2),
        (N'mc-vo-v03', @ProjectId, 3, N'', N'', N'V03', N'Aluminium bifold door D02 - supply in lieu of provisional sum', 0, N'WDR-ALU', N'', N'item', 1.0000, 464.6900, 464.6900, N'', 3),
        (N'mc-vo-v04', @ProjectId, 3, N'', N'', N'V04', N'Howdens kitchen - supply, appliances & installation', 0, N'SUP-KIT', N'', N'item', 1.0000, 28435.8200, 28435.8200, N'', 4),
        (N'mc-vo-v05', @ProjectId, 3, N'', N'', N'V05', N'On the Level wetroom & sanitary ware in lieu of shower seat PS', 0, N'SUP-SAN', N'', N'item', 1.0000, 4498.3800, 4498.3800, N'', 5),
        (N'mc-vo-v06', @ProjectId, 3, N'', N'', N'V06', N'Kitchen fused spurs & double sockets', 0, N'ELE-STD', N'', N'item', 1.0000, 1490.0000, 1490.0000, N'', 6),
        (N'mc-vo-v07', @ProjectId, 3, N'', N'', N'V07', N'Fix only grab rails', 0, N'SUP-SAN', N'', N'item', 1.0000, 450.0000, 450.0000, N'', 7),
        (N'mc-vo-v08', @ProjectId, 3, N'', N'', N'V08', N'Supply of tiles in lieu of wall tiling provisional sum', 2, N'SUP-TIL', N'', N'item', 1.0000, -534.3000, -534.3000, N'', 8),
        (N'mc-vo-v10', @ProjectId, 3, N'', N'', N'V10', N'Lounge nib wall', 0, N'CARP-1FX', N'', N'item', 1.0000, 584.0000, 584.0000, N'', 9),
        (N'mc-vo-v11', @ProjectId, 3, N'', N'', N'V11', N'Shower waste & SVP connections', 0, N'MEC-DRN', N'', N'item', 1.0000, 1140.0000, 1140.0000, N'', 10),
        (N'mc-vo-v12', @ProjectId, 3, N'', N'', N'V12', N'Kitchen walls - insulation, ply & plasterboard', 0, N'INT-PLB', N'', N'item', 1.0000, 1500.0000, 1500.0000, N'', 11),
        (N'mc-vo-v13', @ProjectId, 3, N'', N'', N'V13', N'Wetroom - studwork, insulation, ply & plasterboard', 0, N'CARP-1FX', N'', N'item', 1.0000, 1964.0000, 1964.0000, N'', 12),
        (N'mc-vo-v14', @ProjectId, 3, N'', N'', N'V14', N'Family bathroom - studwork, insulation & plaster', 0, N'CARP-1FX', N'', N'item', 1.0000, 944.0000, 944.0000, N'', 13),
        (N'mc-vo-v15', @ProjectId, 3, N'', N'', N'V15', N'Wood flooring to hallway & kitchen in lieu of Karndean', 0, N'FLR-WD', N'', N'item', 1.0000, 1393.0000, 1393.0000, N'', 14),
        (N'mc-vo-v16', @ProjectId, 3, N'', N'', N'V16', N'Additional feed for oven', 0, N'ELE-STD', N'', N'item', 1.0000, 225.0000, 225.0000, N'', 15),
        (N'mc-vo-v17', @ProjectId, 3, N'', N'', N'V17', N'300mm upstand and edging strips', 0, N'SUP-KIT', N'', N'item', 1.0000, 293.4800, 293.4800, N'', 16),
        (N'mc-vo-v18', @ProjectId, 3, N'', N'', N'V18', N'FD60S apartment doorset & ramped hardwood threshold', 0, N'CARP-DOR', N'', N'item', 1.0000, 3133.0000, 3133.0000, N'', 17),
        (N'mc-vo-v19', @ProjectId, 3, N'', N'', N'V19', N'Radiators relocation provisional sum - omit', 2, N'MEC-PLM', N'', N'item', 1.0000, -1000.0000, -1000.0000, N'', 18),
        (N'mc-vo-v20', @ProjectId, 3, N'', N'', N'V20', N'Built-in wardrobes, kitchen & utility provisional sum - omit', 2, N'CARP-WRD', N'', N'item', 1.0000, -5000.0000, -5000.0000, N'', 19),
        (N'mc-vo-v21', @ProjectId, 3, N'', N'', N'V21', N'Utility cupboard provisional sum - omit', 2, N'CARP-JNR', N'', N'item', 1.0000, -2500.0000, -2500.0000, N'', 20),
        (N'mc-vo-v22', @ProjectId, 3, N'', N'', N'V22', N'Utility cupboard bifolding doors - omit', 2, N'CARP-DOR', N'', N'item', 1.0000, -760.0000, -760.0000, N'', 21),
        (N'mc-vo-v23', @ProjectId, 3, N'', N'', N'V23', N'Block entrance doorway cill works - omit', 2, N'CARP-2FX', N'', N'item', 1.0000, -375.0000, -375.0000, N'', 22),
        (N'mc-vo-v24', @ProjectId, 3, N'', N'', N'V24', N'Whole house water filter in lieu of water softener', 0, N'MEC-PLM', N'', N'item', 1.0000, 380.0000, 380.0000, N'', 23),
        (N'mc-vo-v25', @ProjectId, 3, N'', N'', N'V25', N'Door ironmongery per finishes schedule in lieu of PS', 2, N'SUP-IRO', N'', N'item', 1.0000, -825.0000, -825.0000, N'', 24),
        (N'mc-vo-v26', @ProjectId, 3, N'', N'', N'V26', N'Soft landscaping with turf & Aco drain', 0, N'EXTW-LND', N'', N'item', 1.0000, 1540.0000, 1540.0000, N'', 25),
        (N'mc-vo-v27', @ProjectId, 3, N'', N'', N'V27', N'Hillarys blinds & curtains in lieu of provisional sum', 2, N'WIN-BLD', N'', N'item', 1.0000, -4745.3200, -4745.3200, N'', 26),
        (N'mc-vo-v28', @ProjectId, 3, N'', N'', N'V28', N'Contingency Budget - omit', 2, N'HAND-MSC', N'', N'item', 1.0000, -15000.0000, -15000.0000, N'', 27),
        (N'mc-vo-v29', @ProjectId, 3, N'', N'', N'V29', N'Removal of external doors in lieu of bifold removal PS', 0, N'ENABLE-DEM', N'', N'item', 1.0000, 0.0000, 0.0000, N'Net zero: omit -465.00 / new +465.00', 28),
        (N'mc-vo-v30', @ProjectId, 3, N'', N'', N'V30', N'Grab rails & shower bracket - supply & fix', 0, N'SUP-SAN', N'', N'item', 1.0000, 624.5500, 624.5500, N'', 29),
        (N'mc-vo-v31', @ProjectId, 3, N'', N'', N'V31', N'Self levelling - bathrooms 25mm', 0, N'FLR-SLF', N'', N'item', 1.0000, 840.0000, 840.0000, N'', 30),
        (N'mc-vo-v32', @ProjectId, 3, N'', N'', N'V32', N'Pull down hanging rails - Bedroom 1', 0, N'CARP-WRD', N'', N'item', 1.0000, 280.0000, 280.0000, N'', 31),
        (N'mc-vo-v33', @ProjectId, 3, N'', N'', N'V33', N'Kitchen additional works', 0, N'CARP-KIT', N'', N'item', 1.0000, 1209.0000, 1209.0000, N'', 32),
        (N'mc-vo-v34', @ProjectId, 3, N'', N'', N'V34', N'Rear garden wall decoration', 0, N'DEC-STD', N'', N'item', 1.0000, 1665.0000, 1665.0000, N'', 33),
        (N'mc-vo-v35', @ProjectId, 3, N'', N'', N'V35', N'12 month retention - remedial & additional works', 0, N'SUP-SAN', N'', N'item', 1.0000, 5504.9000, 5504.9000, N'', 34)
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

    PRINT 'Metropolitan Crescent: variation orders & variation lines merged.';

    -- Sanity check: variation lines should reconcile to the workbook register.
    SELECT
        COUNT(*) AS VariationLines,                                                       -- 34
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations, -- 25264.20
        SUM(LineAmount) AS GrossOfAllVoLines                                              -- 25264.20
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType = 3;

    -- Combined check: Contract Sum + Net Variations = Revised Contract Sum.
    SELECT
        SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,   -- 138014.00
        SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations, --  25264.20
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                        -- 163278.20
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId;

    COMMIT TRAN;
END
GO
