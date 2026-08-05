-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per
-- JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: Cornerways East -- Variation Orders (Valuation Report variation lines)
-- ----------------------------------------------------------------------------
-- Project : Cornerways East, Ewell KT17 3ER
-- ProjectId: resolved at run time by site-name matcher 'cornerwayseastewellkt173er'
--
-- Companion to seed-cornerways-valuation.sql, which seeds ONLY the original
-- contract scope (Contract works / PC Sums / Contingency = Contract Sum
-- GBP 641,884.00). This file adds the post-contract VARIATION ORDERS from the
-- "Valuation 22 - Retention Release" workbook, reconciling to the workbook's
-- variations register:
--
--     Net Variations          GBP 112,817.16
--     Contract Sum            GBP 641,884.00
--     ----------------------------------------
--     Revised Contract Sum    GBP 754,701.16
--
-- MODEL NOTE (unified variation orders, post 20260723120000_UnifyVariationOrders)
-- Each workbook VO is split into multiple priced lines (omits of contract/PS
-- scope as negatives, new items as positives). On the JPMS valuation report a
-- VO shows as a SINGLE summary line, so we seed ONE ValuationLineItem per
-- APPROVED VO whose LineAmount is the NET of that VO's workbook lines
-- (Quantity 1 x Rate = net), plus ONE VariationOrderQuotes row per VO (the
-- single unified variation record). There is no separate [VariationOrders]
-- table any more.
--
-- Of the register's 84 VOs, 70 are approved (register net GBP 112,817.16 --
-- the register's own addition matches the stated figure exactly, no penny
-- adjustment needed) and 14 are DECLINED (V32, V33, V45, V46, V47, V52, V61,
-- V63, V70, V72, V76, V77, V78, V80): seeded Status 3 with the quoted amount
-- as EstimatedValue (NULL where the workbook prices nothing), Value 0, and NO
-- valuation line, so they never count toward totals.
--
-- Judgement calls:
--   * V26 nets the workbook's own within-VO decking/balustrade omit-and-
--     re-add to -190.00; the later re-add of the glass balustrade is its own
--     VO (V64, +3,660.00), per the register's labelling.
--   * V74 "Ensuite 2 - Floor tiles" shows rate -277.5 but amount -227.50; the
--     workbook AMOUNT is kept as the truth.
--   * Dates are seeded (workbook gives none): CreatedAt sits just before each
--     VO's first claimed valuation month (claim columns Jan-23..Jul-25),
--     IssuedAt ~7 days later, ApprovedAt at the start of the first claim
--     month; declined VOs get RejectedAt ~7 days after IssuedAt.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation  (all rows here = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net > 0  -> Priced (addition)
--   * net < 0  -> Omit   (net reduction; stored as a negative LineAmount)
--
-- Idempotent: keyed on stable ids (ce-vo-vNN / ce-voq-vNN) via MERGE. The
-- contract/PC/contingency lines seeded by seed-cornerways-valuation.sql are
-- left untouched. Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'cornerwayseastewellkt173er'
       OR LOWER(REPLACE(Name, ' ', '')) = 'cornerwayseastewellkt173er'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'cornerwayseastewellkt173er' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  Cornerways East -- no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[VariationOrderQuotes] AS target
    USING (VALUES
    (N'ce-voq-v01', @ProjectId, N'', 1, N'VOQ-0001', N'Asbestos removal - Goodbye Asbestos survey & removal', N'Asbestos removal - Goodbye Asbestos survey & removal', 2, NULL, NULL, -1480.0000, N'V01', -1480.0000, N'ENABLE-ASB', '2022-12-15', N'seed@jewelgroup.co.uk', '2022-12-22', '2023-01-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v02', @ProjectId, N'', 2, N'VOQ-0002', N'Pool side room demolition & new flat roof for hoists', N'Pool side room demolition & new flat roof for hoists', 2, NULL, NULL, 3770.0000, N'V02', 3770.0000, N'ENABLE-DEM', '2022-12-15', N'seed@jewelgroup.co.uk', '2022-12-22', '2023-01-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v03', @ProjectId, N'', 3, N'VOQ-0003', N'Drainage survey & report in lieu of PS', N'Drainage survey & report in lieu of PS', 2, NULL, NULL, -4590.0000, N'V03', -4590.0000, N'MEC-DRN', '2023-01-15', N'seed@jewelgroup.co.uk', '2023-01-22', '2023-02-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v04', @ProjectId, N'', 4, N'VOQ-0004', N'Rezai Consulting invoice & trial holes', N'Rezai Consulting invoice & trial holes', 2, NULL, NULL, 2220.0000, N'V04', 2220.0000, N'SUB-EXC', '2023-01-15', N'seed@jewelgroup.co.uk', '2023-01-22', '2023-02-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v05', @ProjectId, N'', 5, N'VOQ-0005', N'Underpinning - unpin existing strip footings', N'Underpinning - unpin existing strip footings', 2, NULL, NULL, 4300.0000, N'V05', 4300.0000, N'SUB-UND', '2023-01-15', N'seed@jewelgroup.co.uk', '2023-01-22', '2023-02-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v06', @ProjectId, N'', 6, N'VOQ-0006', N'Epsom & Ewell - Building Control fees', N'Epsom & Ewell - Building Control fees', 2, NULL, NULL, 919.0000, N'V06', 919.0000, N'HAND-SPE', '2023-01-15', N'seed@jewelgroup.co.uk', '2023-01-22', '2023-02-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v07', @ProjectId, N'', 7, N'VOQ-0007', N'Foundations redesign - excavation & concrete', N'Foundations redesign - excavation & concrete', 2, NULL, NULL, 11780.0000, N'V07', 11780.0000, N'SUB-EXC', '2023-02-15', N'seed@jewelgroup.co.uk', '2023-02-22', '2023-03-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v08', @ProjectId, N'', 8, N'VOQ-0008', N'Trenching for the gas & electric meter moves', N'Trenching for the gas & electric meter moves', 2, NULL, NULL, 340.0000, N'V08', 340.0000, N'UTIL-TRN', '2023-02-15', N'seed@jewelgroup.co.uk', '2023-02-22', '2023-03-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v09', @ProjectId, N'', 9, N'VOQ-0009', N'Rezai Consulting invoice - pool room works', N'Rezai Consulting invoice - pool room works', 2, NULL, NULL, 450.0000, N'V09', 450.0000, N'HAND-SPE', '2023-02-15', N'seed@jewelgroup.co.uk', '2023-02-22', '2023-03-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v10', @ProjectId, N'', 10, N'VOQ-0010', N'Structural engineers items - excavation, masonry & steels', N'Structural engineers items - excavation, masonry & steels', 2, NULL, NULL, 4825.0000, N'V10', 4825.0000, N'STR-STL', '2023-03-15', N'seed@jewelgroup.co.uk', '2023-03-22', '2023-04-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v11', @ProjectId, N'', 11, N'VOQ-0011', N'EOT-01 - site supervision, rubbish removal & H&S', N'EOT-01 - site supervision, rubbish removal & H&S', 2, NULL, NULL, 17200.0000, N'V11', 17200.0000, N'PRELIMS-SMG', '2023-12-15', N'seed@jewelgroup.co.uk', '2023-12-22', '2024-01-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v12', @ProjectId, N'', 12, N'VOQ-0012', N'Velux electric openable rooflight RL01', N'Velux electric openable rooflight RL01', 2, NULL, NULL, -1028.0000, N'V12', -1028.0000, N'WDR-SPG', '2023-05-15', N'seed@jewelgroup.co.uk', '2023-05-22', '2023-06-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v13', @ProjectId, N'', 13, N'VOQ-0013', N'Block & beam - bath loading capacity', N'Block & beam - bath loading capacity', 2, NULL, NULL, 1020.0000, N'V13', 1020.0000, N'SUB-CON', '2023-03-15', N'seed@jewelgroup.co.uk', '2023-03-22', '2023-04-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v14', @ProjectId, N'', 14, N'VOQ-0014', N'Fire & smoke detection revision', N'Fire & smoke detection revision', 2, NULL, NULL, -4005.0000, N'V14', -4005.0000, N'ELE-FIR', '2023-05-15', N'seed@jewelgroup.co.uk', '2023-05-22', '2023-06-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v15', @ProjectId, N'', 15, N'VOQ-0015', N'Rezai Consulting Invoice RC/678', N'Rezai Consulting Invoice RC/678', 2, NULL, NULL, 611.5200, N'V15', 611.5200, N'HAND-SPE', '2023-04-15', N'seed@jewelgroup.co.uk', '2023-04-22', '2023-05-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v16', @ProjectId, N'', 16, N'VOQ-0016', N'Alarm system - intruder & external CCTV', N'Alarm system - intruder & external CCTV', 2, NULL, NULL, -1606.0000, N'V16', -1606.0000, N'ELE-ALM', '2023-05-15', N'seed@jewelgroup.co.uk', '2023-05-22', '2023-06-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v17', @ProjectId, N'', 17, N'VOQ-0017', N'Poolroom structural revisions & roof structure', N'Poolroom structural revisions & roof structure', 2, NULL, NULL, 3795.0000, N'V17', 3795.0000, N'STR-STL', '2023-04-15', N'seed@jewelgroup.co.uk', '2023-04-22', '2023-05-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v18', @ProjectId, N'', 18, N'VOQ-0018', N'Garage gable end - tile hanging', N'Garage gable end - tile hanging', 2, NULL, NULL, 3420.0000, N'V18', 3420.0000, N'ROOF-TLN', '2023-04-15', N'seed@jewelgroup.co.uk', '2023-04-22', '2023-05-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v19', @ProjectId, N'', 19, N'VOQ-0019', N'CCTV - omit provisional sum', N'CCTV - omit provisional sum', 2, NULL, NULL, -5500.0000, N'V19', -5500.0000, N'ELE-CCT', '2023-04-15', N'seed@jewelgroup.co.uk', '2023-04-22', '2023-05-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v20', @ProjectId, N'', 20, N'VOQ-0020', N'Hard landscaping to the side path', N'Hard landscaping to the side path', 2, NULL, NULL, 5965.0000, N'V20', 5965.0000, N'EXTW-PAV', '2023-05-15', N'seed@jewelgroup.co.uk', '2023-05-22', '2023-06-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v21', @ProjectId, N'', 21, N'VOQ-0021', N'Rezai Consulting fee', N'Rezai Consulting fee', 2, NULL, NULL, 540.0000, N'V21', 540.0000, N'HAND-SPE', '2023-05-15', N'seed@jewelgroup.co.uk', '2023-05-22', '2023-06-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v22', @ProjectId, N'', 22, N'VOQ-0022', N'Poolroom steels & timber joist layout', N'Poolroom steels & timber joist layout', 2, NULL, NULL, 6350.0000, N'V22', 6350.0000, N'CARP-1FX', '2023-05-15', N'seed@jewelgroup.co.uk', '2023-05-22', '2023-06-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v23', @ProjectId, N'', 23, N'VOQ-0023', N'Structural works - Dwg Issue 11', N'Structural works - Dwg Issue 11', 2, NULL, NULL, 3525.0000, N'V23', 3525.0000, N'STR-STL', '2023-06-15', N'seed@jewelgroup.co.uk', '2023-06-22', '2023-07-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v24', @ProjectId, N'', 24, N'VOQ-0024', N'Gate call out charge', N'Gate call out charge', 2, NULL, NULL, 144.0000, N'V24', 144.0000, N'HAND-MSC', '2023-06-15', N'seed@jewelgroup.co.uk', '2023-06-22', '2023-07-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v25', @ProjectId, N'', 25, N'VOQ-0025', N'Chimney breast removal - roof level to ground', N'Chimney breast removal - roof level to ground', 2, NULL, NULL, 5380.0000, N'V25', 5380.0000, N'ENABLE-DEM', '2023-07-15', N'seed@jewelgroup.co.uk', '2023-07-22', '2023-08-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v26', @ProjectId, N'', 26, N'VOQ-0026', N'Terrace decking & balustrade re-sequence', N'Terrace decking & balustrade re-sequence', 2, NULL, NULL, -190.0000, N'V26', -190.0000, N'EXTW-DEK', '2023-06-15', N'seed@jewelgroup.co.uk', '2023-06-22', '2023-07-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v27', @ProjectId, N'', 27, N'VOQ-0027', N'Parapet build up, coping stones & K-render', N'Parapet build up, coping stones & K-render', 2, NULL, NULL, 5580.0000, N'V27', 5580.0000, N'MASON-BRK', '2023-06-15', N'seed@jewelgroup.co.uk', '2023-06-22', '2023-07-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v28', @ProjectId, N'', 28, N'VOQ-0028', N'EOT-02 - site supervision, rubbish removal & H&S', N'EOT-02 - site supervision, rubbish removal & H&S', 2, NULL, NULL, 6880.0000, N'V28', 6880.0000, N'PRELIMS-SMG', '2024-03-15', N'seed@jewelgroup.co.uk', '2024-03-22', '2024-04-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v29', @ProjectId, N'', 29, N'VOQ-0029', N'Taylor Dolman ceiling hoist in lieu of PS', N'Taylor Dolman ceiling hoist in lieu of PS', 2, NULL, NULL, -8220.0000, N'V29', -8220.0000, N'SPEC-LFT', '2023-06-15', N'seed@jewelgroup.co.uk', '2023-06-22', '2023-07-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v30', @ProjectId, N'', 30, N'VOQ-0030', N'GF solid & 30N blocks, Ancon ties to columns', N'GF solid & 30N blocks, Ancon ties to columns', 2, NULL, NULL, 2963.0000, N'V30', 2963.0000, N'MASON-BRK', '2023-07-15', N'seed@jewelgroup.co.uk', '2023-07-22', '2023-08-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v31', @ProjectId, N'', 31, N'VOQ-0031', N'M&E - additional sockets, pendants & recessed lights', N'M&E - additional sockets, pendants & recessed lights', 2, NULL, NULL, 3298.0000, N'V31', 3298.0000, N'ELE-STD', '2023-08-15', N'seed@jewelgroup.co.uk', '2023-08-22', '2023-09-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v32', @ProjectId, N'', 32, N'VOQ-0032', N'Catnic lintel, steel beam D03 & RSA to B2', N'Catnic lintel, steel beam D03 & RSA to B2', 3, NULL, NULL, 980.0000, NULL, 0.0000, NULL, '2023-08-15', N'seed@jewelgroup.co.uk', '2023-08-22', NULL, NULL, '2023-08-29'),
    (N'ce-voq-v33', @ProjectId, N'', 33, N'VOQ-0033', N'Masonry & paving', N'Masonry & paving', 3, NULL, NULL, 45037.0000, NULL, 0.0000, NULL, '2023-08-15', N'seed@jewelgroup.co.uk', '2023-08-22', NULL, NULL, '2023-08-29'),
    (N'ce-voq-v34', @ProjectId, N'', 34, N'VOQ-0034', N'Phase 2 works - strip out, steels, ensuite & M&E', N'Phase 2 works - strip out, steels, ensuite & M&E', 2, NULL, NULL, 59037.0000, N'V34', 59037.0000, N'PRELIMS-SMG', '2023-09-15', N'seed@jewelgroup.co.uk', '2023-09-22', '2023-10-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v35', @ProjectId, N'', 35, N'VOQ-0035', N'Poolroom foundations as per engineer drawing', N'Poolroom foundations as per engineer drawing', 2, NULL, NULL, 435.0000, N'V35', 435.0000, N'SUB-CON', '2023-09-15', N'seed@jewelgroup.co.uk', '2023-09-22', '2023-10-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v36', @ProjectId, N'', 36, N'VOQ-0036', N'Howdens kitchen & utility, quartz & installation', N'Howdens kitchen & utility, quartz & installation', 2, NULL, NULL, 18908.2300, N'V36', 18908.2300, N'SUP-KIT', '2023-11-15', N'seed@jewelgroup.co.uk', '2023-11-22', '2023-12-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v37', @ProjectId, N'', 37, N'VOQ-0037', N'FF - Ensuite structural works', N'FF - Ensuite structural works', 2, NULL, NULL, 1770.0000, N'V37', 1770.0000, N'STR-STL', '2023-11-15', N'seed@jewelgroup.co.uk', '2023-11-22', '2023-12-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v38', @ProjectId, N'', 38, N'VOQ-0038', N'On the Level - wetroom', N'On the Level - wetroom', 2, NULL, NULL, 4357.0000, N'V38', 4357.0000, N'WPF-INT', '2023-10-15', N'seed@jewelgroup.co.uk', '2023-10-22', '2023-11-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v39', @ProjectId, N'', 39, N'VOQ-0039', N'Loft hatch & boarding', N'Loft hatch & boarding', 2, NULL, NULL, 3810.0000, N'V39', 3810.0000, N'CARP-1FX', '2023-10-15', N'seed@jewelgroup.co.uk', '2023-10-22', '2023-11-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v40', @ProjectId, N'', 40, N'VOQ-0040', N'Render & paint rear extension in lieu of upstand', N'Render & paint rear extension in lieu of upstand', 2, NULL, NULL, -1400.0000, N'V40', -1400.0000, N'EXT-STC', '2023-10-15', N'seed@jewelgroup.co.uk', '2023-10-22', '2023-11-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v41', @ProjectId, N'', 41, N'VOQ-0041', N'Sanitary ware supply & fit - finishing schedule Rev I', N'Sanitary ware supply & fit - finishing schedule Rev I', 2, NULL, NULL, -287.8100, N'V41', -287.8100, N'SUP-SAN', '2023-10-15', N'seed@jewelgroup.co.uk', '2023-10-22', '2023-11-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v42', @ProjectId, N'', 42, N'VOQ-0042', N'GF studwork to the WC', N'GF studwork to the WC', 2, NULL, NULL, 420.0000, N'V42', 420.0000, N'CARP-1FX', '2023-10-15', N'seed@jewelgroup.co.uk', '2023-10-22', '2023-11-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v43', @ProjectId, N'', 43, N'VOQ-0043', N'EXD01/EXD02 doors & poolroom windows revision', N'EXD01/EXD02 doors & poolroom windows revision', 2, NULL, NULL, 8377.0000, N'V43', 8377.0000, N'WDR-ALU', '2023-12-15', N'seed@jewelgroup.co.uk', '2023-12-22', '2024-01-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v44', @ProjectId, N'', 44, N'VOQ-0044', N'Soundbloc plasterboard to hoist areas', N'Soundbloc plasterboard to hoist areas', 2, NULL, NULL, 954.8000, N'V44', 954.8000, N'INT-PLB', '2023-12-15', N'seed@jewelgroup.co.uk', '2023-12-22', '2024-01-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v45', @ProjectId, N'', 45, N'VOQ-0045', N'GF insulation between floor joists', N'GF insulation between floor joists', 3, NULL, NULL, NULL, NULL, 0.0000, NULL, '2023-12-15', N'seed@jewelgroup.co.uk', '2023-12-22', NULL, NULL, '2023-12-29'),
    (N'ce-voq-v46', @ProjectId, N'', 46, N'VOQ-0046', N'Wet UFH to entire GF', N'Wet UFH to entire GF', 3, NULL, NULL, NULL, NULL, 0.0000, NULL, '2023-12-15', N'seed@jewelgroup.co.uk', '2023-12-22', NULL, NULL, '2023-12-29'),
    (N'ce-voq-v47', @ProjectId, N'', 47, N'VOQ-0047', N'Solar panel removal & retiling', N'Solar panel removal & retiling', 3, NULL, NULL, NULL, NULL, 0.0000, NULL, '2023-12-15', N'seed@jewelgroup.co.uk', '2023-12-22', NULL, NULL, '2023-12-29'),
    (N'ce-voq-v48', @ProjectId, N'', 48, N'VOQ-0048', N'Supply, install & decorate staircase in lieu of PS', N'Supply, install & decorate staircase in lieu of PS', 2, NULL, NULL, 5325.0000, N'V48', 5325.0000, N'STAIR-TIM', '2023-11-15', N'seed@jewelgroup.co.uk', '2023-11-22', '2023-12-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v49', @ProjectId, N'', 49, N'VOQ-0049', N'Tile supply per schedule Rev I, adhesive & grout', N'Tile supply per schedule Rev I, adhesive & grout', 2, NULL, NULL, 2439.1200, N'V49', 2439.1200, N'SUP-TIL', '2024-01-15', N'seed@jewelgroup.co.uk', '2024-01-22', '2024-02-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v50', @ProjectId, N'', 50, N'VOQ-0050', N'Wall tiling install, mosaics & GF WC finishes', N'Wall tiling install, mosaics & GF WC finishes', 2, NULL, NULL, 11950.0000, N'V50', 11950.0000, N'TIL-STD', '2024-03-15', N'seed@jewelgroup.co.uk', '2024-03-22', '2024-04-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v51', @ProjectId, N'', 51, N'VOQ-0051', N'Electrics & heating per revised drawings', N'Electrics & heating per revised drawings', 2, NULL, NULL, 12415.0000, N'V51', 12415.0000, N'MEC-PLM', '2024-01-15', N'seed@jewelgroup.co.uk', '2024-01-22', '2024-02-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v52', @ProjectId, N'', 52, N'VOQ-0052', N'925 internal door & internal double doors', N'925 internal door & internal double doors', 3, NULL, NULL, 300.0000, NULL, 0.0000, NULL, '2024-01-15', N'seed@jewelgroup.co.uk', '2024-01-22', NULL, NULL, '2024-01-29'),
    (N'ce-voq-v53', @ProjectId, N'', 53, N'VOQ-0053', N'Bedroom 4 window opening', N'Bedroom 4 window opening', 2, NULL, NULL, 225.0000, N'V53', 225.0000, N'MASON-BRK', '2023-12-15', N'seed@jewelgroup.co.uk', '2023-12-22', '2024-01-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v54', @ProjectId, N'', 54, N'VOQ-0054', N'EOT-04 - site manager, H&S & rubbish removal', N'EOT-04 - site manager, H&S & rubbish removal', 2, NULL, NULL, 6880.0000, N'V54', 6880.0000, N'PRELIMS-SMG', '2024-06-15', N'seed@jewelgroup.co.uk', '2024-06-22', '2024-07-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v55', @ProjectId, N'', 55, N'VOQ-0055', N'Karndean herringbone flooring in lieu of contract flooring', N'Karndean herringbone flooring in lieu of contract flooring', 2, NULL, NULL, 11240.0000, N'V55', 11240.0000, N'FLR-LVT', '2024-01-15', N'seed@jewelgroup.co.uk', '2024-01-22', '2024-02-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v56', @ProjectId, N'', 56, N'VOQ-0056', N'Blinds & curtains - omit provisional sum', N'Blinds & curtains - omit provisional sum', 2, NULL, NULL, -17500.0000, N'V56', -17500.0000, N'WIN-BLD', '2024-01-15', N'seed@jewelgroup.co.uk', '2024-01-22', '2024-02-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v57', @ProjectId, N'', 57, N'VOQ-0057', N'External works omits - decking, balustrade & landscaping', N'External works omits - decking, balustrade & landscaping', 2, NULL, NULL, -24995.0000, N'V57', -24995.0000, N'EXTW-DEK', '2024-01-15', N'seed@jewelgroup.co.uk', '2024-01-22', '2024-02-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v58', @ProjectId, N'', 58, N'VOQ-0058', N'Contingency Budget - omit', N'Contingency Budget - omit', 2, NULL, NULL, -50000.0000, N'V58', -50000.0000, N'HAND-MSC', '2024-01-15', N'seed@jewelgroup.co.uk', '2024-01-22', '2024-02-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v59', @ProjectId, N'', 59, N'VOQ-0059', N'Decoration uplifts - black doors & glitter paint', N'Decoration uplifts - black doors & glitter paint', 2, NULL, NULL, 1230.0000, N'V59', 1230.0000, N'DEC-STD', '2024-04-15', N'seed@jewelgroup.co.uk', '2024-04-22', '2024-05-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v60', @ProjectId, N'', 60, N'VOQ-0060', N'Render & paint plinth in lieu of damp / roof cleaning PS', N'Render & paint plinth in lieu of damp / roof cleaning PS', 2, NULL, NULL, -1485.0000, N'V60', -1485.0000, N'WPF-DMP', '2024-03-15', N'seed@jewelgroup.co.uk', '2024-03-22', '2024-04-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v61', @ProjectId, N'', 61, N'VOQ-0061', N'Tile hanging TBC', N'Tile hanging TBC', 3, NULL, NULL, NULL, NULL, 0.0000, NULL, '2024-03-15', N'seed@jewelgroup.co.uk', '2024-03-22', NULL, NULL, '2024-03-29'),
    (N'ce-voq-v62', @ProjectId, N'', 62, N'VOQ-0062', N'Generator hire - 4 weeks', N'Generator hire - 4 weeks', 2, NULL, NULL, 2000.0000, N'V62', 2000.0000, N'ELE-STD', '2024-02-15', N'seed@jewelgroup.co.uk', '2024-02-22', '2024-03-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v63', @ProjectId, N'', 63, N'VOQ-0063', N'Bedroom electrics', N'Bedroom electrics', 3, NULL, NULL, NULL, NULL, 0.0000, NULL, '2024-02-15', N'seed@jewelgroup.co.uk', '2024-02-22', NULL, NULL, '2024-02-29'),
    (N'ce-voq-v64', @ProjectId, N'', 64, N'VOQ-0064', N'1100 mm glass balustrade to terrace - reinstated', N'1100 mm glass balustrade to terrace - reinstated', 2, NULL, NULL, 3660.0000, N'V64', 3660.0000, N'STR-GRL', '2024-03-15', N'seed@jewelgroup.co.uk', '2024-03-22', '2024-04-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v65', @ProjectId, N'', 65, N'VOQ-0065', N'Baywater Victrion vanity unit & restock charge', N'Baywater Victrion vanity unit & restock charge', 2, NULL, NULL, -35.0000, N'V65', -35.0000, N'SUP-SAN', '2024-03-15', N'seed@jewelgroup.co.uk', '2024-03-22', '2024-04-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v66', @ProjectId, N'', 66, N'VOQ-0066', N'Pocket door - Ensuite D25', N'Pocket door - Ensuite D25', 2, NULL, NULL, 325.0000, N'V66', 325.0000, N'CARP-DOR', '2024-05-15', N'seed@jewelgroup.co.uk', '2024-05-22', '2024-06-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v67', @ProjectId, N'', 67, N'VOQ-0067', N'Ironmongery per finishes schedule in lieu of PS', N'Ironmongery per finishes schedule in lieu of PS', 2, NULL, NULL, -820.0000, N'V67', -820.0000, N'SUP-IRO', '2024-03-15', N'seed@jewelgroup.co.uk', '2024-03-22', '2024-04-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v68', @ProjectId, N'', 68, N'VOQ-0068', N'Kitchen appliances - Fohen tap, Bosch & Samsung', N'Kitchen appliances - Fohen tap, Bosch & Samsung', 2, NULL, NULL, 5600.0000, N'V68', 5600.0000, N'SUP-APP', '2024-03-15', N'seed@jewelgroup.co.uk', '2024-03-22', '2024-04-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v69', @ProjectId, N'', 69, N'VOQ-0069', N'Quartz additional worktop', N'Quartz additional worktop', 2, NULL, NULL, 555.0000, N'V69', 555.0000, N'SUP-KIT', '2024-04-15', N'seed@jewelgroup.co.uk', '2024-04-22', '2024-05-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v70', @ProjectId, N'', 70, N'VOQ-0070', N'Ensuite 1 pocket door, framing & finishes', N'Ensuite 1 pocket door, framing & finishes', 3, NULL, NULL, 3620.0000, NULL, 0.0000, NULL, '2024-04-15', N'seed@jewelgroup.co.uk', '2024-04-22', NULL, NULL, '2024-04-29'),
    (N'ce-voq-v71', @ProjectId, N'', 71, N'VOQ-0071', N'Electric cabling for AC provision in lieu of PS', N'Electric cabling for AC provision in lieu of PS', 2, NULL, NULL, -2450.0000, N'V71', -2450.0000, N'MEC-AC', '2024-04-15', N'seed@jewelgroup.co.uk', '2024-04-22', '2024-05-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v72', @ProjectId, N'', 72, N'VOQ-0072', N'Poolroom plasterboard & T&G flooring', N'Poolroom plasterboard & T&G flooring', 3, NULL, NULL, 3475.0000, NULL, 0.0000, NULL, '2024-04-15', N'seed@jewelgroup.co.uk', '2024-04-22', NULL, NULL, '2024-04-29'),
    (N'ce-voq-v73', @ProjectId, N'', 73, N'VOQ-0073', N'Main bathroom - electric UFH & client supplied tiles', N'Main bathroom - electric UFH & client supplied tiles', 2, NULL, NULL, 1430.0000, N'V73', 1430.0000, N'TIL-STD', '2024-04-15', N'seed@jewelgroup.co.uk', '2024-04-22', '2024-05-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v74', @ProjectId, N'', 74, N'VOQ-0074', N'Ensuite wall & floor tile omits', N'Ensuite wall & floor tile omits', 2, NULL, NULL, -1431.5000, N'V74', -1431.5000, N'SUP-TIL', '2024-04-15', N'seed@jewelgroup.co.uk', '2024-04-22', '2024-05-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v75', @ProjectId, N'', 75, N'VOQ-0075', N'Wardrobes, shelving, storage - omit provisional sum', N'Wardrobes, shelving, storage - omit provisional sum', 2, NULL, NULL, -20000.0000, N'V75', -20000.0000, N'CARP-WRD', '2024-04-15', N'seed@jewelgroup.co.uk', '2024-04-22', '2024-05-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v76', @ProjectId, N'', 76, N'VOQ-0076', N'Glazed sliding barn door Ensuite 1', N'Glazed sliding barn door Ensuite 1', 3, NULL, NULL, 1087.0000, NULL, 0.0000, NULL, '2024-05-15', N'seed@jewelgroup.co.uk', '2024-05-22', NULL, NULL, '2024-05-29'),
    (N'ce-voq-v77', @ProjectId, N'', 77, N'VOQ-0077', N'Entrance steps & ramp', N'Entrance steps & ramp', 3, NULL, NULL, NULL, NULL, 0.0000, NULL, '2024-05-15', N'seed@jewelgroup.co.uk', '2024-05-22', NULL, NULL, '2024-05-29'),
    (N'ce-voq-v78', @ProjectId, N'', 78, N'VOQ-0078', N'Howdens larder units', N'Howdens larder units', 3, NULL, NULL, 560.0000, NULL, 0.0000, NULL, '2024-05-15', N'seed@jewelgroup.co.uk', '2024-05-22', NULL, NULL, '2024-05-29'),
    (N'ce-voq-v79', @ProjectId, N'', 79, N'VOQ-0079', N'Kitchen / utility handles', N'Kitchen / utility handles', 2, NULL, NULL, 485.0000, N'V79', 485.0000, N'SUP-IRO', '2024-05-15', N'seed@jewelgroup.co.uk', '2024-05-22', '2024-06-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v80', @ProjectId, N'', 80, N'VOQ-0080', N'First floor radiators', N'First floor radiators', 3, NULL, NULL, 4920.0000, NULL, 0.0000, NULL, '2024-05-15', N'seed@jewelgroup.co.uk', '2024-05-22', NULL, NULL, '2024-05-29'),
    (N'ce-voq-v81', @ProjectId, N'', 81, N'VOQ-0081', N'Relocate socket in bedroom 1', N'Relocate socket in bedroom 1', 2, NULL, NULL, 200.0000, N'V81', 200.0000, N'ELE-STD', '2024-06-15', N'seed@jewelgroup.co.uk', '2024-06-22', '2024-07-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v82', @ProjectId, N'', 82, N'VOQ-0082', N'780 x 980 rooflight RL03 - omit', N'780 x 980 rooflight RL03 - omit', 2, NULL, NULL, -1200.0000, N'V82', -1200.0000, N'WDR-SPG', '2024-06-15', N'seed@jewelgroup.co.uk', '2024-06-22', '2024-07-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v83', @ProjectId, N'', 83, N'VOQ-0083', N'Howdens kitchen corner units', N'Howdens kitchen corner units', 2, NULL, NULL, 1261.8000, N'V83', 1261.8000, N'SUP-KIT', '2024-07-15', N'seed@jewelgroup.co.uk', '2024-07-22', '2024-08-05', N'seed@jewelgroup.co.uk', NULL),
    (N'ce-voq-v84', @ProjectId, N'', 84, N'VOQ-0084', N'Entrance door hardware', N'Entrance door hardware', 2, NULL, NULL, 475.0000, N'V84', 475.0000, N'SUP-IRO', '2024-07-15', N'seed@jewelgroup.co.uk', '2024-07-22', '2024-08-05', N'seed@jewelgroup.co.uk', NULL)
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
    (N'ce-vo-v01', @ProjectId, 3, N'', N'', N'V01', N'Asbestos removal - Goodbye Asbestos survey & removal', 2, N'ENABLE-ASB', N'', N'item', 1.0000, -1480.0000, -1480.0000, N'', 1),
    (N'ce-vo-v02', @ProjectId, 3, N'', N'', N'V02', N'Pool side room demolition & new flat roof for hoists', 0, N'ENABLE-DEM', N'', N'item', 1.0000, 3770.0000, 3770.0000, N'', 2),
    (N'ce-vo-v03', @ProjectId, 3, N'', N'', N'V03', N'Drainage survey & report in lieu of PS', 2, N'MEC-DRN', N'', N'item', 1.0000, -4590.0000, -4590.0000, N'', 3),
    (N'ce-vo-v04', @ProjectId, 3, N'', N'', N'V04', N'Rezai Consulting invoice & trial holes', 0, N'SUB-EXC', N'', N'item', 1.0000, 2220.0000, 2220.0000, N'', 4),
    (N'ce-vo-v05', @ProjectId, 3, N'', N'', N'V05', N'Underpinning - unpin existing strip footings', 0, N'SUB-UND', N'', N'item', 1.0000, 4300.0000, 4300.0000, N'', 5),
    (N'ce-vo-v06', @ProjectId, 3, N'', N'', N'V06', N'Epsom & Ewell - Building Control fees', 0, N'HAND-SPE', N'', N'item', 1.0000, 919.0000, 919.0000, N'', 6),
    (N'ce-vo-v07', @ProjectId, 3, N'', N'', N'V07', N'Foundations redesign - excavation & concrete', 0, N'SUB-EXC', N'', N'item', 1.0000, 11780.0000, 11780.0000, N'', 7),
    (N'ce-vo-v08', @ProjectId, 3, N'', N'', N'V08', N'Trenching for the gas & electric meter moves', 0, N'UTIL-TRN', N'', N'item', 1.0000, 340.0000, 340.0000, N'', 8),
    (N'ce-vo-v09', @ProjectId, 3, N'', N'', N'V09', N'Rezai Consulting invoice - pool room works', 0, N'HAND-SPE', N'', N'item', 1.0000, 450.0000, 450.0000, N'', 9),
    (N'ce-vo-v10', @ProjectId, 3, N'', N'', N'V10', N'Structural engineers items - excavation, masonry & steels', 0, N'STR-STL', N'', N'item', 1.0000, 4825.0000, 4825.0000, N'', 10),
    (N'ce-vo-v11', @ProjectId, 3, N'', N'', N'V11', N'EOT-01 - site supervision, rubbish removal & H&S', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 17200.0000, 17200.0000, N'', 11),
    (N'ce-vo-v12', @ProjectId, 3, N'', N'', N'V12', N'Velux electric openable rooflight RL01', 2, N'WDR-SPG', N'', N'item', 1.0000, -1028.0000, -1028.0000, N'', 12),
    (N'ce-vo-v13', @ProjectId, 3, N'', N'', N'V13', N'Block & beam - bath loading capacity', 0, N'SUB-CON', N'', N'item', 1.0000, 1020.0000, 1020.0000, N'', 13),
    (N'ce-vo-v14', @ProjectId, 3, N'', N'', N'V14', N'Fire & smoke detection revision', 2, N'ELE-FIR', N'', N'item', 1.0000, -4005.0000, -4005.0000, N'', 14),
    (N'ce-vo-v15', @ProjectId, 3, N'', N'', N'V15', N'Rezai Consulting Invoice RC/678', 0, N'HAND-SPE', N'', N'item', 1.0000, 611.5200, 611.5200, N'', 15),
    (N'ce-vo-v16', @ProjectId, 3, N'', N'', N'V16', N'Alarm system - intruder & external CCTV', 2, N'ELE-ALM', N'', N'item', 1.0000, -1606.0000, -1606.0000, N'', 16),
    (N'ce-vo-v17', @ProjectId, 3, N'', N'', N'V17', N'Poolroom structural revisions & roof structure', 0, N'STR-STL', N'', N'item', 1.0000, 3795.0000, 3795.0000, N'', 17),
    (N'ce-vo-v18', @ProjectId, 3, N'', N'', N'V18', N'Garage gable end - tile hanging', 0, N'ROOF-TLN', N'', N'item', 1.0000, 3420.0000, 3420.0000, N'', 18),
    (N'ce-vo-v19', @ProjectId, 3, N'', N'', N'V19', N'CCTV - omit provisional sum', 2, N'ELE-CCT', N'', N'item', 1.0000, -5500.0000, -5500.0000, N'', 19),
    (N'ce-vo-v20', @ProjectId, 3, N'', N'', N'V20', N'Hard landscaping to the side path', 0, N'EXTW-PAV', N'', N'item', 1.0000, 5965.0000, 5965.0000, N'', 20),
    (N'ce-vo-v21', @ProjectId, 3, N'', N'', N'V21', N'Rezai Consulting fee', 0, N'HAND-SPE', N'', N'item', 1.0000, 540.0000, 540.0000, N'', 21),
    (N'ce-vo-v22', @ProjectId, 3, N'', N'', N'V22', N'Poolroom steels & timber joist layout', 0, N'CARP-1FX', N'', N'item', 1.0000, 6350.0000, 6350.0000, N'', 22),
    (N'ce-vo-v23', @ProjectId, 3, N'', N'', N'V23', N'Structural works - Dwg Issue 11', 0, N'STR-STL', N'', N'item', 1.0000, 3525.0000, 3525.0000, N'', 23),
    (N'ce-vo-v24', @ProjectId, 3, N'', N'', N'V24', N'Gate call out charge', 0, N'HAND-MSC', N'', N'item', 1.0000, 144.0000, 144.0000, N'', 24),
    (N'ce-vo-v25', @ProjectId, 3, N'', N'', N'V25', N'Chimney breast removal - roof level to ground', 0, N'ENABLE-DEM', N'', N'item', 1.0000, 5380.0000, 5380.0000, N'', 25),
    (N'ce-vo-v26', @ProjectId, 3, N'', N'', N'V26', N'Terrace decking & balustrade re-sequence', 2, N'EXTW-DEK', N'', N'item', 1.0000, -190.0000, -190.0000, N'', 26),
    (N'ce-vo-v27', @ProjectId, 3, N'', N'', N'V27', N'Parapet build up, coping stones & K-render', 0, N'MASON-BRK', N'', N'item', 1.0000, 5580.0000, 5580.0000, N'', 27),
    (N'ce-vo-v28', @ProjectId, 3, N'', N'', N'V28', N'EOT-02 - site supervision, rubbish removal & H&S', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 6880.0000, 6880.0000, N'', 28),
    (N'ce-vo-v29', @ProjectId, 3, N'', N'', N'V29', N'Taylor Dolman ceiling hoist in lieu of PS', 2, N'SPEC-LFT', N'', N'item', 1.0000, -8220.0000, -8220.0000, N'', 29),
    (N'ce-vo-v30', @ProjectId, 3, N'', N'', N'V30', N'GF solid & 30N blocks, Ancon ties to columns', 0, N'MASON-BRK', N'', N'item', 1.0000, 2963.0000, 2963.0000, N'', 30),
    (N'ce-vo-v31', @ProjectId, 3, N'', N'', N'V31', N'M&E - additional sockets, pendants & recessed lights', 0, N'ELE-STD', N'', N'item', 1.0000, 3298.0000, 3298.0000, N'', 31),
    (N'ce-vo-v34', @ProjectId, 3, N'', N'', N'V34', N'Phase 2 works - strip out, steels, ensuite & M&E', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 59037.0000, 59037.0000, N'', 32),
    (N'ce-vo-v35', @ProjectId, 3, N'', N'', N'V35', N'Poolroom foundations as per engineer drawing', 0, N'SUB-CON', N'', N'item', 1.0000, 435.0000, 435.0000, N'', 33),
    (N'ce-vo-v36', @ProjectId, 3, N'', N'', N'V36', N'Howdens kitchen & utility, quartz & installation', 0, N'SUP-KIT', N'', N'item', 1.0000, 18908.2300, 18908.2300, N'', 34),
    (N'ce-vo-v37', @ProjectId, 3, N'', N'', N'V37', N'FF - Ensuite structural works', 0, N'STR-STL', N'', N'item', 1.0000, 1770.0000, 1770.0000, N'', 35),
    (N'ce-vo-v38', @ProjectId, 3, N'', N'', N'V38', N'On the Level - wetroom', 0, N'WPF-INT', N'', N'item', 1.0000, 4357.0000, 4357.0000, N'', 36),
    (N'ce-vo-v39', @ProjectId, 3, N'', N'', N'V39', N'Loft hatch & boarding', 0, N'CARP-1FX', N'', N'item', 1.0000, 3810.0000, 3810.0000, N'', 37),
    (N'ce-vo-v40', @ProjectId, 3, N'', N'', N'V40', N'Render & paint rear extension in lieu of upstand', 2, N'EXT-STC', N'', N'item', 1.0000, -1400.0000, -1400.0000, N'', 38),
    (N'ce-vo-v41', @ProjectId, 3, N'', N'', N'V41', N'Sanitary ware supply & fit - finishing schedule Rev I', 2, N'SUP-SAN', N'', N'item', 1.0000, -287.8100, -287.8100, N'', 39),
    (N'ce-vo-v42', @ProjectId, 3, N'', N'', N'V42', N'GF studwork to the WC', 0, N'CARP-1FX', N'', N'item', 1.0000, 420.0000, 420.0000, N'', 40),
    (N'ce-vo-v43', @ProjectId, 3, N'', N'', N'V43', N'EXD01/EXD02 doors & poolroom windows revision', 0, N'WDR-ALU', N'', N'item', 1.0000, 8377.0000, 8377.0000, N'', 41),
    (N'ce-vo-v44', @ProjectId, 3, N'', N'', N'V44', N'Soundbloc plasterboard to hoist areas', 0, N'INT-PLB', N'', N'item', 1.0000, 954.8000, 954.8000, N'', 42),
    (N'ce-vo-v48', @ProjectId, 3, N'', N'', N'V48', N'Supply, install & decorate staircase in lieu of PS', 0, N'STAIR-TIM', N'', N'item', 1.0000, 5325.0000, 5325.0000, N'', 43),
    (N'ce-vo-v49', @ProjectId, 3, N'', N'', N'V49', N'Tile supply per schedule Rev I, adhesive & grout', 0, N'SUP-TIL', N'', N'item', 1.0000, 2439.1200, 2439.1200, N'', 44),
    (N'ce-vo-v50', @ProjectId, 3, N'', N'', N'V50', N'Wall tiling install, mosaics & GF WC finishes', 0, N'TIL-STD', N'', N'item', 1.0000, 11950.0000, 11950.0000, N'', 45),
    (N'ce-vo-v51', @ProjectId, 3, N'', N'', N'V51', N'Electrics & heating per revised drawings', 0, N'MEC-PLM', N'', N'item', 1.0000, 12415.0000, 12415.0000, N'', 46),
    (N'ce-vo-v53', @ProjectId, 3, N'', N'', N'V53', N'Bedroom 4 window opening', 0, N'MASON-BRK', N'', N'item', 1.0000, 225.0000, 225.0000, N'', 47),
    (N'ce-vo-v54', @ProjectId, 3, N'', N'', N'V54', N'EOT-04 - site manager, H&S & rubbish removal', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 6880.0000, 6880.0000, N'', 48),
    (N'ce-vo-v55', @ProjectId, 3, N'', N'', N'V55', N'Karndean herringbone flooring in lieu of contract flooring', 0, N'FLR-LVT', N'', N'item', 1.0000, 11240.0000, 11240.0000, N'', 49),
    (N'ce-vo-v56', @ProjectId, 3, N'', N'', N'V56', N'Blinds & curtains - omit provisional sum', 2, N'WIN-BLD', N'', N'item', 1.0000, -17500.0000, -17500.0000, N'', 50),
    (N'ce-vo-v57', @ProjectId, 3, N'', N'', N'V57', N'External works omits - decking, balustrade & landscaping', 2, N'EXTW-DEK', N'', N'item', 1.0000, -24995.0000, -24995.0000, N'', 51),
    (N'ce-vo-v58', @ProjectId, 3, N'', N'', N'V58', N'Contingency Budget - omit', 2, N'HAND-MSC', N'', N'item', 1.0000, -50000.0000, -50000.0000, N'', 52),
    (N'ce-vo-v59', @ProjectId, 3, N'', N'', N'V59', N'Decoration uplifts - black doors & glitter paint', 0, N'DEC-STD', N'', N'item', 1.0000, 1230.0000, 1230.0000, N'', 53),
    (N'ce-vo-v60', @ProjectId, 3, N'', N'', N'V60', N'Render & paint plinth in lieu of damp / roof cleaning PS', 2, N'WPF-DMP', N'', N'item', 1.0000, -1485.0000, -1485.0000, N'', 54),
    (N'ce-vo-v62', @ProjectId, 3, N'', N'', N'V62', N'Generator hire - 4 weeks', 0, N'ELE-STD', N'', N'item', 1.0000, 2000.0000, 2000.0000, N'', 55),
    (N'ce-vo-v64', @ProjectId, 3, N'', N'', N'V64', N'1100 mm glass balustrade to terrace - reinstated', 0, N'STR-GRL', N'', N'item', 1.0000, 3660.0000, 3660.0000, N'', 56),
    (N'ce-vo-v65', @ProjectId, 3, N'', N'', N'V65', N'Baywater Victrion vanity unit & restock charge', 2, N'SUP-SAN', N'', N'item', 1.0000, -35.0000, -35.0000, N'', 57),
    (N'ce-vo-v66', @ProjectId, 3, N'', N'', N'V66', N'Pocket door - Ensuite D25', 0, N'CARP-DOR', N'', N'item', 1.0000, 325.0000, 325.0000, N'', 58),
    (N'ce-vo-v67', @ProjectId, 3, N'', N'', N'V67', N'Ironmongery per finishes schedule in lieu of PS', 2, N'SUP-IRO', N'', N'item', 1.0000, -820.0000, -820.0000, N'', 59),
    (N'ce-vo-v68', @ProjectId, 3, N'', N'', N'V68', N'Kitchen appliances - Fohen tap, Bosch & Samsung', 0, N'SUP-APP', N'', N'item', 1.0000, 5600.0000, 5600.0000, N'', 60),
    (N'ce-vo-v69', @ProjectId, 3, N'', N'', N'V69', N'Quartz additional worktop', 0, N'SUP-KIT', N'', N'item', 1.0000, 555.0000, 555.0000, N'', 61),
    (N'ce-vo-v71', @ProjectId, 3, N'', N'', N'V71', N'Electric cabling for AC provision in lieu of PS', 2, N'MEC-AC', N'', N'item', 1.0000, -2450.0000, -2450.0000, N'', 62),
    (N'ce-vo-v73', @ProjectId, 3, N'', N'', N'V73', N'Main bathroom - electric UFH & client supplied tiles', 0, N'TIL-STD', N'', N'item', 1.0000, 1430.0000, 1430.0000, N'', 63),
    (N'ce-vo-v74', @ProjectId, 3, N'', N'', N'V74', N'Ensuite wall & floor tile omits', 2, N'SUP-TIL', N'', N'item', 1.0000, -1431.5000, -1431.5000, N'', 64),
    (N'ce-vo-v75', @ProjectId, 3, N'', N'', N'V75', N'Wardrobes, shelving, storage - omit provisional sum', 2, N'CARP-WRD', N'', N'item', 1.0000, -20000.0000, -20000.0000, N'', 65),
    (N'ce-vo-v79', @ProjectId, 3, N'', N'', N'V79', N'Kitchen / utility handles', 0, N'SUP-IRO', N'', N'item', 1.0000, 485.0000, 485.0000, N'', 66),
    (N'ce-vo-v81', @ProjectId, 3, N'', N'', N'V81', N'Relocate socket in bedroom 1', 0, N'ELE-STD', N'', N'item', 1.0000, 200.0000, 200.0000, N'', 67),
    (N'ce-vo-v82', @ProjectId, 3, N'', N'', N'V82', N'780 x 980 rooflight RL03 - omit', 2, N'WDR-SPG', N'', N'item', 1.0000, -1200.0000, -1200.0000, N'', 68),
    (N'ce-vo-v83', @ProjectId, 3, N'', N'', N'V83', N'Howdens kitchen corner units', 0, N'SUP-KIT', N'', N'item', 1.0000, 1261.8000, 1261.8000, N'', 69),
    (N'ce-vo-v84', @ProjectId, 3, N'', N'', N'V84', N'Entrance door hardware', 0, N'SUP-IRO', N'', N'item', 1.0000, 475.0000, 475.0000, N'', 70)
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

    PRINT 'Cornerways East: variation orders & variation lines merged.';
    COMMIT TRAN;

    -- Sanity check: variation lines should reconcile to the workbook register.
    SELECT
        (SELECT COUNT(*) FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = @ProjectId) AS VariationOrders,                        -- 84
        (SELECT COUNT(*) FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = @ProjectId AND Status = 2) AS ApprovedVariationOrders, -- 70
        (SELECT COUNT(*) FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = @ProjectId AND Status = 3) AS DeclinedVariationOrders, -- 14
        (SELECT COUNT(*) FROM [dbo].[ValuationLineItems]   WHERE ProjectId = @ProjectId AND ElementType = 3) AS VariationLines,     -- 70
        (SELECT SUM(LineAmount) FROM [dbo].[ValuationLineItems]
          WHERE ProjectId = @ProjectId AND ElementType = 3 AND LineType NOT IN (3, 4)) AS NetVariations;                            -- 112817.16

    -- Combined check: original Contract Sum + Net Variations = Revised Contract Sum.
    SELECT
        SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,    -- 641884.00
        SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations,  -- 112817.16
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                         -- 754701.16
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId;
END
GO
