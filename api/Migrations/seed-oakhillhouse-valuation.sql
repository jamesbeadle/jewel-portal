-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed,
-- per JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: Oakhill House Godalming -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : Oakhill House, Station Lane, Godalming, Surrey, GU8 5AN
-- ProjectId: resolved at run time by site-name matcher 'oakhillhousegodalming'
--
-- Seeds the CONTRACT SCOPE only, taken from the "Oakhill House Val 7"
-- workbook (Valuation 6 / 6 Month Defects, scope REV3 07.12.23). A single
-- Contract Works block makes up the Contract Sum; the workbook's two
-- provisional sections -- "Glazing - Provisional allowance" (GBP 16,375.00)
-- and "Plumbing installation - All provisional" (GBP 17,507.00) -- sit inline
-- in the bill (ElementType 0, LineType 1) rather than in a separate PC block:
--
--     Contract works (incl. GBP 33,882.00 inline provisional sums)
--     Contract Sum      GBP 102,883.00
--
-- Variations (V01..V12, net GBP 31,804.00) are NOT seeded here -- they belong
-- in seed-oakhillhouse-variations.sql. Per-valuation claim history
-- (Valuation 1..7, retention & release) is claim data
-- (ValuationClaims/ClaimLines), not bill structure.
--
--     Contract Sum            GBP 102,883.00
--     Net Variations          GBP  31,804.00
--     ------------------------------------------
--     Revised Contract Sum    GBP 134,687.00
--
-- The workbook has no NRM2 numbering (its own 0001..0024 codes repeat across
-- headings), so SectionCode is assigned sequentially (01..13) in workbook
-- order, per the Ravenswood pattern. CostCode maps each line to the Jewel
-- cost-centre master (seed-cost-centers.sql).
--
-- SKIPPED workbook rows (carry no contract value):
--   * "Finishes" section, "Fix only - Stone floor tiles" (54 m2 @ 80.00) --
--     no amount extended in the Contract Sum column.
--   * "Finishes" section, "Fix only - Timber flooring" (22 m2 @ 42.00) --
--     no amount extended in the Contract Sum column.
--   The Finishes heading therefore seeds no lines and takes no SectionCode.
--
-- "Omit item Vnn" comments are informational: those lines are omitted by
-- variations in the register, so they stay Priced/ProvisionalSum here and the
-- omission's value lives in the variations seed as part of that VO's net.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (oh-cw-NNN). A re-run
-- refreshes every field via MERGE. Variation lines for this project are left
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
    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
        (N'oh-cw-001', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site manager', N'week', 12.0000, 395.0000, 4740.0000, N'', 1),
        (N'oh-cw-002', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Rubbish removal', N'week', 6.0000, 380.0000, 2280.0000, N'', 2),
        (N'oh-cw-003', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-PRO', N'General protection', N'item', 1.0000, 500.0000, 500.0000, N'', 3),
        (N'oh-cw-004', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WC', N'Temporary toilet', N'week', 12.0000, 90.0000, 1080.0000, N'', 4),
        (N'oh-cw-005', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-HSC', N'CDM', N'item', 1.0000, 1200.0000, 1200.0000, N'', 5),
        (N'oh-cw-006', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'item', 12.0000, 125.0000, 1500.0000, N'', 6),
        (N'oh-cw-007', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Clean on completion', N'item', 1.0000, 350.0000, 350.0000, N'', 7),
        (N'oh-cw-008', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Isolate electrical installation in connection with demolitions & strip out as required', N'item', 1.0000, 250.0000, 250.0000, N'', 8),
        (N'oh-cw-009', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Ditto plumbing & heating installation', N'item', 1.0000, 300.0000, 300.0000, N'', 9),
        (N'oh-cw-010', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove & set aside boiler & HWC', N'item', 1.0000, 350.0000, 350.0000, N'', 10),
        (N'oh-cw-011', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove flooring, tiles, skirtings, picture rail, cove, fittings etc.', N'item', 1.0000, 175.0000, 175.0000, N'', 11),
        (N'oh-cw-012', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove kitchen & utility units, worktops & appliances', N'item', 1.0000, 280.0000, 280.0000, N'', 12),
        (N'oh-cw-013', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove internal doors & frames', N'nr', 7.0000, 18.0000, 126.0000, N'', 13),
        (N'oh-cw-014', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal walls / nibs to form new layout', N'm2', 20.0000, 36.0000, 720.0000, N'', 14),
        (N'oh-cw-015', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove existing staircase', N'item', 1.0000, 275.0000, 275.0000, N'', 15),
        (N'oh-cw-016', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove plasterboard ceilings to associated areas', N'm2', 72.0000, 12.0000, 864.0000, N'', 16),
        (N'oh-cw-017', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-STS', N'Erect temporary propping to existing construction', N'm', 10.0000, 80.0000, 800.0000, N'', 17),
        (N'oh-cw-018', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Demolish external cavity walls to form new lay out', N'm2', 8.0000, 95.0000, 760.0000, N'', 18),
        (N'oh-cw-019', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Break up existing floor screed ready for new UFH', N'm2', 76.0000, 24.0000, 1824.0000, N'', 19),
        (N'oh-cw-020', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Break up areas of existing concrete floor slab', N'm2', 6.0000, 44.0000, 264.0000, N'', 20),
        (N'oh-cw-021', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'EXTW-LND', N'Carefully adapt existing planting as required', N'item', 1.0000, 150.0000, 150.0000, N'', 21),
        (N'oh-cw-022', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove existing patio slabs', N'm2', 16.0000, 10.0000, 160.0000, N'', 22),
        (N'oh-cw-023', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-EXC', N'Excavate to reduce levels & remove spoil', N'm3', 2.0000, 155.0000, 310.0000, N'', 23),
        (N'oh-cw-024', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-EXC', N'Excavate pad foundations & remove spoil', N'm3', 2.0000, 220.0000, 440.0000, N'', 24),
        (N'oh-cw-025', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'Concrete in foundations', N'm3', 2.0000, 245.0000, 490.0000, N'', 25),
        (N'oh-cw-026', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'Dowel into existing foundation', N'item', 1.0000, 200.0000, 200.0000, N'', 26),
        (N'oh-cw-027', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'WPF-DMP', N'Cavity walls below dpc in two skins of engineering brickwork & lean mix cavity gill', N'm2', 3.0000, 236.0000, 708.0000, N'', 27),
        (N'oh-cw-028', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'WPF-DMP', N'Damp proof course', N'm', 9.0000, 16.0000, 144.0000, N'', 28),
        (N'oh-cw-029', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'WPF-DMP', N'Liquid DPM to existing areas', N'm2', 58.0000, 28.0000, 1624.0000, N'', 29),
        (N'oh-cw-030', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'150 mm hardcore blinded with sand', N'm2', 6.0000, 42.0000, 252.0000, N'Omit item V03', 30),
        (N'oh-cw-031', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'150 mm bed of concrete', N'm2', 6.0000, 80.0000, 480.0000, N'Omit item V03', 31),
        (N'oh-cw-032', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'A393 mesh to floor slab', N'm2', 6.0000, 38.0000, 228.0000, N'Omit item V03', 32),
        (N'oh-cw-033', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'WPF-DMP', N'Damp proof membranes 1200 g', N'm2', 6.0000, 18.0000, 108.0000, N'Omit item V03', 33),
        (N'oh-cw-034', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'INT-INF', N'100 mm Celotex floor insulation', N'm2', 6.0000, 38.0000, 228.0000, N'', 34),
        (N'oh-cw-035', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'FLR-SCR', N'65 mm sand / cement floor screed with mesh', N'm2', 58.0000, 72.0000, 4176.0000, N'', 35),
        (N'oh-cw-036', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 1500.0000, 1500.0000, N'', 36),
        (N'oh-cw-037', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'MEC-DRN', N'Connect into existing drainage run', N'item', 1.0000, 300.0000, 300.0000, N'', 37),
        (N'oh-cw-038', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'SUB-CON', N'New sub base to patio area', N'm2', 16.0000, 44.0000, 704.0000, N'', 38),
        (N'oh-cw-039', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'EXTW-PAV', N'Fix only - Stone floor tiles', N'm2', 16.0000, 80.0000, 1280.0000, N'', 39),
        (N'oh-cw-040', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'EXTW-PAV', N'Make good damaged areas', N'item', 1.0000, 250.0000, 250.0000, N'', 40),
        (N'oh-cw-041', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Single skin of matching facing brickwork', N'm2', 3.0000, 108.0000, 324.0000, N'', 41),
        (N'oh-cw-042', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Separate cost for supply of facing bricks (£2.50 each)', N'm2', 3.0000, 150.0000, 450.0000, N'', 42),
        (N'oh-cw-043', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Make good upto existing first floor', N'item', 1.0000, 400.0000, 400.0000, N'', 43),
        (N'oh-cw-044', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Movement joints, toothing, wall ties, etc', N'item', 1.0000, 250.0000, 250.0000, N'', 44),
        (N'oh-cw-045', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'IG L11 lintel - L1', N'm', 1.0000, 168.0000, 168.0000, N'', 45),
        (N'oh-cw-046', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Naylor ER7 lintel - L2', N'nr', 3.0000, 155.0000, 465.0000, N'', 46),
        (N'oh-cw-047', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'300 x 100 x 10 RHS steel beam - B1', N'kg', 480.0000, 7.0000, 3360.0000, N'', 47),
        (N'oh-cw-048', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'152 x 152 x 23 kg steel beam - B2', N'kg', 50.0000, 7.0000, 350.0000, N'', 48),
        (N'oh-cw-049', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'152 x 152 x 23 kg steel column - C1', N'kg', 70.0000, 7.0000, 490.0000, N'', 49),
        (N'oh-cw-050', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'100 x 100 x 10 SHS steel column - C2', N'kg', 55.0000, 7.0000, 385.0000, N'', 50),
        (N'oh-cw-051', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'Base plate & hold down brackets', N'nr', 2.0000, 60.0000, 120.0000, N'', 51),
        (N'oh-cw-052', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'SUB-CON', N'Cut out & cast concrete padstones', N'nr', 3.0000, 95.0000, 285.0000, N'', 52),
        (N'oh-cw-053', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'FIRE-PSV', N'Fireline protection to steels', N'item', 1.0000, 300.0000, 300.0000, N'', 53),
        (N'oh-cw-054', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Timber wall plate bolted to wall', N'm', 4.0000, 40.0000, 160.0000, N'', 54),
        (N'oh-cw-055', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'50 x 150 mm timber floor joists (stairs infill)', N'm2', 2.0000, 116.0000, 232.0000, N'', 55),
        (N'oh-cw-056', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'25 mm T & G chipboard flooring (stairs infill)', N'm2', 2.0000, 38.0000, 76.0000, N'', 56),
        (N'oh-cw-057', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'50 x 125 mm timber internal stud walls (to external walls)', N'm2', 3.0000, 88.0000, 264.0000, N'', 57),
        (N'oh-cw-058', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'50 x 100 mm timber internal stud walls', N'm2', 6.0000, 68.0000, 408.0000, N'', 58),
        (N'oh-cw-059', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'Extra for forming pocket doors', N'item', 1.0000, 100.0000, 100.0000, N'', 59),
        (N'oh-cw-060', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-DOR', N'Internal door lining, door & ironmongery (£200 supply)', N'nr', 2.0000, 445.0000, 890.0000, N'ID03 & ID07', 60),
        (N'oh-cw-061', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-DOR', N'Internal door lining, pocket door & ironmongery', N'nr', 1.0000, 1150.0000, 1150.0000, N'ID01', 61),
        (N'oh-cw-062', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-DOR', N'Internal door lining, glazed door & ironmongery', N'nr', 2.0000, 650.0000, 1300.0000, N'ID02', 62),
        (N'oh-cw-063', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-DOR', N'Internal door lining, glazed double door & ironmongery', N'nr', 1.0000, 1125.0000, 1125.0000, N'Omit item V04', 63),
        (N'oh-cw-064', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'MDF architraves to new door frames', N'm', 60.0000, 12.0000, 720.0000, N'', 64),
        (N'oh-cw-065', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'MDF skirting to walls to new walls (£8/m supply)', N'm', 48.0000, 26.0000, 1248.0000, N'', 65),
        (N'oh-cw-066', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'MDF window boards', N'm', 8.0000, 36.0000, 288.0000, N'', 66),
        (N'oh-cw-067', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-1FX', N'Plywood boxing to internal pipework', N'item', 1.0000, 250.0000, 250.0000, N'', 67),
        (N'oh-cw-068', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-JNR', N'New shelving to stud', N'item', 1.0000, 800.0000, 800.0000, N'Omit item V07', 68),
        (N'oh-cw-069', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-KIT', N'Fix only - Refit  Kitchen units, worktops & appliances', N'item', 1.0000, 2000.0000, 2000.0000, N'Omit Item V06', 69),
        (N'oh-cw-070', @ProjectId, 0, N'08', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-DOR', N'Internal door lining, Crittall door & ironmongery (£400 supply)', N'nr', 1.0000, 645.0000, 645.0000, N'Omit item V04', 70),
        (N'oh-cw-071', @ProjectId, 0, N'09', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-ALU', N'900 x 2000 mm black powder coated entrance door', N'nr', 1.0000, 1710.0000, 1710.0000, N'Omit item V04', 71),
        (N'oh-cw-072', @ProjectId, 0, N'09', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-ALU', N'1500 x 2000 mm black powder coated French doors', N'nr', 1.0000, 2950.0000, 2950.0000, N'Omit item V04', 72),
        (N'oh-cw-073', @ProjectId, 0, N'09', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-ALU', N'1450 x 2000 mm black powder coated window', N'nr', 3.0000, 2175.0000, 6525.0000, N'Omit item V04', 73),
        (N'oh-cw-074', @ProjectId, 0, N'09', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-ALU', N'713 x 2000 mm black powder coated window', N'nr', 2.0000, 2140.0000, 4280.0000, N'Omit item V04', 74),
        (N'oh-cw-075', @ProjectId, 0, N'09', N'Glazing - Provisional allowance', N'', N'', 1, N'WDR-ALU', N'300 x 2000 mm black powder coated window', N'nr', 2.0000, 455.0000, 910.0000, N'Omit item V04', 75),
        (N'oh-cw-076', @ProjectId, 0, N'10', N'Insulation & Plastering', N'', N'', 0, N'INT-INC', N'100 mm mineral insulation above ceiling joists', N'm2', 54.0000, 28.0000, 1512.0000, N'', 76),
        (N'oh-cw-077', @ProjectId, 0, N'10', N'Insulation & Plastering', N'', N'', 0, N'INT-PLB', N'12.5mm plasterboard to ceilings', N'm2', 54.0000, 20.0000, 1080.0000, N'', 77),
        (N'oh-cw-078', @ProjectId, 0, N'10', N'Insulation & Plastering', N'', N'', 0, N'INT-PLS', N'3 mm skim to ceilings', N'm2', 54.0000, 18.0000, 972.0000, N'', 78),
        (N'oh-cw-079', @ProjectId, 0, N'10', N'Insulation & Plastering', N'', N'', 0, N'INT-INW', N'120 mm Kingspan insulation to stud walls (external walls)', N'm2', 3.0000, 40.0000, 120.0000, N'', 79),
        (N'oh-cw-080', @ProjectId, 0, N'10', N'Insulation & Plastering', N'', N'', 0, N'INT-PLB', N'62.5 mm Kingspan plasterboard to stud walls', N'm2', 3.0000, 42.0000, 126.0000, N'', 80),
        (N'oh-cw-081', @ProjectId, 0, N'10', N'Insulation & Plastering', N'', N'', 0, N'INT-INW', N'50 mm mineral insulation between stud walls', N'm2', 6.0000, 15.0000, 90.0000, N'', 81),
        (N'oh-cw-082', @ProjectId, 0, N'10', N'Insulation & Plastering', N'', N'', 0, N'INT-PLB', N'12.5 mm plasterboard to studs', N'm2', 12.0000, 18.0000, 216.0000, N'', 82),
        (N'oh-cw-083', @ProjectId, 0, N'10', N'Insulation & Plastering', N'', N'', 0, N'INT-PLS', N'3 mm skim to walls', N'm2', 15.0000, 18.0000, 270.0000, N'', 83),
        (N'oh-cw-084', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'New consumer unit', N'nr', 1.0000, 975.0000, 975.0000, N'', 84),
        (N'oh-cw-085', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Double socket outlet', N'nr', 14.0000, 110.0000, 1540.0000, N'', 85),
        (N'oh-cw-086', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Thermostats', N'nr', 3.0000, 320.0000, 960.0000, N'', 86),
        (N'oh-cw-087', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Recessed light fittings', N'nr', 34.0000, 105.0000, 3570.0000, N'', 87),
        (N'oh-cw-088', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'External lights', N'nr', 4.0000, 145.0000, 580.0000, N'', 88),
        (N'oh-cw-089', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 7.0000, 38.0000, 266.0000, N'', 89),
        (N'oh-cw-090', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'MEC-VNT', N'Extractor fans', N'nr', 2.0000, 275.0000, 550.0000, N'', 90),
        (N'oh-cw-091', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Fan isolator switches', N'nr', 2.0000, 100.0000, 200.0000, N'', 91),
        (N'oh-cw-092', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Smoke/heat detector', N'nr', 1.0000, 125.0000, 125.0000, N'', 92),
        (N'oh-cw-093', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Fix only - Ring door bell', N'nr', 1.0000, 200.0000, 200.0000, N'', 93),
        (N'oh-cw-094', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Builders work in connection with electrical installation', N'item', 1.0000, 500.0000, 500.0000, N'', 94),
        (N'oh-cw-095', @ProjectId, 0, N'12', N'Plumbing installation - All provisional', N'', N'', 1, N'MEC-BLR', N'New central heating boiler & associated work', N'item', 1.0000, 4025.0000, 4025.0000, N'Provisional Sum', 95),
        (N'oh-cw-096', @ProjectId, 0, N'12', N'Plumbing installation - All provisional', N'', N'', 1, N'MEC-PLM', N'Relocate existing HWC', N'item', 1.0000, 750.0000, 750.0000, N'Provisional Sum', 96),
        (N'oh-cw-097', @ProjectId, 0, N'12', N'Plumbing installation - All provisional', N'', N'', 1, N'MEC-UFH', N'18 mm overlay wet underfloor heating', N'm2', 58.0000, 178.0000, 10324.0000, N'Provisional Sum', 97),
        (N'oh-cw-098', @ProjectId, 0, N'12', N'Plumbing installation - All provisional', N'', N'', 1, N'MEC-PLM', N'Radiator with TRVs', N'nr', 3.0000, 455.0000, 1365.0000, N'Provisional Sum', 98),
        (N'oh-cw-099', @ProjectId, 0, N'12', N'Plumbing installation - All provisional', N'', N'', 1, N'MEC-DRN', N'Stub stack & SVP', N'nr', 1.0000, 125.0000, 125.0000, N'Provisional Sum', 99),
        (N'oh-cw-100', @ProjectId, 0, N'12', N'Plumbing installation - All provisional', N'', N'', 1, N'MEC-PLM', N'Hot & cold supplies to utility', N'nr', 2.0000, 165.0000, 330.0000, N'Provisional Sum', 100),
        (N'oh-cw-101', @ProjectId, 0, N'12', N'Plumbing installation - All provisional', N'', N'', 1, N'MEC-DRN', N'Wastes to ditto', N'nr', 1.0000, 88.0000, 88.0000, N'Provisional Sum', 101),
        (N'oh-cw-102', @ProjectId, 0, N'12', N'Plumbing installation - All provisional', N'', N'', 1, N'MEC-PLM', N'Builders work in connection with plumbing & heating installation', N'item', 1.0000, 500.0000, 500.0000, N'Provisional Sum', 102),
        (N'oh-cw-103', @ProjectId, 0, N'13', N'Decorations', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 76.0000, 16.0000, 1216.0000, N'', 103),
        (N'oh-cw-104', @ProjectId, 0, N'13', N'Decorations', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to walls', N'm2', 136.0000, 14.0000, 1904.0000, N'', 104),
        (N'oh-cw-105', @ProjectId, 0, N'13', N'Decorations', N'', N'', 0, N'CARP-2FX', N'Frames, architrave, window board & skirtings', N'm', 82.0000, 8.0000, 656.0000, N'', 105)
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
    PRINT 'Oakhill House Godalming: valuation lines merged.';
    COMMIT TRAN;
END
GO

-- Sanity check: the seeded block should reconcile to the workbook.
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'oakhillhousegodalming'
       OR LOWER(REPLACE(Name, ' ', '')) = 'oakhillhousegodalming'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'oakhillhousegodalming' THEN 0 ELSE 1 END);
SELECT
    SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 102883.00 (incl. 33882.00 inline PS)
    SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         --      0.00 (none - PS lines are inline)
    SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --      0.00 (none)
    SUM(LineAmount) AS ContractSum                                               -- 102883.00
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = @ProjectId AND ElementType IN (0, 1, 2)
  AND LineType NOT IN (3, 4);
GO
