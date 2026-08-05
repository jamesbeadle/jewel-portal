-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed,
-- per JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: Longwood (Horsham Road, Cranleigh) -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : Longwood, 133 Horsham Road, Cranleigh
-- ProjectId: resolved at run time by site-name matcher 'horshamroadlongwoodcranleigh'
--
-- Seeds the ORIGINAL contract scope only, taken from the
-- "Longwood Valuation 1 - Sept 25" workbook. Three blocks make up the
-- Contract Sum, as per the Albany Mews pattern:
--
--     Contract works    GBP 575,384.00
--     Provisional Sums  GBP 140,280.00   (PC01..PC21, the 12 present)
--     Contingency       GBP  50,000.00
--     ----------------------------------
--     Contract Sum      GBP 765,664.00
--
-- Variations (V01..V02, net GBP -1,630.00) are NOT seeded here -- they belong
-- in seed-longwood-variations.sql. Per-valuation claim history (Claim 1..7,
-- retention) is claim data (ValuationClaims/ClaimLines), not bill structure.
--
-- SectionCode/SectionName retain the workbook's NRM-style references; PS lines
-- retain their PC codes. CostCode maps each section to the Jewel cost-centre
-- master (from seed-cost-centers.sql), consistent with the Ravenswood/Albany
-- seeds; the workbook's own numeric codes (0001..0044) are dropped.
-- "Omit item Vnn" comments are informational: those lines are omitted by
-- variations in the register, so they stay Priced/ProvisionalSum here.
--
-- SKIPPED workbook rows (zero contract value):
--     D20 Excavation & filling  -- single row "completed", no amount
--     E10 In situ concrete      -- single row "completed", no amount
--     K1  Floors                -- single row "completed", no amount
--
-- Judgement notes:
--   * Inline "- Provisional sum" lines -> LineType 1; whole sections the
--     workbook flags as provisional (L10 "Provision Sum", T90 "All
--     Provisional", W90 "All Provisional") -> LineType 1 for their lines.
--   * "- Provisional area" lines (R11 drainage runs, Q22 make good / gravel /
--     paving) are remeasurable quantities, not provisional sums -> Priced.
--   * PS block rates are "PS" text -> Quantity 1, Rate = amount.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (lw-cw-NNN / lw-ps-NN
-- / lw-cont-NN). A re-run refreshes every field via MERGE. Variation lines for
-- this project are left untouched. Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'horshamroadlongwoodcranleigh'
       OR LOWER(REPLACE(Name, ' ', '')) = 'horshamroadlongwoodcranleigh'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'horshamroadlongwoodcranleigh' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  Longwood (Horsham Road, Cranleigh) — no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
        (N'lw-cw-001', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-PMG', N'Project manager - 1 day pw', N'week', 24.0000, 550.0000, 13200.0000, N'', 1),
        (N'lw-cw-002', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site manager - 5 days pw', N'week', 24.0000, 1250.0000, 30000.0000, N'', 2),
        (N'lw-cw-003', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-LAB', N'Site labour - 3 days pw', N'week', 24.0000, 450.0000, 10800.0000, N'', 3),
        (N'lw-cw-004', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-HRD', N'Hoarding & protection', N'week', 24.0000, 125.0000, 3000.0000, N'', 4),
        (N'lw-cw-005', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Rubbish removal', N'skips', 18.0000, 380.0000, 6840.0000, N'', 5),
        (N'lw-cw-006', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WC', N'Temporary toilet', N'week', 24.0000, 90.0000, 2160.0000, N'', 6),
        (N'lw-cw-007', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Temporary welfare', N'week', 24.0000, 110.0000, 2640.0000, N'', 7),
        (N'lw-cw-008', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-SEC', N'Site security', N'week', 24.0000, 75.0000, 1800.0000, N'', 8),
        (N'lw-cw-009', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'ELE-STD', N'Plant, lighting & machinery', N'week', 24.0000, 55.0000, 1320.0000, N'', 9),
        (N'lw-cw-010', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'ELE-STD', N'Temporary plumbing & electrics', N'item', 1.0000, 1500.0000, 1500.0000, N'', 10),
        (N'lw-cw-011', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-HSC', N'CDM', N'item', 1.0000, 1200.0000, 1200.0000, N'', 11),
        (N'lw-cw-012', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'nr', 24.0000, 125.0000, 3000.0000, N'', 12),
        (N'lw-cw-013', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Clean on completion', N'item', 1.0000, 1200.0000, 1200.0000, N'', 13),
        (N'lw-cw-014', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 1, N'SCAFF-STD', N'Scaffolding - Provisional sum', N'item', 1.0000, 10000.0000, 10000.0000, N'Omit item V01', 14),
        (N'lw-cw-015', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove existing windows', N'nr', 4.0000, 52.0000, 208.0000, N'', 15),
        (N'lw-cw-016', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove chimney liner & block off flue', N'item', 1.0000, 550.0000, 550.0000, N'', 16),
        (N'lw-cw-017', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Check existing floorboards & dry out - (no allowance for replace)', N'item', 1.0000, 350.0000, 350.0000, N'', 17),
        (N'lw-cw-018', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove existing hot water cylinder/ system', N'item', 1.0000, 750.0000, 750.0000, N'', 18),
        (N'lw-cw-019', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove sanitry from ground floor WC', N'item', 1.0000, 120.0000, 120.0000, N'', 19),
        (N'lw-cw-020', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish section of masonry wall from first floor corridor', N'item', 1.0000, 240.0000, 240.0000, N'', 20),
        (N'lw-cw-021', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove remaining lath plaster from first floor as required', N'item', 1.0000, 450.0000, 450.0000, N'', 21),
        (N'lw-cw-022', @ProjectId, 0, N'P30', N'Trenches, pipeways for engineering services', N'', N'', 1, N'UTIL-TRN', N'All assosiated work - Provisional sum', N'item', 1.0000, 1500.0000, 1500.0000, N'Omit item V02', 22),
        (N'lw-cw-023', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-DRN', N'Aco slot drains', N'm', 20.0000, 132.0000, 2640.0000, N'', 23),
        (N'lw-cw-024', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 1, N'SUB-EXC', N'Excavate & lay new underground drainage runs - Provisional sum', N'm', 10.0000, 125.0000, 1250.0000, N'', 24),
        (N'lw-cw-025', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 1, N'SUB-DRN', N'Make connection into existing runs - Provisional sum', N'day', 1.0000, 550.0000, 550.0000, N'', 25),
        (N'lw-cw-026', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-DRN', N'New soakaway', N'nr', 1.0000, 1150.0000, 1150.0000, N'', 26),
        (N'lw-cw-027', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Stub stack & durgo valve', N'nr', 5.0000, 130.0000, 650.0000, N'', 27),
        (N'lw-cw-028', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Soil vent pipework & connections', N'm', 10.0000, 80.0000, 800.0000, N'', 28),
        (N'lw-cw-029', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Wastes connections', N'nr', 22.0000, 88.0000, 1936.0000, N'', 29),
        (N'lw-cw-030', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs - Provisional area', N'm', 10.0000, 125.0000, 1250.0000, N'', 30),
        (N'lw-cw-031', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'SUB-CON', N'Precast lintels over drainage runs', N'item', 1.0000, 400.0000, 400.0000, N'', 31),
        (N'lw-cw-032', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'New manhole', N'nr', 2.0000, 725.0000, 1450.0000, N'', 32),
        (N'lw-cw-033', @ProjectId, 0, N'E60', N'Pre cast concrete floors', N'', N'', 0, N'INT-INF', N'165 mm polystirene insulation - to new B & B flooring only', N'm2', 158.0000, 52.0000, 8216.0000, N'', 33),
        (N'lw-cw-034', @ProjectId, 0, N'M10', N'Cement based levelling screeds', N'', N'', 0, N'FLR-SCR', N'75 mm sand / cement floor screed', N'm2', 158.0000, 90.0000, 14220.0000, N'', 34),
        (N'lw-cw-035', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'Tidying of openings', N'item', 1.0000, 750.0000, 750.0000, N'', 35),
        (N'lw-cw-036', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'Thermabate cavity closers', N'm', 40.0000, 22.0000, 880.0000, N'', 36),
        (N'lw-cw-037', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'Block up existing windows', N'nr', 4.0000, 202.0000, 808.0000, N'', 37),
        (N'lw-cw-038', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Complete dormer partitioning as required', N'item', 1.0000, 900.0000, 900.0000, N'', 38),
        (N'lw-cw-039', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-TLN', N'Make good membrane & battens / new as required', N'item', 1.0000, 1800.0000, 1800.0000, N'', 39),
        (N'lw-cw-040', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-TLN', N'Concrete tiles', N'm2', 128.0000, 56.0000, 7168.0000, N'', 40),
        (N'lw-cw-041', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Ridge / hip tiles', N'm', 24.0000, 65.0000, 1560.0000, N'', 41),
        (N'lw-cw-042', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Separate cost for supply of tiles (£1.00 each)', N'm2', 128.0000, 60.0000, 7680.0000, N'', 42),
        (N'lw-cw-043', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Roof ventilators', N'nr', 14.0000, 85.0000, 1190.0000, N'', 43),
        (N'lw-cw-044', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-LED', N'Lead flashing / valley lining / soakers / aprons', N'item', 1.0000, 1500.0000, 1500.0000, N'', 44),
        (N'lw-cw-045', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-FSU', N'Fascia / soffit', N'm', 58.0000, 48.0000, 2784.0000, N'', 45),
        (N'lw-cw-046', @ProjectId, 0, N'H74', N'Zinc cladding', N'', N'', 0, N'ROOF-FLT', N'VM Zinc standing seam to dormer', N'm2', 25.0000, 398.0000, 9950.0000, N'', 46),
        (N'lw-cw-047', @ProjectId, 0, N'J40', N'Flexible sheet waterproofing', N'', N'', 0, N'WPF-DMP', N'Damp proof membranes 1200 g', N'm2', 158.0000, 16.0000, 2528.0000, N'', 47),
        (N'lw-cw-048', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'ROOF-FLT', N'Complete upstand to flat roof light', N'item', 1.0000, 420.0000, 420.0000, N'', 48),
        (N'lw-cw-049', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'ROOF-FLT', N'Sarnifli single ply membrane', N'm2', 140.0000, 150.0000, 21000.0000, N'', 49),
        (N'lw-cw-050', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'ROOF-LED', N'Lead flashing / valley lining', N'm', 25.0000, 62.0000, 1550.0000, N'', 50),
        (N'lw-cw-051', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'ROOF-FLT', N'Single ply to secret gutter', N'm', 28.0000, 45.0000, 1260.0000, N'', 51),
        (N'lw-cw-052', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'EXT-MCP', N'Metal copping detail to parapet wall', N'm', 42.0000, 132.0000, 5544.0000, N'', 52),
        (N'lw-cw-053', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'ROOF-FSM', N'Bespoke soffit to ground floor roof', N'm', 42.0000, 88.0000, 3696.0000, N'', 53),
        (N'lw-cw-054', @ProjectId, 0, N'R10', N'Rainwater drainage systems', N'', N'', 0, N'ROOF-GRU', N'Guttering', N'm', 52.0000, 34.0000, 1768.0000, N'', 54),
        (N'lw-cw-055', @ProjectId, 0, N'R10', N'Rainwater drainage systems', N'', N'', 0, N'ROOF-GRU', N'Rainwater pipework', N'm', 38.0000, 36.0000, 1368.0000, N'', 55),
        (N'lw-cw-056', @ProjectId, 0, N'H21', N'Timber weatherboarding', N'', N'', 0, N'EXT-TIC', N'Breatherable membrane, battens & Western red ceder (Supply 10m2)', N'm2', 40.0000, 162.0000, 6480.0000, N'', 56),
        (N'lw-cw-057', @ProjectId, 0, N'G12', N'Isolated structural metal members', N'', N'', 0, N'STR-STL', N'Make good around steel column', N'item', 1.0000, 300.0000, 300.0000, N'', 57),
        (N'lw-cw-058', @ProjectId, 0, N'P10', N'Sundry insulation', N'', N'', 0, N'CARP-1FX', N'Plywood boxing & insulation to internal pipes', N'day', 2.0000, 300.0000, 600.0000, N'', 58),
        (N'lw-cw-059', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'CARP-1FX', N'38 x 89 mm timber internal stud walls', N'm2', 28.0000, 62.0000, 1736.0000, N'', 59),
        (N'lw-cw-060', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INC', N'150 mm Kingspan insulation to flat roof deck joists', N'm2', 140.0000, 52.0000, 7280.0000, N'', 60),
        (N'lw-cw-061', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INC', N'100 mm Kingspan insulation between roof rafters', N'm2', 60.0000, 42.0000, 2520.0000, N'', 61),
        (N'lw-cw-062', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INC', N'30 mm Kingspan insulation under rafters', N'm2', 60.0000, 24.0000, 1440.0000, N'', 62),
        (N'lw-cw-063', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INC', N'300 mm mineral insulation to eaves space', N'm2', 80.0000, 40.0000, 3200.0000, N'', 63),
        (N'lw-cw-064', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INF', N'100 mm mineral insulation between floor joists', N'm2', 136.0000, 28.0000, 3808.0000, N'', 64),
        (N'lw-cw-065', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-PLB', N'12.5mm plasterboard to ceilings', N'm2', 368.0000, 20.0000, 7360.0000, N'', 65),
        (N'lw-cw-066', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INW', N'100 mm Kingspan to dormer walls', N'm2', 25.0000, 38.0000, 950.0000, N'', 66),
        (N'lw-cw-067', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INW', N'50 mm rockwool insulation between stud walls', N'm2', 78.0000, 16.0000, 1248.0000, N'', 67),
        (N'lw-cw-068', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INW', N'52.5 mm Kingspan insulation to dormer walls', N'm2', 18.0000, 36.0000, 648.0000, N'', 68),
        (N'lw-cw-069', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-PLB', N'12.5 mm plasterboard to studs & exsitng walls', N'm2', 576.0000, 20.0000, 11520.0000, N'', 69),
        (N'lw-cw-070', @ProjectId, 0, N'K11', N'Rigid sheet flooring/sheathing/decking', N'', N'', 0, N'CARP-1FX', N'22 mm T&G chipboard flooring to new first floor', N'm2', 88.0000, 34.0000, 2992.0000, N'', 70),
        (N'lw-cw-071', @ProjectId, 0, N'K11', N'Rigid sheet flooring/sheathing/decking', N'', N'', 0, N'CARP-1FX', N'18 mm plywood to stud walls', N'm2', 68.0000, 28.0000, 1904.0000, N'', 71),
        (N'lw-cw-072', @ProjectId, 0, N'L30', N'Stairs/ladders/walkways', N'', N'', 1, N'STAIR-TIM', N'Make good / overhaul existing staircase - Provisional sum', N'item', 1.0000, 750.0000, 750.0000, N'', 72),
        (N'lw-cw-073', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres - Provision Sum', N'', N'', 1, N'WDR-TIM', N'New windows & external doors', N'nr', 1.0000, 35000.0000, 35000.0000, N'', 73),
        (N'lw-cw-074', @ProjectId, 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'MEC-PLM', N'Hot & cold water supply', N'nr', 31.0000, 180.0000, 5580.0000, N'', 74),
        (N'lw-cw-075', @ProjectId, 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'MEC-PLM', N'Water softner', N'nr', 1.0000, 855.0000, 855.0000, N'', 75),
        (N'lw-cw-076', @ProjectId, 0, N'T90', N'Heating systems- All Provisional', N'', N'', 1, N'MEC-BLR', N'New central heating boiler & associated pipework', N'nr', 1.0000, 4450.0000, 4450.0000, N'', 76),
        (N'lw-cw-077', @ProjectId, 0, N'T90', N'Heating systems- All Provisional', N'', N'', 1, N'UTIL-STD', N'Relocate gas meter', N'nr', 1.0000, 750.0000, 750.0000, N'', 77),
        (N'lw-cw-078', @ProjectId, 0, N'T90', N'Heating systems- All Provisional', N'', N'', 1, N'MEC-PLM', N'Megaflor hot water cylinder', N'nr', 1.0000, 2980.0000, 2980.0000, N'', 78),
        (N'lw-cw-079', @ProjectId, 0, N'T90', N'Heating systems- All Provisional', N'', N'', 1, N'MEC-PLM', N'Radiators with TRVs', N'nr', 19.0000, 475.0000, 9025.0000, N'', 79),
        (N'lw-cw-080', @ProjectId, 0, N'T90', N'Heating systems- All Provisional', N'', N'', 1, N'MEC-PLM', N'Towel rails with TRVS', N'nr', 6.0000, 500.0000, 3000.0000, N'', 80),
        (N'lw-cw-081', @ProjectId, 0, N'T90', N'Heating systems- All Provisional', N'', N'', 1, N'MEC-UFH', N'Wet underfloor heating to new areas', N'm2', 158.0000, 128.0000, 20224.0000, N'', 81),
        (N'lw-cw-082', @ProjectId, 0, N'T90', N'Heating systems- All Provisional', N'', N'', 1, N'MEC-UFH', N'Manifold & thermostats', N'nr', 4.0000, 450.0000, 1800.0000, N'', 82),
        (N'lw-cw-083', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 1, N'SUP-SAN', N'Supply of sanitry items - Provisional sum', N'item', 1.0000, 8000.0000, 8000.0000, N'', 83),
        (N'lw-cw-084', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-SAN', N'Fix only - WC', N'nr', 5.0000, 320.0000, 1600.0000, N'', 84),
        (N'lw-cw-085', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-SAN', N'Fix only - Wash hand basin', N'nr', 5.0000, 310.0000, 1550.0000, N'', 85),
        (N'lw-cw-086', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-SAN', N'Fix only - Showers / baths', N'nr', 4.0000, 500.0000, 2000.0000, N'', 86),
        (N'lw-cw-087', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-SAN', N'Fix only - Mirrors, towel rails, hooks, etc', N'day', 1.0000, 300.0000, 300.0000, N'', 87),
        (N'lw-cw-088', @ProjectId, 0, N'U90', N'General ventilation', N'', N'', 0, N'MEC-VNT', N'Extract fan', N'nr', 7.0000, 275.0000, 1925.0000, N'', 88),
        (N'lw-cw-089', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Consumer unit', N'nr', 1.0000, 1150.0000, 1150.0000, N'', 89),
        (N'lw-cw-090', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'UTIL-STD', N'Relocate electric meter', N'nr', 1.0000, 750.0000, 750.0000, N'', 90),
        (N'lw-cw-091', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Double socket outlet', N'nr', 112.0000, 115.0000, 12880.0000, N'', 91),
        (N'lw-cw-092', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'5 amp light socket', N'nr', 12.0000, 110.0000, 1320.0000, N'', 92),
        (N'lw-cw-093', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'External double socket', N'nr', 2.0000, 120.0000, 240.0000, N'', 93),
        (N'lw-cw-094', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'TV / data', N'nr', 10.0000, 120.0000, 1200.0000, N'', 94),
        (N'lw-cw-095', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Fused spurs', N'nr', 16.0000, 100.0000, 1600.0000, N'', 95),
        (N'lw-cw-096', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Cooker switch', N'nr', 1.0000, 120.0000, 120.0000, N'', 96),
        (N'lw-cw-097', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Shavers socket', N'nr', 3.0000, 95.0000, 285.0000, N'', 97),
        (N'lw-cw-098', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Recessed light fitting', N'nr', 118.0000, 108.0000, 12744.0000, N'', 98),
        (N'lw-cw-099', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Pendant lights', N'nr', 14.0000, 88.0000, 1232.0000, N'', 99),
        (N'lw-cw-100', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'External lights', N'nr', 20.0000, 175.0000, 3500.0000, N'', 100),
        (N'lw-cw-101', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 44.0000, 40.0000, 1760.0000, N'', 101),
        (N'lw-cw-102', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Wall lights', N'nr', 10.0000, 145.0000, 1450.0000, N'', 102),
        (N'lw-cw-103', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Kitchen strip lighting', N'm', 10.0000, 120.0000, 1200.0000, N'', 103),
        (N'lw-cw-104', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Fan isolator switches', N'nr', 7.0000, 100.0000, 700.0000, N'', 104),
        (N'lw-cw-105', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Smoke/heat detector', N'nr', 5.0000, 150.0000, 750.0000, N'', 105),
        (N'lw-cw-106', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Carbon monoxide detector', N'nr', 1.0000, 118.0000, 118.0000, N'', 106),
        (N'lw-cw-107', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'MEC-UFH', N'Electric heat matting', N'm2', 10.0000, 50.0000, 500.0000, N'', 107),
        (N'lw-cw-108', @ProjectId, 0, N'W90', N'Communications & security systems - All Provisional', N'', N'', 1, N'PRELIMS-SEC', N'All assoisated works', N'nr', 1.0000, 2000.0000, 2000.0000, N'', 108),
        (N'lw-cw-109', @ProjectId, 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-RDR', N'Silicon coloured render to external walls', N'm2', 130.0000, 140.0000, 18200.0000, N'', 109),
        (N'lw-cw-110', @ProjectId, 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-PLS', N'3 mm skim to ceilings', N'm2', 368.0000, 20.0000, 7360.0000, N'', 110),
        (N'lw-cw-111', @ProjectId, 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-PLS', N'3 mm skim to new & existing walls', N'm2', 576.0000, 20.0000, 11520.0000, N'', 111),
        (N'lw-cw-112', @ProjectId, 0, N'L20', N'Doors/ shutters/ hatches', N'', N'', 0, N'SUP-DOR', N'Internal door lining & single door ( £150 supply )', N'nr', 14.0000, 375.0000, 5250.0000, N'', 112),
        (N'lw-cw-113', @ProjectId, 0, N'L20', N'Doors/ shutters/ hatches', N'', N'', 0, N'SUP-DOR', N'Internal door lining & double door ( £300 supply )', N'nr', 5.0000, 780.0000, 3900.0000, N'', 113),
        (N'lw-cw-114', @ProjectId, 0, N'L20', N'Doors/ shutters/ hatches', N'', N'', 0, N'SUP-DOR', N'Internal door lining & single pocket door (£600 supply)', N'nr', 2.0000, 1225.0000, 2450.0000, N'', 114),
        (N'lw-cw-115', @ProjectId, 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF architraves to new door frames (£4/m supply)', N'm', 400.0000, 14.0000, 5600.0000, N'', 115),
        (N'lw-cw-116', @ProjectId, 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF skirting to walls to new walls (£8/m supply)', N'm', 342.0000, 28.0000, 9576.0000, N'', 116),
        (N'lw-cw-117', @ProjectId, 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF window boards (£10/m supply)', N'm', 36.0000, 36.0000, 1296.0000, N'', 117),
        (N'lw-cw-118', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 380.0000, 22.0000, 8360.0000, N'', 118),
        (N'lw-cw-119', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Ditto walls', N'm2', 576.0000, 20.0000, 11520.0000, N'', 119),
        (N'lw-cw-120', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'WDR-TIM', N'Prepare & decorate doors', N'm2', 128.0000, 34.0000, 4352.0000, N'', 120),
        (N'lw-cw-121', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'CARP-2FX', N'Frames, architrave, window board & skirtings', N'm', 502.0000, 9.0000, 4518.0000, N'', 121),
        (N'lw-cw-122', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'INT-RDR', N'Decorate existing render', N'item', 1.0000, 3000.0000, 3000.0000, N'', 122),
        (N'lw-cw-123', @ProjectId, 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-LVT', N'Karndean vinyl flooring (£40 supply)', N'm2', 198.0000, 125.0000, 24750.0000, N'', 123),
        (N'lw-cw-124', @ProjectId, 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-LVT', N'Altro vinyl flooring (£40 supply)', N'm2', 36.0000, 125.0000, 4500.0000, N'', 124),
        (N'lw-cw-125', @ProjectId, 0, N'M51', N'Carpet', N'', N'', 0, N'FLR-CPT', N'Underlay & carpet (£25 supply)', N'm2', 140.0000, 50.0000, 7000.0000, N'', 125),
        (N'lw-cw-126', @ProjectId, 0, N'Q22', N'Ashpalt roads/pavings', N'', N'', 0, N'EXTW-PAV', N'Make good existing external areas - Provisional area', N'm2', 10.0000, 50.0000, 500.0000, N'', 126),
        (N'lw-cw-127', @ProjectId, 0, N'Q22', N'Ashpalt roads/pavings', N'', N'', 0, N'EXTW-PAV', N'New gravel to driveway - 50 mm thick - Provisional area', N'm2', 50.0000, 45.0000, 2250.0000, N'', 127),
        (N'lw-cw-128', @ProjectId, 0, N'Q22', N'Ashpalt roads/pavings', N'', N'', 0, N'EXTW-PAV', N'Form external ramps', N'item', 1.0000, 2000.0000, 2000.0000, N'', 128),
        (N'lw-cw-129', @ProjectId, 0, N'Q22', N'Ashpalt roads/pavings', N'', N'', 0, N'SUB-CON', N'Excavate & concretein foundation to retaining wall', N'm', 26.0000, 190.0000, 4940.0000, N'', 129),
        (N'lw-cw-130', @ProjectId, 0, N'Q22', N'Ashpalt roads/pavings', N'', N'', 0, N'MASON-BRK', N'215 mm hollow blockwork wall with copping retaining wall', N'm2', 22.0000, 172.0000, 3784.0000, N'', 130),
        (N'lw-cw-131', @ProjectId, 0, N'Q22', N'Ashpalt roads/pavings', N'', N'', 0, N'MASON-BRK', N'Blockwork with render & copping retaining wall', N'm2', 22.0000, 320.0000, 7040.0000, N'', 131),
        (N'lw-cw-132', @ProjectId, 0, N'Q22', N'Ashpalt roads/pavings', N'', N'', 0, N'SUB-GWK', N'Infill between walls', N'item', 1.0000, 1500.0000, 1500.0000, N'', 132),
        (N'lw-cw-133', @ProjectId, 0, N'Q22', N'Ashpalt roads/pavings', N'', N'', 0, N'EXTW-PAV', N'Sub base & flag stone paving slabs to external areas - Provisional area', N'm2', 100.0000, 175.0000, 17500.0000, N'', 133),
        (N'lw-ps-01', @ProjectId, 1, N'PC01', N'Provisional Sums', N'', N'', 1, N'SUP-TIL', N'Wall tiling (BASED ON 212M2 @ £80.00 / M2)', N'item', 1.0000, 12350.0000, 12350.0000, N'', 1),
        (N'lw-ps-02', @ProjectId, 1, N'PC02', N'Provisional Sums', N'', N'', 1, N'SUP-IRO', N'Internal Door ironmongery', N'item', 1.0000, 5180.0000, 5180.0000, N'', 2),
        (N'lw-ps-03', @ProjectId, 1, N'PC07', N'Provisional Sums', N'', N'', 1, N'SUP-KIT', N'New Kitchen', N'item', 1.0000, 23250.0000, 23250.0000, N'', 3),
        (N'lw-ps-04', @ProjectId, 1, N'PC08', N'Provisional Sums', N'', N'', 1, N'SUP-KIT', N'Utility', N'item', 1.0000, 9000.0000, 9000.0000, N'', 4),
        (N'lw-ps-05', @ProjectId, 1, N'PC10', N'Provisional Sums', N'', N'', 1, N'MEC-DRN', N'Repairs to below ground drainage', N'item', 1.0000, 7000.0000, 7000.0000, N'', 5),
        (N'lw-ps-06', @ProjectId, 1, N'PC11', N'Provisional Sums', N'', N'', 1, N'WIN-BLD', N'Window coverings', N'item', 1.0000, 18500.0000, 18500.0000, N'', 6),
        (N'lw-ps-07', @ProjectId, 1, N'PC12', N'Provisional Sums', N'', N'', 1, N'ELE-ALM', N'Intruder Alarm', N'item', 1.0000, 5500.0000, 5500.0000, N'', 7),
        (N'lw-ps-08', @ProjectId, 1, N'PC13', N'Provisional Sums', N'', N'', 1, N'ELE-FIR', N'Fire and smoke alarm', N'item', 1.0000, 5500.0000, 5500.0000, N'', 8),
        (N'lw-ps-09', @ProjectId, 1, N'PC14', N'Provisional Sums', N'', N'', 1, N'CARP-WRD', N'Wardrobes', N'item', 1.0000, 12800.0000, 12800.0000, N'', 9),
        (N'lw-ps-10', @ProjectId, 1, N'PC16', N'Provisional Sums', N'', N'', 1, N'EXTW-LND', N'Soft Landscape', N'item', 1.0000, 15000.0000, 15000.0000, N'', 10),
        (N'lw-ps-11', @ProjectId, 1, N'PC18', N'Provisional Sums', N'', N'', 1, N'WPF-DMP', N'Damp repairs as appednix 7', N'item', 1.0000, 1200.0000, 1200.0000, N'', 11),
        (N'lw-ps-12', @ProjectId, 1, N'PC21', N'Provisional Sums', N'', N'', 1, N'UTIL-STD', N'Installation of 3 Phase new Supply', N'item', 1.0000, 25000.0000, 25000.0000, N'', 12),
        (N'lw-cont-01', @ProjectId, 2, N'', N'Contingency', N'', N'', 0, N'HAND-MSC', N'Contingency Budget', N'item', 1.0000, 50000.0000, 50000.0000, N'', 1)
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

    PRINT 'Longwood (Horsham Road, Cranleigh): valuation lines merged.';

    -- Sanity check: the three seeded blocks should reconcile to the workbook.
    SELECT
        SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 575384.00
        SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         -- 140280.00
        SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --  50000.00
        SUM(LineAmount) AS ContractSum                                               -- 765664.00
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType IN (0, 1, 2)
      AND LineType NOT IN (3, 4);

    COMMIT TRAN;
END
GO
