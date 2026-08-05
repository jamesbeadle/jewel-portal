-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per
-- JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: 24 Sherwood Park -- Variation Orders (Valuation Report variation lines)
-- ----------------------------------------------------------------------------
-- Project : 24 Sherwood Park Road, Sutton SM1 2SQ
-- ProjectId: resolved at run time by site-name matcher '24sherwoodparksm12sq'
--
-- Companion to seed-sherwoodpark-valuation.sql, which seeds ONLY the original
-- contract scope (Contract works + PS = Contract Sum GBP 563,138.00). This
-- file adds the post-contract VARIATION ORDERS from the "Valuation 17"
-- workbook, reconciling to the register:
--
--     Contract Sum            GBP 563,138.00
--     Net Variations          GBP  97,518.74
--     ----------------------------------------
--     Revised (Live Build)    GBP 660,656.74
--
-- MODEL NOTE (unified variation orders, post-20260723 UnifyVariationOrders)
-- Each workbook VO is split into multiple priced rows (omits of contract/PS
-- scope as negatives, new items as positives). On the JPMS valuation report a
-- VO shows as a SINGLE summary line, so we seed ONE ValuationLineItem per
-- approved VO whose LineAmount is the NET of that VO's workbook rows
-- (Quantity 1 x Rate = net), plus ONE row per VO in VariationOrderQuotes
-- (the unified variation order record; there is no separate VariationOrders
-- table any more).
--
-- DECLINED / TBC VOs (no valuation line, VOQ Status 3 = Rejected, Value 0,
-- excluded from the register net):
--   * V07 -- "Missing from the Tender" groundworks items, TBC and never
--            valued (est. from qty x rate: 7,465.00)
--   * V17 -- Karndean Dutch Limed Oak swap, marked "Declinded", no values
--            (EstimatedValue NULL)
--   * V64 -- Kitchen sink & unit, quoted 825.00
--   * V78 -- Handrail remove & replace, quoted 1,075.00
--   * V81 -- Aco drains, quoted 5,667.50
--
-- Approved VOs sum to the stated Net Variations of GBP 97,518.74 EXACTLY (the
-- register's per-claim addition shows 97,518.79 -- a 0.05 casting slip inside
-- V15's claim columns; the amount column, used here, is authoritative).
-- Approval dates are derived from each VO's first claimed valuation month
-- (Dec-22 .. Jan-24); declined VOs are dated alongside their neighbours.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation (all rows = 3)
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--   * net > 0 -> Priced (addition);  net < 0 -> Omit (negative LineAmount)
-- VOQ Status : 2=Approved 3=Rejected
--
-- Idempotent: keyed on stable ids (sp-voq-vNN / sp-vo-vNN) via MERGE; rows of
-- other projects are never touched (no BY SOURCE clause). Safe to run
-- repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '24sherwoodparksm12sq'
       OR LOWER(REPLACE(Name, ' ', '')) = '24sherwoodparksm12sq'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '24sherwoodparksm12sq' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  24 Sherwood Park -- no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[VariationOrderQuotes] AS target
    USING (VALUES
        (N'sp-voq-v01', @ProjectId, N'', 1, N'VOQ-0001', N'Omit loft staircase & first floor ceiling opening', N'Omit loft staircase & first floor ceiling opening', 2, NULL, NULL, -5000.0000, N'V01', -5000.0000, N'STAIR-TIM', '2022-11-24', N'seed@jewelgroup.co.uk', '2022-12-01', '2022-12-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v02', @ProjectId, N'', 2, N'VOQ-0002', N'Asbestos removal - Goodbye Asbestos survey & removal vs PS', N'Asbestos removal - Goodbye Asbestos survey & removal vs PS', 2, NULL, NULL, -2144.0000, N'V02', -2144.0000, N'ENABLE-ASB', '2022-11-24', N'seed@jewelgroup.co.uk', '2022-12-01', '2022-12-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v03', @ProjectId, N'', 3, N'VOQ-0003', N'Strip & retile entire main roof vs works-to-roof PS', N'Strip & retile entire main roof vs works-to-roof PS', 2, NULL, NULL, 18100.0000, N'V03', 18100.0000, N'ROOF-TLO', '2022-11-24', N'seed@jewelgroup.co.uk', '2022-12-01', '2022-12-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v04', @ProjectId, N'', 4, N'VOQ-0004', N'Drainage survey & remedial works vs PS', N'Drainage survey & remedial works vs PS', 2, NULL, NULL, -3700.0000, N'V04', -3700.0000, N'MEC-DRN', '2022-12-24', N'seed@jewelgroup.co.uk', '2023-01-01', '2023-01-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v05', @ProjectId, N'', 5, N'VOQ-0005', N'Structural works & propping vs PS', N'Structural works & propping vs PS', 2, NULL, NULL, -2760.0000, N'V05', -2760.0000, N'STR-STL', '2022-12-24', N'seed@jewelgroup.co.uk', '2023-01-01', '2023-01-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v06', @ProjectId, N'', 6, N'VOQ-0006', N'Building Control fees - paid direct by Jewel Bespoke', N'Building Control fees - paid direct by Jewel Bespoke', 2, NULL, NULL, 1200.0000, N'V06', 1200.0000, N'HAND-MSC', '2022-12-24', N'seed@jewelgroup.co.uk', '2023-01-01', '2023-01-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v07', @ProjectId, N'', 7, N'VOQ-0007', N'Missing tender items - reduced level dig, foundations & concrete (TBC)', N'Missing tender items - reduced level dig, foundations & concrete (TBC)', 3, NULL, NULL, 7465.0000, NULL, 0.0000, NULL, '2022-12-24', N'seed@jewelgroup.co.uk', '2023-01-01', NULL, NULL, '2023-01-08'),
        (N'sp-voq-v08', @ProjectId, N'', 8, N'VOQ-0008', N'Excavate & concrete strip footings, pad foundations & padstones', N'Excavate & concrete strip footings, pad foundations & padstones', 2, NULL, NULL, 5096.0000, N'V08', 5096.0000, N'SUB-EXC', '2023-01-24', N'seed@jewelgroup.co.uk', '2023-02-01', '2023-02-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v09', @ProjectId, N'', 9, N'VOQ-0009', N'Structural steels & associated works vs PS', N'Structural steels & associated works vs PS', 2, NULL, NULL, 5200.0000, N'V09', 5200.0000, N'STR-STL', '2023-02-24', N'seed@jewelgroup.co.uk', '2023-03-01', '2023-03-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v10', @ProjectId, N'', 10, N'VOQ-0010', N'DPC/DPM detail to drawing WD-P-016 vs contract DPC', N'DPC/DPM detail to drawing WD-P-016 vs contract DPC', 2, NULL, NULL, 1812.0000, N'V10', 1812.0000, N'WPF-DMP', '2023-02-24', N'seed@jewelgroup.co.uk', '2023-03-01', '2023-03-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v11', @ProjectId, N'', 11, N'VOQ-0011', N'Party Wall invoice', N'Party Wall invoice', 2, NULL, NULL, 960.0000, N'V11', 960.0000, N'HAND-SPE', '2023-02-24', N'seed@jewelgroup.co.uk', '2023-03-01', '2023-03-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v12', @ProjectId, N'', 12, N'VOQ-0012', N'Structural Engineers - INV-1743', N'Structural Engineers - INV-1743', 2, NULL, NULL, 540.0000, N'V12', 540.0000, N'HAND-SPE', '2023-03-24', N'seed@jewelgroup.co.uk', '2023-04-01', '2023-04-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v13', @ProjectId, N'', 13, N'VOQ-0013', N'Supply & install loft hatch & pull-down ladder', N'Supply & install loft hatch & pull-down ladder', 2, NULL, NULL, 950.0000, N'V13', 950.0000, N'CARP-2FX', '2023-03-24', N'seed@jewelgroup.co.uk', '2023-04-01', '2023-04-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v14', @ProjectId, N'', 14, N'VOQ-0014', N'Supply & install T&G ground floor', N'Supply & install T&G ground floor', 2, NULL, NULL, 3450.0000, N'V14', 3450.0000, N'CARP-1FX', '2023-04-24', N'seed@jewelgroup.co.uk', '2023-05-01', '2023-05-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v15', @ProjectId, N'', 15, N'VOQ-0015', N'Velux curved glass & electric rooflights vs contract rooflights', N'Velux curved glass & electric rooflights vs contract rooflights', 2, NULL, NULL, 699.2300, N'V15', 699.2300, N'WDR-SPG', '2023-03-24', N'seed@jewelgroup.co.uk', '2023-04-01', '2023-04-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v16', @ProjectId, N'', 16, N'VOQ-0016', N'Sanitaryware supply - Taylor Dolman & schedule vs PC6', N'Sanitaryware supply - Taylor Dolman & schedule vs PC6', 2, NULL, NULL, 6840.0000, N'V16', 6840.0000, N'SUP-SAN', '2023-05-24', N'seed@jewelgroup.co.uk', '2023-06-01', '2023-06-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v17', @ProjectId, N'', 17, N'VOQ-0017', N'Karndean Dutch Limed Oak flooring swap', N'Karndean Dutch Limed Oak flooring swap', 3, NULL, NULL, NULL, NULL, 0.0000, NULL, '2023-05-24', N'seed@jewelgroup.co.uk', '2023-06-01', NULL, NULL, '2023-06-08'),
        (N'sp-voq-v18', @ProjectId, N'', 18, N'VOQ-0018', N'UPVC bay windows vs kitchenette window', N'UPVC bay windows vs kitchenette window', 2, NULL, NULL, 8275.0000, N'V18', 8275.0000, N'WDR-UPV', '2023-04-24', N'seed@jewelgroup.co.uk', '2023-05-01', '2023-05-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v19', @ProjectId, N'', 19, N'VOQ-0019', N'Howdens kitchen & utility - supply, worktop & installation vs PS', N'Howdens kitchen & utility - supply, worktop & installation vs PS', 2, NULL, NULL, 8425.0000, N'V19', 8425.0000, N'SUP-KIT', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v20', @ProjectId, N'', 20, N'VOQ-0020', N'Aluminium sliding doors to kitchen vs upvc French doors', N'Aluminium sliding doors to kitchen vs upvc French doors', 2, NULL, NULL, 703.0000, N'V20', 703.0000, N'WDR-ALU', '2023-04-24', N'seed@jewelgroup.co.uk', '2023-05-01', '2023-05-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v21', @ProjectId, N'', 21, N'VOQ-0021', N'Fire & smoke detection vs PS', N'Fire & smoke detection vs PS', 2, NULL, NULL, -1865.0000, N'V21', -1865.0000, N'ELE-FIR', '2023-05-24', N'seed@jewelgroup.co.uk', '2023-06-01', '2023-06-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v22', @ProjectId, N'', 22, N'VOQ-0022', N'Alarm system - Ring doorbell vs PS', N'Alarm system - Ring doorbell vs PS', 2, NULL, NULL, -860.0000, N'V22', -860.0000, N'ELE-ALM', '2023-05-24', N'seed@jewelgroup.co.uk', '2023-06-01', '2023-06-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v23', @ProjectId, N'', 23, N'VOQ-0023', N'Outhouse floor - strip, T&G & Karndean', N'Outhouse floor - strip, T&G & Karndean', 2, NULL, NULL, 4175.0000, N'V23', 4175.0000, N'FLR-LVT', '2023-06-24', N'seed@jewelgroup.co.uk', '2023-07-01', '2023-07-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v24', @ProjectId, N'', 24, N'VOQ-0024', N'6ft close board fencing - extended length', N'6ft close board fencing - extended length', 2, NULL, NULL, 4620.0000, N'V24', 4620.0000, N'EXTW-FEN', '2023-05-24', N'seed@jewelgroup.co.uk', '2023-06-01', '2023-06-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v25', @ProjectId, N'', 25, N'VOQ-0025', N'Omit hot tub provisional sum', N'Omit hot tub provisional sum', 2, NULL, NULL, -12500.0000, N'V25', -12500.0000, N'SPEC-SPA', '2023-05-24', N'seed@jewelgroup.co.uk', '2023-06-01', '2023-06-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v26', @ProjectId, N'', 26, N'VOQ-0026', N'Omit garden gazebo provisional sum', N'Omit garden gazebo provisional sum', 2, NULL, NULL, -7500.0000, N'V26', -7500.0000, N'SPEC-GAZ', '2023-05-24', N'seed@jewelgroup.co.uk', '2023-06-01', '2023-06-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v27', @ProjectId, N'', 27, N'VOQ-0027', N'Composite entrance doors with side lights - revised supply', N'Composite entrance doors with side lights - revised supply', 2, NULL, NULL, 4050.0000, N'V27', 4050.0000, N'WDR-TIM', '2023-05-24', N'seed@jewelgroup.co.uk', '2023-06-01', '2023-06-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v28', @ProjectId, N'', 28, N'VOQ-0028', N'External works redesign - retaining wall, edging, topsoil vs planters/balustrade/landscaping', N'External works redesign - retaining wall, edging, topsoil vs planters/balustrade/landscaping', 2, NULL, NULL, -850.0000, N'V28', -850.0000, N'EXTW-LND', '2023-06-24', N'seed@jewelgroup.co.uk', '2023-07-01', '2023-07-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v29', @ProjectId, N'', 29, N'VOQ-0029', N'Data / TV points & relocation of bedroom wiring', N'Data / TV points & relocation of bedroom wiring', 2, NULL, NULL, 2480.0000, N'V29', 2480.0000, N'ELE-STD', '2023-06-24', N'seed@jewelgroup.co.uk', '2023-07-01', '2023-07-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v30', @ProjectId, N'', 30, N'VOQ-0030', N'HSS generator hire - week 1', N'HSS generator hire - week 1', 2, NULL, NULL, 500.0000, N'V30', 500.0000, N'PRELIMS-TMP', '2023-06-24', N'seed@jewelgroup.co.uk', '2023-07-01', '2023-07-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v31', @ProjectId, N'', 31, N'VOQ-0031', N'Omit PV solar panels', N'Omit PV solar panels', 2, NULL, NULL, -3500.0000, N'V31', -3500.0000, N'MEC-SOL', '2023-08-24', N'seed@jewelgroup.co.uk', '2023-09-01', '2023-09-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v32', @ProjectId, N'', 32, N'VOQ-0032', N'Omit kitchenette to out building', N'Omit kitchenette to out building', 2, NULL, NULL, -2000.0000, N'V32', -2000.0000, N'CARP-KIT', '2023-06-24', N'seed@jewelgroup.co.uk', '2023-07-01', '2023-07-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v33', @ProjectId, N'', 33, N'VOQ-0033', N'HSS generator hire - week 2', N'HSS generator hire - week 2', 2, NULL, NULL, 500.0000, N'V33', 500.0000, N'PRELIMS-TMP', '2023-06-24', N'seed@jewelgroup.co.uk', '2023-07-01', '2023-07-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v34', @ProjectId, N'', 34, N'VOQ-0034', N'Staircase & balustrade redesign incl. decoration', N'Staircase & balustrade redesign incl. decoration', 2, NULL, NULL, 1155.0000, N'V34', 1155.0000, N'STAIR-TIM', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v35', @ProjectId, N'', 35, N'VOQ-0035', N'HSS generator hire - weeks 3 & 4', N'HSS generator hire - weeks 3 & 4', 2, NULL, NULL, 1000.0000, N'V35', 1000.0000, N'PRELIMS-TMP', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v36', @ProjectId, N'', 36, N'VOQ-0036', N'Wall tiling supply per schedule vs PS', N'Wall tiling supply per schedule vs PS', 2, NULL, NULL, 565.0000, N'V36', 565.0000, N'SUP-TIL', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v37', @ProjectId, N'', 37, N'VOQ-0037', N'Karndean to first floor vs underlay & carpet', N'Karndean to first floor vs underlay & carpet', 2, NULL, NULL, 2170.0000, N'V37', 2170.0000, N'FLR-LVT', '2023-08-24', N'seed@jewelgroup.co.uk', '2023-09-01', '2023-09-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v38', @ProjectId, N'', 38, N'VOQ-0038', N'Omit wardrobes & storage provisional sum', N'Omit wardrobes & storage provisional sum', 2, NULL, NULL, -8000.0000, N'V38', -8000.0000, N'CARP-WRD', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v39', @ProjectId, N'', 39, N'VOQ-0039', N'HSS generator hire - week 5', N'HSS generator hire - week 5', 2, NULL, NULL, 500.0000, N'V39', 500.0000, N'PRELIMS-TMP', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v40', @ProjectId, N'', 40, N'VOQ-0040', N'Electrical amendments - Bedroom 2', N'Electrical amendments - Bedroom 2', 2, NULL, NULL, 280.0000, N'V40', 280.0000, N'ELE-STD', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v41', @ProjectId, N'', 41, N'VOQ-0041', N'Underlay & carpet - staircase runner', N'Underlay & carpet - staircase runner', 2, NULL, NULL, 885.0000, N'V41', 885.0000, N'FLR-CPT', '2023-10-24', N'seed@jewelgroup.co.uk', '2023-11-01', '2023-11-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v42', @ProjectId, N'', 42, N'VOQ-0042', N'Kitchen amendments credit', N'Kitchen amendments credit', 2, NULL, NULL, -284.8200, N'V42', -284.8200, N'SUP-KIT', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v43', @ProjectId, N'', 43, N'VOQ-0043', N'Internal door ironmongery vs PS', N'Internal door ironmongery vs PS', 2, NULL, NULL, -1316.0000, N'V43', -1316.0000, N'SUP-IRO', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v44', @ProjectId, N'', 44, N'VOQ-0044', N'Landscape works - IN610-008', N'Landscape works - IN610-008', 2, NULL, NULL, 7820.0000, N'V44', 7820.0000, N'EXTW-LND', '2023-08-24', N'seed@jewelgroup.co.uk', '2023-09-01', '2023-09-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v45', @ProjectId, N'', 45, N'VOQ-0045', N'EOT-01 prelims - site supervision, H&S & temp toilet', N'EOT-01 prelims - site supervision, H&S & temp toilet', 2, NULL, NULL, 4825.0000, N'V45', 4825.0000, N'PRELIMS-SMG', '2023-09-24', N'seed@jewelgroup.co.uk', '2023-10-01', '2023-10-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v46', @ProjectId, N'', 46, N'VOQ-0046', N'HSS generator hire - week 6', N'HSS generator hire - week 6', 2, NULL, NULL, 500.0000, N'V46', 500.0000, N'PRELIMS-TMP', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v47', @ProjectId, N'', 47, N'VOQ-0047', N'HSS generator hire - week 7', N'HSS generator hire - week 7', 2, NULL, NULL, 500.0000, N'V47', 500.0000, N'PRELIMS-TMP', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v48', @ProjectId, N'', 48, N'VOQ-0048', N'HSS generator hire - week 8', N'HSS generator hire - week 8', 2, NULL, NULL, 500.0000, N'V48', 500.0000, N'PRELIMS-TMP', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v49', @ProjectId, N'', 49, N'VOQ-0049', N'Supply & install outside tap to front', N'Supply & install outside tap to front', 2, NULL, NULL, 120.0000, N'V49', 120.0000, N'MEC-PLM', '2023-08-24', N'seed@jewelgroup.co.uk', '2023-09-01', '2023-09-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v50', @ProjectId, N'', 50, N'VOQ-0050', N'M&E additions - spot lights, sockets & making good', N'M&E additions - spot lights, sockets & making good', 2, NULL, NULL, 3241.0000, N'V50', 3241.0000, N'ELE-STD', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v51', @ProjectId, N'', 51, N'VOQ-0051', N'HSS generator hire - week 9', N'HSS generator hire - week 9', 2, NULL, NULL, 500.0000, N'V51', 500.0000, N'PRELIMS-TMP', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v52', @ProjectId, N'', 52, N'VOQ-0052', N'Electrics - Tonys rooms shaver socket & making good', N'Electrics - Tonys rooms shaver socket & making good', 2, NULL, NULL, 310.0000, N'V52', 310.0000, N'ELE-STD', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v53', @ProjectId, N'', 53, N'VOQ-0053', N'Sheds, concrete base, Marshalls paving & topsoil', N'Sheds, concrete base, Marshalls paving & topsoil', 2, NULL, NULL, 6840.0000, N'V53', 6840.0000, N'EXTW-LND', '2023-08-24', N'seed@jewelgroup.co.uk', '2023-09-01', '2023-09-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v54', @ProjectId, N'', 54, N'VOQ-0054', N'Mailbox - supply & install', N'Mailbox - supply & install', 2, NULL, NULL, 80.0000, N'V54', 80.0000, N'HAND-MSC', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v55', @ProjectId, N'', 55, N'VOQ-0055', N'HSS generator hire - week 10', N'HSS generator hire - week 10', 2, NULL, NULL, 500.0000, N'V55', 500.0000, N'PRELIMS-TMP', '2023-07-24', N'seed@jewelgroup.co.uk', '2023-08-01', '2023-08-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v56', @ProjectId, N'', 56, N'VOQ-0056', N'Blinds per schedule vs blinds & curtains PS', N'Blinds per schedule vs blinds & curtains PS', 2, NULL, NULL, -627.6500, N'V56', -627.6500, N'WIN-BLD', '2023-08-24', N'seed@jewelgroup.co.uk', '2023-09-01', '2023-09-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v57', @ProjectId, N'', 57, N'VOQ-0057', N'HSS generator hire - week 11', N'HSS generator hire - week 11', 2, NULL, NULL, 500.0000, N'V57', 500.0000, N'PRELIMS-TMP', '2023-08-24', N'seed@jewelgroup.co.uk', '2023-09-01', '2023-09-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v58', @ProjectId, N'', 58, N'VOQ-0058', N'External painting', N'External painting', 2, NULL, NULL, 2800.0000, N'V58', 2800.0000, N'DEC-STD', '2023-08-24', N'seed@jewelgroup.co.uk', '2023-09-01', '2023-09-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v59', @ProjectId, N'', 59, N'VOQ-0059', N'HSS generator hire - week 12', N'HSS generator hire - week 12', 2, NULL, NULL, 500.0000, N'V59', 500.0000, N'PRELIMS-TMP', '2023-08-24', N'seed@jewelgroup.co.uk', '2023-09-01', '2023-09-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v60', @ProjectId, N'', 60, N'VOQ-0060', N'Wardrobes - Ikea units, TV unit & storeroom joinery vs PS', N'Wardrobes - Ikea units, TV unit & storeroom joinery vs PS', 2, NULL, NULL, 3154.9600, N'V60', 3154.9600, N'CARP-WRD', '2023-08-24', N'seed@jewelgroup.co.uk', '2023-09-01', '2023-09-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v61', @ProjectId, N'', 61, N'VOQ-0061', N'Side entrance gate', N'Side entrance gate', 2, NULL, NULL, 295.0000, N'V61', 295.0000, N'EXTW-FEN', '2023-09-24', N'seed@jewelgroup.co.uk', '2023-10-01', '2023-10-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v62', @ProjectId, N'', 62, N'VOQ-0062', N'WC vanity unit', N'WC vanity unit', 2, NULL, NULL, 595.0000, N'V62', 595.0000, N'SUP-SAN', '2023-09-24', N'seed@jewelgroup.co.uk', '2023-10-01', '2023-10-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v63', @ProjectId, N'', 63, N'VOQ-0063', N'Building Control payment - re-roof', N'Building Control payment - re-roof', 2, NULL, NULL, 396.0000, N'V63', 396.0000, N'HAND-MSC', '2023-09-24', N'seed@jewelgroup.co.uk', '2023-10-01', '2023-10-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v64', @ProjectId, N'', 64, N'VOQ-0064', N'Kitchen sink & unit', N'Kitchen sink & unit', 3, NULL, NULL, 825.0000, NULL, 0.0000, NULL, '2023-09-24', N'seed@jewelgroup.co.uk', '2023-10-01', NULL, NULL, '2023-10-08'),
        (N'sp-voq-v65', @ProjectId, N'', 65, N'VOQ-0065', N'Side path paving', N'Side path paving', 2, NULL, NULL, 275.0000, N'V65', 275.0000, N'EXTW-PAV', '2023-09-24', N'seed@jewelgroup.co.uk', '2023-10-01', '2023-10-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v66', @ProjectId, N'', 66, N'VOQ-0066', N'Staircase spindles', N'Staircase spindles', 2, NULL, NULL, 460.0000, N'V66', 460.0000, N'STAIR-TIM', '2023-09-24', N'seed@jewelgroup.co.uk', '2023-10-01', '2023-10-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v67', @ProjectId, N'', 67, N'VOQ-0067', N'Lamona extractor fan', N'Lamona extractor fan', 2, NULL, NULL, 477.0200, N'V67', 477.0200, N'SUP-APP', '2023-10-24', N'seed@jewelgroup.co.uk', '2023-11-01', '2023-11-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v68', @ProjectId, N'', 68, N'VOQ-0068', N'Electrical & painting amendments', N'Electrical & painting amendments', 2, NULL, NULL, 1230.0000, N'V68', 1230.0000, N'ELE-STD', '2023-10-24', N'seed@jewelgroup.co.uk', '2023-11-01', '2023-11-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v69', @ProjectId, N'', 69, N'VOQ-0069', N'External paving & pillar', N'External paving & pillar', 2, NULL, NULL, 475.0000, N'V69', 475.0000, N'EXTW-PAV', '2023-10-24', N'seed@jewelgroup.co.uk', '2023-11-01', '2023-11-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v70', @ProjectId, N'', 70, N'VOQ-0070', N'Supply & install door stops', N'Supply & install door stops', 2, NULL, NULL, 450.0000, N'V70', 450.0000, N'SUP-IRO', '2023-10-24', N'seed@jewelgroup.co.uk', '2023-11-01', '2023-11-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v71', @ProjectId, N'', 71, N'VOQ-0071', N'Supply of dining room lights', N'Supply of dining room lights', 2, NULL, NULL, 145.0000, N'V71', 145.0000, N'ELE-STD', '2023-10-24', N'seed@jewelgroup.co.uk', '2023-11-01', '2023-11-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v72', @ProjectId, N'', 72, N'VOQ-0072', N'Howdens sink, worktop & units', N'Howdens sink, worktop & units', 2, NULL, NULL, 10012.0000, N'V72', 10012.0000, N'SUP-KIT', '2023-10-24', N'seed@jewelgroup.co.uk', '2023-11-01', '2023-11-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v73', @ProjectId, N'', 73, N'VOQ-0073', N'Kitchen window, cill & external works', N'Kitchen window, cill & external works', 2, NULL, NULL, 2910.0000, N'V73', 2910.0000, N'WDR-UPV', '2023-10-24', N'seed@jewelgroup.co.uk', '2023-11-01', '2023-11-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v74', @ProjectId, N'', 74, N'VOQ-0074', N'Extension of prelims EOT-02 x 6 weeks', N'Extension of prelims EOT-02 x 6 weeks', 2, NULL, NULL, 5790.0000, N'V74', 5790.0000, N'PRELIMS-SMG', '2023-10-24', N'seed@jewelgroup.co.uk', '2023-11-01', '2023-11-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v75', @ProjectId, N'', 75, N'VOQ-0075', N'Easi Hold resin - French drains', N'Easi Hold resin - French drains', 2, NULL, NULL, 240.0000, N'V75', 240.0000, N'SUB-DRN', '2023-12-24', N'seed@jewelgroup.co.uk', '2024-01-01', '2024-01-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v76', @ProjectId, N'', 76, N'VOQ-0076', N'Towel radiator - main bathroom', N'Towel radiator - main bathroom', 2, NULL, NULL, 395.0000, N'V76', 395.0000, N'MEC-PLM', '2023-10-24', N'seed@jewelgroup.co.uk', '2023-11-01', '2023-11-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v77', @ProjectId, N'', 77, N'VOQ-0077', N'Shower hose', N'Shower hose', 2, NULL, NULL, 120.0000, N'V77', 120.0000, N'SUP-SAN', '2023-11-24', N'seed@jewelgroup.co.uk', '2023-12-01', '2023-12-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v78', @ProjectId, N'', 78, N'VOQ-0078', N'Handrail - remove & replace', N'Handrail - remove & replace', 3, NULL, NULL, 1075.0000, NULL, 0.0000, NULL, '2023-11-24', N'seed@jewelgroup.co.uk', '2023-12-01', NULL, NULL, '2023-12-08'),
        (N'sp-voq-v79', @ProjectId, N'', 79, N'VOQ-0079', N'Towel radiator relocation - main bathroom', N'Towel radiator relocation - main bathroom', 2, NULL, NULL, 880.0000, N'V79', 880.0000, N'MEC-PLM', '2023-12-24', N'seed@jewelgroup.co.uk', '2024-01-01', '2024-01-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v80', @ProjectId, N'', 80, N'VOQ-0080', N'Remove existing mirrors & replace', N'Remove existing mirrors & replace', 2, NULL, NULL, 200.0000, N'V80', 200.0000, N'HAND-MSC', '2023-12-24', N'seed@jewelgroup.co.uk', '2024-01-01', '2024-01-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v81', @ProjectId, N'', 81, N'VOQ-0081', N'Aco drains', N'Aco drains', 3, NULL, NULL, 5667.5000, NULL, 0.0000, NULL, '2023-12-24', N'seed@jewelgroup.co.uk', '2024-01-01', NULL, NULL, '2024-01-08'),
        (N'sp-voq-v82', @ProjectId, N'', 82, N'VOQ-0082', N'Extension of prelims EOT-03 x 3 weeks', N'Extension of prelims EOT-03 x 3 weeks', 2, NULL, NULL, 2895.0000, N'V82', 2895.0000, N'PRELIMS-SMG', '2023-12-24', N'seed@jewelgroup.co.uk', '2024-01-01', '2024-01-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v83', @ProjectId, N'', 83, N'VOQ-0083', N'Outhouse boxing', N'Outhouse boxing', 2, NULL, NULL, 1340.0000, N'V83', 1340.0000, N'CARP-2FX', '2023-12-24', N'seed@jewelgroup.co.uk', '2024-01-01', '2024-01-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v84', @ProjectId, N'', 84, N'VOQ-0084', N'Electrical supply to mirrors', N'Electrical supply to mirrors', 2, NULL, NULL, 610.0000, N'V84', 610.0000, N'ELE-STD', '2023-12-24', N'seed@jewelgroup.co.uk', '2024-01-01', '2024-01-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v85', @ProjectId, N'', 85, N'VOQ-0085', N'Supply & install demister pad - Tonys wetroom', N'Supply & install demister pad - Tonys wetroom', 2, NULL, NULL, 425.0000, N'V85', 425.0000, N'ELE-STD', '2023-12-24', N'seed@jewelgroup.co.uk', '2024-01-01', '2024-01-15', N'seed@jewelgroup.co.uk', NULL),
        (N'sp-voq-v86', @ProjectId, N'', 86, N'VOQ-0086', N'Handover snagging - outhouse cap, fire alarm, gravel, Nest, towel holders & electric blinds', N'Handover snagging - outhouse cap, fire alarm, gravel, Nest, towel holders & electric blinds', 2, NULL, NULL, 1990.0000, N'V86', 1990.0000, N'HAND-MSC', '2023-12-24', N'seed@jewelgroup.co.uk', '2024-01-01', '2024-01-15', N'seed@jewelgroup.co.uk', NULL)
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
        (N'sp-vo-v01', @ProjectId, 3, N'', N'', N'V01', N'Omit loft staircase & first floor ceiling opening', 2, N'STAIR-TIM', N'', N'item', 1.0000, -5000.0000, -5000.0000, N'', 1),
        (N'sp-vo-v02', @ProjectId, 3, N'', N'', N'V02', N'Asbestos removal - Goodbye Asbestos survey & removal vs PS', 2, N'ENABLE-ASB', N'', N'item', 1.0000, -2144.0000, -2144.0000, N'', 2),
        (N'sp-vo-v03', @ProjectId, 3, N'', N'', N'V03', N'Strip & retile entire main roof vs works-to-roof PS', 0, N'ROOF-TLO', N'', N'item', 1.0000, 18100.0000, 18100.0000, N'', 3),
        (N'sp-vo-v04', @ProjectId, 3, N'', N'', N'V04', N'Drainage survey & remedial works vs PS', 2, N'MEC-DRN', N'', N'item', 1.0000, -3700.0000, -3700.0000, N'', 4),
        (N'sp-vo-v05', @ProjectId, 3, N'', N'', N'V05', N'Structural works & propping vs PS', 2, N'STR-STL', N'', N'item', 1.0000, -2760.0000, -2760.0000, N'', 5),
        (N'sp-vo-v06', @ProjectId, 3, N'', N'', N'V06', N'Building Control fees - paid direct by Jewel Bespoke', 0, N'HAND-MSC', N'', N'item', 1.0000, 1200.0000, 1200.0000, N'', 6),
        (N'sp-vo-v08', @ProjectId, 3, N'', N'', N'V08', N'Excavate & concrete strip footings, pad foundations & padstones', 0, N'SUB-EXC', N'', N'item', 1.0000, 5096.0000, 5096.0000, N'', 7),
        (N'sp-vo-v09', @ProjectId, 3, N'', N'', N'V09', N'Structural steels & associated works vs PS', 0, N'STR-STL', N'', N'item', 1.0000, 5200.0000, 5200.0000, N'', 8),
        (N'sp-vo-v10', @ProjectId, 3, N'', N'', N'V10', N'DPC/DPM detail to drawing WD-P-016 vs contract DPC', 0, N'WPF-DMP', N'', N'item', 1.0000, 1812.0000, 1812.0000, N'', 9),
        (N'sp-vo-v11', @ProjectId, 3, N'', N'', N'V11', N'Party Wall invoice', 0, N'HAND-SPE', N'', N'item', 1.0000, 960.0000, 960.0000, N'', 10),
        (N'sp-vo-v12', @ProjectId, 3, N'', N'', N'V12', N'Structural Engineers - INV-1743', 0, N'HAND-SPE', N'', N'item', 1.0000, 540.0000, 540.0000, N'', 11),
        (N'sp-vo-v13', @ProjectId, 3, N'', N'', N'V13', N'Supply & install loft hatch & pull-down ladder', 0, N'CARP-2FX', N'', N'item', 1.0000, 950.0000, 950.0000, N'', 12),
        (N'sp-vo-v14', @ProjectId, 3, N'', N'', N'V14', N'Supply & install T&G ground floor', 0, N'CARP-1FX', N'', N'item', 1.0000, 3450.0000, 3450.0000, N'', 13),
        (N'sp-vo-v15', @ProjectId, 3, N'', N'', N'V15', N'Velux curved glass & electric rooflights vs contract rooflights', 0, N'WDR-SPG', N'', N'item', 1.0000, 699.2300, 699.2300, N'', 14),
        (N'sp-vo-v16', @ProjectId, 3, N'', N'', N'V16', N'Sanitaryware supply - Taylor Dolman & schedule vs PC6', 0, N'SUP-SAN', N'', N'item', 1.0000, 6840.0000, 6840.0000, N'', 15),
        (N'sp-vo-v18', @ProjectId, 3, N'', N'', N'V18', N'UPVC bay windows vs kitchenette window', 0, N'WDR-UPV', N'', N'item', 1.0000, 8275.0000, 8275.0000, N'', 16),
        (N'sp-vo-v19', @ProjectId, 3, N'', N'', N'V19', N'Howdens kitchen & utility - supply, worktop & installation vs PS', 0, N'SUP-KIT', N'', N'item', 1.0000, 8425.0000, 8425.0000, N'', 17),
        (N'sp-vo-v20', @ProjectId, 3, N'', N'', N'V20', N'Aluminium sliding doors to kitchen vs upvc French doors', 0, N'WDR-ALU', N'', N'item', 1.0000, 703.0000, 703.0000, N'', 18),
        (N'sp-vo-v21', @ProjectId, 3, N'', N'', N'V21', N'Fire & smoke detection vs PS', 2, N'ELE-FIR', N'', N'item', 1.0000, -1865.0000, -1865.0000, N'', 19),
        (N'sp-vo-v22', @ProjectId, 3, N'', N'', N'V22', N'Alarm system - Ring doorbell vs PS', 2, N'ELE-ALM', N'', N'item', 1.0000, -860.0000, -860.0000, N'', 20),
        (N'sp-vo-v23', @ProjectId, 3, N'', N'', N'V23', N'Outhouse floor - strip, T&G & Karndean', 0, N'FLR-LVT', N'', N'item', 1.0000, 4175.0000, 4175.0000, N'', 21),
        (N'sp-vo-v24', @ProjectId, 3, N'', N'', N'V24', N'6ft close board fencing - extended length', 0, N'EXTW-FEN', N'', N'item', 1.0000, 4620.0000, 4620.0000, N'', 22),
        (N'sp-vo-v25', @ProjectId, 3, N'', N'', N'V25', N'Omit hot tub provisional sum', 2, N'SPEC-SPA', N'', N'item', 1.0000, -12500.0000, -12500.0000, N'', 23),
        (N'sp-vo-v26', @ProjectId, 3, N'', N'', N'V26', N'Omit garden gazebo provisional sum', 2, N'SPEC-GAZ', N'', N'item', 1.0000, -7500.0000, -7500.0000, N'', 24),
        (N'sp-vo-v27', @ProjectId, 3, N'', N'', N'V27', N'Composite entrance doors with side lights - revised supply', 0, N'WDR-TIM', N'', N'item', 1.0000, 4050.0000, 4050.0000, N'', 25),
        (N'sp-vo-v28', @ProjectId, 3, N'', N'', N'V28', N'External works redesign - retaining wall, edging, topsoil vs planters/balustrade/landscaping', 2, N'EXTW-LND', N'', N'item', 1.0000, -850.0000, -850.0000, N'', 26),
        (N'sp-vo-v29', @ProjectId, 3, N'', N'', N'V29', N'Data / TV points & relocation of bedroom wiring', 0, N'ELE-STD', N'', N'item', 1.0000, 2480.0000, 2480.0000, N'', 27),
        (N'sp-vo-v30', @ProjectId, 3, N'', N'', N'V30', N'HSS generator hire - week 1', 0, N'PRELIMS-TMP', N'', N'item', 1.0000, 500.0000, 500.0000, N'', 28),
        (N'sp-vo-v31', @ProjectId, 3, N'', N'', N'V31', N'Omit PV solar panels', 2, N'MEC-SOL', N'', N'item', 1.0000, -3500.0000, -3500.0000, N'', 29),
        (N'sp-vo-v32', @ProjectId, 3, N'', N'', N'V32', N'Omit kitchenette to out building', 2, N'CARP-KIT', N'', N'item', 1.0000, -2000.0000, -2000.0000, N'', 30),
        (N'sp-vo-v33', @ProjectId, 3, N'', N'', N'V33', N'HSS generator hire - week 2', 0, N'PRELIMS-TMP', N'', N'item', 1.0000, 500.0000, 500.0000, N'', 31),
        (N'sp-vo-v34', @ProjectId, 3, N'', N'', N'V34', N'Staircase & balustrade redesign incl. decoration', 0, N'STAIR-TIM', N'', N'item', 1.0000, 1155.0000, 1155.0000, N'', 32),
        (N'sp-vo-v35', @ProjectId, 3, N'', N'', N'V35', N'HSS generator hire - weeks 3 & 4', 0, N'PRELIMS-TMP', N'', N'item', 1.0000, 1000.0000, 1000.0000, N'', 33),
        (N'sp-vo-v36', @ProjectId, 3, N'', N'', N'V36', N'Wall tiling supply per schedule vs PS', 0, N'SUP-TIL', N'', N'item', 1.0000, 565.0000, 565.0000, N'', 34),
        (N'sp-vo-v37', @ProjectId, 3, N'', N'', N'V37', N'Karndean to first floor vs underlay & carpet', 0, N'FLR-LVT', N'', N'item', 1.0000, 2170.0000, 2170.0000, N'', 35),
        (N'sp-vo-v38', @ProjectId, 3, N'', N'', N'V38', N'Omit wardrobes & storage provisional sum', 2, N'CARP-WRD', N'', N'item', 1.0000, -8000.0000, -8000.0000, N'', 36),
        (N'sp-vo-v39', @ProjectId, 3, N'', N'', N'V39', N'HSS generator hire - week 5', 0, N'PRELIMS-TMP', N'', N'item', 1.0000, 500.0000, 500.0000, N'', 37),
        (N'sp-vo-v40', @ProjectId, 3, N'', N'', N'V40', N'Electrical amendments - Bedroom 2', 0, N'ELE-STD', N'', N'item', 1.0000, 280.0000, 280.0000, N'', 38),
        (N'sp-vo-v41', @ProjectId, 3, N'', N'', N'V41', N'Underlay & carpet - staircase runner', 0, N'FLR-CPT', N'', N'item', 1.0000, 885.0000, 885.0000, N'', 39),
        (N'sp-vo-v42', @ProjectId, 3, N'', N'', N'V42', N'Kitchen amendments credit', 2, N'SUP-KIT', N'', N'item', 1.0000, -284.8200, -284.8200, N'', 40),
        (N'sp-vo-v43', @ProjectId, 3, N'', N'', N'V43', N'Internal door ironmongery vs PS', 2, N'SUP-IRO', N'', N'item', 1.0000, -1316.0000, -1316.0000, N'', 41),
        (N'sp-vo-v44', @ProjectId, 3, N'', N'', N'V44', N'Landscape works - IN610-008', 0, N'EXTW-LND', N'', N'item', 1.0000, 7820.0000, 7820.0000, N'', 42),
        (N'sp-vo-v45', @ProjectId, 3, N'', N'', N'V45', N'EOT-01 prelims - site supervision, H&S & temp toilet', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 4825.0000, 4825.0000, N'', 43),
        (N'sp-vo-v46', @ProjectId, 3, N'', N'', N'V46', N'HSS generator hire - week 6', 0, N'PRELIMS-TMP', N'', N'item', 1.0000, 500.0000, 500.0000, N'', 44),
        (N'sp-vo-v47', @ProjectId, 3, N'', N'', N'V47', N'HSS generator hire - week 7', 0, N'PRELIMS-TMP', N'', N'item', 1.0000, 500.0000, 500.0000, N'', 45),
        (N'sp-vo-v48', @ProjectId, 3, N'', N'', N'V48', N'HSS generator hire - week 8', 0, N'PRELIMS-TMP', N'', N'item', 1.0000, 500.0000, 500.0000, N'', 46),
        (N'sp-vo-v49', @ProjectId, 3, N'', N'', N'V49', N'Supply & install outside tap to front', 0, N'MEC-PLM', N'', N'item', 1.0000, 120.0000, 120.0000, N'', 47),
        (N'sp-vo-v50', @ProjectId, 3, N'', N'', N'V50', N'M&E additions - spot lights, sockets & making good', 0, N'ELE-STD', N'', N'item', 1.0000, 3241.0000, 3241.0000, N'', 48),
        (N'sp-vo-v51', @ProjectId, 3, N'', N'', N'V51', N'HSS generator hire - week 9', 0, N'PRELIMS-TMP', N'', N'item', 1.0000, 500.0000, 500.0000, N'', 49),
        (N'sp-vo-v52', @ProjectId, 3, N'', N'', N'V52', N'Electrics - Tonys rooms shaver socket & making good', 0, N'ELE-STD', N'', N'item', 1.0000, 310.0000, 310.0000, N'', 50),
        (N'sp-vo-v53', @ProjectId, 3, N'', N'', N'V53', N'Sheds, concrete base, Marshalls paving & topsoil', 0, N'EXTW-LND', N'', N'item', 1.0000, 6840.0000, 6840.0000, N'', 51),
        (N'sp-vo-v54', @ProjectId, 3, N'', N'', N'V54', N'Mailbox - supply & install', 0, N'HAND-MSC', N'', N'item', 1.0000, 80.0000, 80.0000, N'', 52),
        (N'sp-vo-v55', @ProjectId, 3, N'', N'', N'V55', N'HSS generator hire - week 10', 0, N'PRELIMS-TMP', N'', N'item', 1.0000, 500.0000, 500.0000, N'', 53),
        (N'sp-vo-v56', @ProjectId, 3, N'', N'', N'V56', N'Blinds per schedule vs blinds & curtains PS', 2, N'WIN-BLD', N'', N'item', 1.0000, -627.6500, -627.6500, N'', 54),
        (N'sp-vo-v57', @ProjectId, 3, N'', N'', N'V57', N'HSS generator hire - week 11', 0, N'PRELIMS-TMP', N'', N'item', 1.0000, 500.0000, 500.0000, N'', 55),
        (N'sp-vo-v58', @ProjectId, 3, N'', N'', N'V58', N'External painting', 0, N'DEC-STD', N'', N'item', 1.0000, 2800.0000, 2800.0000, N'', 56),
        (N'sp-vo-v59', @ProjectId, 3, N'', N'', N'V59', N'HSS generator hire - week 12', 0, N'PRELIMS-TMP', N'', N'item', 1.0000, 500.0000, 500.0000, N'', 57),
        (N'sp-vo-v60', @ProjectId, 3, N'', N'', N'V60', N'Wardrobes - Ikea units, TV unit & storeroom joinery vs PS', 0, N'CARP-WRD', N'', N'item', 1.0000, 3154.9600, 3154.9600, N'', 58),
        (N'sp-vo-v61', @ProjectId, 3, N'', N'', N'V61', N'Side entrance gate', 0, N'EXTW-FEN', N'', N'item', 1.0000, 295.0000, 295.0000, N'', 59),
        (N'sp-vo-v62', @ProjectId, 3, N'', N'', N'V62', N'WC vanity unit', 0, N'SUP-SAN', N'', N'item', 1.0000, 595.0000, 595.0000, N'', 60),
        (N'sp-vo-v63', @ProjectId, 3, N'', N'', N'V63', N'Building Control payment - re-roof', 0, N'HAND-MSC', N'', N'item', 1.0000, 396.0000, 396.0000, N'', 61),
        (N'sp-vo-v65', @ProjectId, 3, N'', N'', N'V65', N'Side path paving', 0, N'EXTW-PAV', N'', N'item', 1.0000, 275.0000, 275.0000, N'', 62),
        (N'sp-vo-v66', @ProjectId, 3, N'', N'', N'V66', N'Staircase spindles', 0, N'STAIR-TIM', N'', N'item', 1.0000, 460.0000, 460.0000, N'', 63),
        (N'sp-vo-v67', @ProjectId, 3, N'', N'', N'V67', N'Lamona extractor fan', 0, N'SUP-APP', N'', N'item', 1.0000, 477.0200, 477.0200, N'', 64),
        (N'sp-vo-v68', @ProjectId, 3, N'', N'', N'V68', N'Electrical & painting amendments', 0, N'ELE-STD', N'', N'item', 1.0000, 1230.0000, 1230.0000, N'', 65),
        (N'sp-vo-v69', @ProjectId, 3, N'', N'', N'V69', N'External paving & pillar', 0, N'EXTW-PAV', N'', N'item', 1.0000, 475.0000, 475.0000, N'', 66),
        (N'sp-vo-v70', @ProjectId, 3, N'', N'', N'V70', N'Supply & install door stops', 0, N'SUP-IRO', N'', N'item', 1.0000, 450.0000, 450.0000, N'', 67),
        (N'sp-vo-v71', @ProjectId, 3, N'', N'', N'V71', N'Supply of dining room lights', 0, N'ELE-STD', N'', N'item', 1.0000, 145.0000, 145.0000, N'', 68),
        (N'sp-vo-v72', @ProjectId, 3, N'', N'', N'V72', N'Howdens sink, worktop & units', 0, N'SUP-KIT', N'', N'item', 1.0000, 10012.0000, 10012.0000, N'', 69),
        (N'sp-vo-v73', @ProjectId, 3, N'', N'', N'V73', N'Kitchen window, cill & external works', 0, N'WDR-UPV', N'', N'item', 1.0000, 2910.0000, 2910.0000, N'', 70),
        (N'sp-vo-v74', @ProjectId, 3, N'', N'', N'V74', N'Extension of prelims EOT-02 x 6 weeks', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 5790.0000, 5790.0000, N'', 71),
        (N'sp-vo-v75', @ProjectId, 3, N'', N'', N'V75', N'Easi Hold resin - French drains', 0, N'SUB-DRN', N'', N'item', 1.0000, 240.0000, 240.0000, N'', 72),
        (N'sp-vo-v76', @ProjectId, 3, N'', N'', N'V76', N'Towel radiator - main bathroom', 0, N'MEC-PLM', N'', N'item', 1.0000, 395.0000, 395.0000, N'', 73),
        (N'sp-vo-v77', @ProjectId, 3, N'', N'', N'V77', N'Shower hose', 0, N'SUP-SAN', N'', N'item', 1.0000, 120.0000, 120.0000, N'', 74),
        (N'sp-vo-v79', @ProjectId, 3, N'', N'', N'V79', N'Towel radiator relocation - main bathroom', 0, N'MEC-PLM', N'', N'item', 1.0000, 880.0000, 880.0000, N'', 75),
        (N'sp-vo-v80', @ProjectId, 3, N'', N'', N'V80', N'Remove existing mirrors & replace', 0, N'HAND-MSC', N'', N'item', 1.0000, 200.0000, 200.0000, N'', 76),
        (N'sp-vo-v82', @ProjectId, 3, N'', N'', N'V82', N'Extension of prelims EOT-03 x 3 weeks', 0, N'PRELIMS-SMG', N'', N'item', 1.0000, 2895.0000, 2895.0000, N'', 77),
        (N'sp-vo-v83', @ProjectId, 3, N'', N'', N'V83', N'Outhouse boxing', 0, N'CARP-2FX', N'', N'item', 1.0000, 1340.0000, 1340.0000, N'', 78),
        (N'sp-vo-v84', @ProjectId, 3, N'', N'', N'V84', N'Electrical supply to mirrors', 0, N'ELE-STD', N'', N'item', 1.0000, 610.0000, 610.0000, N'', 79),
        (N'sp-vo-v85', @ProjectId, 3, N'', N'', N'V85', N'Supply & install demister pad - Tonys wetroom', 0, N'ELE-STD', N'', N'item', 1.0000, 425.0000, 425.0000, N'', 80),
        (N'sp-vo-v86', @ProjectId, 3, N'', N'', N'V86', N'Handover snagging - outhouse cap, fire alarm, gravel, Nest, towel holders & electric blinds', 0, N'HAND-MSC', N'', N'item', 1.0000, 1990.0000, 1990.0000, N'', 81)
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
    PRINT '24 Sherwood Park: variation orders & variation lines merged.';
    COMMIT TRAN;

    -- Sanity check: variation lines should reconcile to the workbook register.
    SELECT
        COUNT(*) AS VariationLines,                                                       -- 81
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations  -- 97518.74
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType = 3;

    -- Combined: Contract Sum + Net Variations = Revised (Live Build) Sum.
    SELECT
        SUM(CASE WHEN ElementType IN (0,1,2) AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS ContractSum,   -- 563138.00
        SUM(CASE WHEN ElementType = 3        AND LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS NetVariations, --  97518.74
        SUM(CASE WHEN LineType NOT IN (3,4) THEN LineAmount ELSE 0 END) AS RevisedContractSum                        -- 660656.74
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId;

    -- VOQ records mirror the report lines: one approved VOQ per line, same net.
    SELECT
        (SELECT COUNT(*)   FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = @ProjectId) AS VariationOrders,        -- 86
        (SELECT COUNT(*)   FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = @ProjectId AND Status = 2) AS Approved, -- 81
        (SELECT SUM(Value) FROM [dbo].[VariationOrderQuotes] WHERE ProjectId = @ProjectId AND Status = 2) AS NetVoValue; -- 97518.74
END
GO
