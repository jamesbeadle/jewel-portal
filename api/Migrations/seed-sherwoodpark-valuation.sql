-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per
-- JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: 24 Sherwood Park -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : 24 Sherwood Park Road, Sutton SM1 2SQ
-- ProjectId: resolved at run time by site-name matcher '24sherwoodparksm12sq'
--
-- Seeds the ORIGINAL contract scope only, taken from the "Valuation 17"
-- workbook. Two blocks make up the Contract Sum (there is NO contingency
-- block on this project):
--
--     Contract works    GBP 436,408.00
--     Provisional Sums  GBP 126,730.00
--     ----------------------------------
--     Contract Sum      GBP 563,138.00
--
-- Variations (V01..V86, net GBP 97,518.74) are NOT seeded here -- they belong
-- in seed-sherwoodpark-variations.sql. Per-valuation claim history
-- (Valuation 01..17, retention) is claim data, not bill structure.
--
-- SectionCode/SectionName retain the workbook's NRM-style references; PS lines
-- retain their PC codes (PC01..PC16). The inline "PC6 Supply of sanitaryware"
-- line inside N13 is an inline provisional sum (ElementType 0, LineType 1),
-- as is the G12 structural steels PS line (rate given as 'PS'; Quantity 1,
-- Rate = amount).
--
-- SKIPPED workbook rows (zero value, no contract sum):
--   * D20 "Excavate to reduce levels & remove spoil : to new areas" -- 0.00,
--     "Missing from the Tender" (re-appears as V07, TBC / never valued)
--   * D20 "Excavate foundations 600 x 1000 mm & remove spoil"       -- ditto
--   * D20 "Concrete in foundations"                                 -- ditto
--
-- "Omit item Vnn" comments are informational: those lines are omitted by
-- variations in the register, so they stay Priced/ProvisionalSum here.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (sp-cw-NNN /
-- sp-ps-NN). A re-run refreshes every field via MERGE; rows of other projects
-- are never touched (no BY SOURCE clause). Safe to run repeatedly.
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
    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
        (N'sp-cw-001', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'SCAFF-STD', N'Scaffolding', N'm2', 220.0000, 34.0000, 7480.0000, N'', 1),
        (N'sp-cw-002', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WPR', N'Temporary covering / weather protection', N'item', 1.0000, 750.0000, 750.0000, N'', 2),
        (N'sp-cw-003', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site Supervision', N'week', 40.0000, 1250.0000, 50000.0000, N'', 3),
        (N'sp-cw-004', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Rubbish removal', N'week', 40.0000, 345.0000, 13800.0000, N'', 4),
        (N'sp-cw-005', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-PRO', N'General protection', N'item', 1.0000, 600.0000, 600.0000, N'', 5),
        (N'sp-cw-006', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WC', N'Temporary toilet', N'week', 40.0000, 90.0000, 3600.0000, N'', 6),
        (N'sp-cw-007', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'ELE-STD', N'Temporary plumbing & electrics', N'item', 1.0000, 1500.0000, 1500.0000, N'', 7),
        (N'sp-cw-008', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-HSC', N'CDM', N'item', 1.0000, 1200.0000, 1200.0000, N'', 8),
        (N'sp-cw-009', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'item', 40.0000, 125.0000, 5000.0000, N'', 9),
        (N'sp-cw-010', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Clean on completion', N'item', 1.0000, 500.0000, 500.0000, N'', 10),
        (N'sp-cw-011', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Isolate electrical installation in connection with demolitions & strip out as required', N'item', 1.0000, 750.0000, 750.0000, N'', 11),
        (N'sp-cw-012', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Ditto plumbing & heating installation', N'item', 1.0000, 800.0000, 800.0000, N'', 12),
        (N'sp-cw-013', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove flooring, tiles, skirtings, picture rail, cove, fittings etc.', N'item', 1.0000, 600.0000, 600.0000, N'', 13),
        (N'sp-cw-014', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove kitchen & utility units, worktops & appliances', N'item', 1.0000, 255.0000, 255.0000, N'', 14),
        (N'sp-cw-015', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove sanitary items from WCs & bathrooms', N'item', 1.0000, 500.0000, 500.0000, N'', 15),
        (N'sp-cw-016', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove internal doors & frames', N'nr', 13.0000, 22.0000, 286.0000, N'', 16),
        (N'sp-cw-017', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal single skin walls', N'm2', 26.0000, 44.0000, 1144.0000, N'', 17),
        (N'sp-cw-018', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove plasterboard ceiling throughout', N'm2', 154.0000, 14.0000, 2156.0000, N'', 18),
        (N'sp-cw-019', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Cut opening in first floor ceiling for new staircase to loft', N'nr', 1.0000, 245.0000, 245.0000, N'Omit Item V01', 19),
        (N'sp-cw-020', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove windows & external doors as required', N'item', 1.0000, 420.0000, 420.0000, N'', 20),
        (N'sp-cw-021', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-STS', N'Errect temporary propping to existing construction', N'm', 12.0000, 80.0000, 960.0000, N'', 21),
        (N'sp-cw-022', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish external walls to form new layout', N'm2', 12.0000, 110.0000, 1320.0000, N'', 22),
        (N'sp-cw-023', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish chimney breasts & stacks', N'item', 1.0000, 3000.0000, 3000.0000, N'', 23),
        (N'sp-cw-024', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove roof to front, side extension & out building', N'm2', 86.0000, 24.0000, 2064.0000, N'', 24),
        (N'sp-cw-025', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove existing fascia / soffit & rainwater goods', N'item', 1.0000, 120.0000, 120.0000, N'', 25),
        (N'sp-cw-026', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish conservatory - complete', N'm2', 24.0000, 45.0000, 1080.0000, N'', 26),
        (N'sp-cw-027', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Form new openings to out building front elevation', N'item', 1.0000, 380.0000, 380.0000, N'', 27),
        (N'sp-cw-028', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove existing paving, shrubs, etc to areas of new work', N'm2', 40.0000, 12.0000, 480.0000, N'', 28),
        (N'sp-cw-029', @ProjectId, 0, N'D20', N'Excavation & filling', N'', N'', 0, N'SUB-CON', N'100 mm concrete oversite', N'm2', 60.0000, 24.0000, 1440.0000, N'', 29),
        (N'sp-cw-030', @ProjectId, 0, N'D20', N'Excavation & filling', N'', N'', 0, N'INT-INF', N'165 mm polystirene insulation', N'm2', 60.0000, 36.0000, 2160.0000, N'', 30),
        (N'sp-cw-031', @ProjectId, 0, N'D20', N'Excavation & filling', N'', N'', 0, N'SUB-CON', N'150 mm beam & block flooring', N'm2', 60.0000, 98.0000, 5880.0000, N'', 31),
        (N'sp-cw-032', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Stub stack & durgo valve', N'nr', 4.0000, 130.0000, 520.0000, N'', 32),
        (N'sp-cw-033', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Soil vent pipework', N'item', 1.0000, 750.0000, 750.0000, N'', 33),
        (N'sp-cw-034', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Wastes connections', N'nr', 16.0000, 88.0000, 1408.0000, N'', 34),
        (N'sp-cw-035', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 3500.0000, 3500.0000, N'', 35),
        (N'sp-cw-036', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'SUB-CON', N'Precast lintels over drainage runs', N'item', 1.0000, 400.0000, 400.0000, N'', 36),
        (N'sp-cw-037', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'New manhole', N'nr', 2.0000, 725.0000, 1450.0000, N'', 37),
        (N'sp-cw-038', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'EXTW-PAV', N'Aco slot drains', N'm', 11.0000, 132.0000, 1452.0000, N'', 38),
        (N'sp-cw-039', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 3000.0000, 3000.0000, N'', 39),
        (N'sp-cw-040', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'MEC-DRN', N'Make connection into existing runs', N'item', 1.0000, 250.0000, 250.0000, N'', 40),
        (N'sp-cw-041', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-DRN', N'New soakaway', N'nr', 1.0000, 1150.0000, 1150.0000, N'', 41),
        (N'sp-cw-042', @ProjectId, 0, N'C45', N'Damp proof course renewal/insertion', N'', N'', 0, N'WPF-DMP', N'Damp proof course to new walls', N'm2', 30.0000, 14.0000, 420.0000, N'Omit Item V10', 42),
        (N'sp-cw-043', @ProjectId, 0, N'F1', N'Masonry', N'', N'', 0, N'WPF-DMP', N'Cavity walls below dpc in engineering brickwork, 100 mm blockwork & lean mix cavity fill', N'm2', 10.0000, 198.0000, 1980.0000, N'', 43),
        (N'sp-cw-044', @ProjectId, 0, N'F1', N'Masonry', N'', N'', 0, N'MASON-BRK', N'Air vent bricks', N'item', 1.0000, 550.0000, 550.0000, N'', 44),
        (N'sp-cw-045', @ProjectId, 0, N'F1', N'Masonry', N'', N'', 0, N'INT-INW', N'Cavity wall in two skins of 100 mm blockwork with 60 mm Kingspan insulation to cavity', N'm2', 52.0000, 186.0000, 9672.0000, N'', 45),
        (N'sp-cw-046', @ProjectId, 0, N'F1', N'Masonry', N'', N'', 0, N'INT-INW', N'Cavity wall in matching facing brickwork, 60 mm Kingspan insulation & 100 mm blockwork internal skin', N'm2', 8.0000, 202.0000, 1616.0000, N'', 46),
        (N'sp-cw-047', @ProjectId, 0, N'F1', N'Masonry', N'', N'', 0, N'MASON-BRK', N'Separate cost for supply of facing bricks (£2.50 each)', N'm2', 8.0000, 150.0000, 1200.0000, N'', 47),
        (N'sp-cw-048', @ProjectId, 0, N'F1', N'Masonry', N'', N'', 0, N'MASON-BRK', N'Precast concrete copping to parapet wals', N'm', 13.0000, 92.0000, 1196.0000, N'', 48),
        (N'sp-cw-049', @ProjectId, 0, N'F1', N'Masonry', N'', N'', 0, N'MASON-BRK', N'Wall extension profiles', N'm', 20.0000, 32.0000, 640.0000, N'', 49),
        (N'sp-cw-050', @ProjectId, 0, N'F1', N'Masonry', N'', N'', 0, N'MASON-BRK', N'Thermabate cavity closers', N'm', 28.0000, 22.0000, 616.0000, N'', 50),
        (N'sp-cw-051', @ProjectId, 0, N'F1', N'Masonry', N'', N'', 0, N'MASON-BRK', N'IG L1/S lintel & tray over new openings', N'm', 8.0000, 122.0000, 976.0000, N'', 51),
        (N'sp-cw-052', @ProjectId, 0, N'G12', N'Isolated structural metal members', N'', N'', 1, N'STR-STL', N'Structural steels & associated works', N'item', 1.0000, 5000.0000, 5000.0000, N'Omit Item V09', 52),
        (N'sp-cw-053', @ProjectId, 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'CARP-1FX', N'Timber wall plate bolted to wall', N'm', 24.0000, 32.0000, 768.0000, N'', 53),
        (N'sp-cw-054', @ProjectId, 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'CARP-CUT', N'50 x 150 mm timber roof rafters', N'm', 224.0000, 30.0000, 6720.0000, N'', 54),
        (N'sp-cw-055', @ProjectId, 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'CARP-1FX', N'50 x 150 mm timber roof joists', N'm', 80.0000, 30.0000, 2400.0000, N'', 55),
        (N'sp-cw-056', @ProjectId, 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'CARP-1FX', N'Joist hangers', N'nr', 72.0000, 5.0000, 360.0000, N'', 56),
        (N'sp-cw-057', @ProjectId, 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'CARP-1FX', N'Galvanised restraint straps', N'nr', 32.0000, 16.0000, 512.0000, N'', 57),
        (N'sp-cw-058', @ProjectId, 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'ROOF-FLT', N'18 mm plywood over firings to flat roof deck', N'm2', 80.0000, 48.0000, 3840.0000, N'', 58),
        (N'sp-cw-059', @ProjectId, 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'CARP-KIT', N'Kitchenette to out building', N'item', 1.0000, 2000.0000, 2000.0000, N'Omit Item V31', 59),
        (N'sp-cw-060', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-FLT', N'Single ply flat roof membrane', N'm2', 80.0000, 120.0000, 9600.0000, N'', 60),
        (N'sp-cw-061', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-TLN', N'Breatherable membrane, battens & matching tiles', N'm2', 52.0000, 98.0000, 5096.0000, N'', 61),
        (N'sp-cw-062', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-TLN', N'Separate cost for supply of tiles (£1.20 each)', N'm2', 52.0000, 72.0000, 3744.0000, N'', 62),
        (N'sp-cw-063', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-LED', N'Lead flashing / valley lining', N'm', 26.0000, 56.0000, 1456.0000, N'', 63),
        (N'sp-cw-064', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-TLN', N'Ridge / hip tiles', N'm', 8.0000, 65.0000, 520.0000, N'', 64),
        (N'sp-cw-065', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-FSU', N'Fascia / soffit throughout', N'm', 62.0000, 46.0000, 2852.0000, N'', 65),
        (N'sp-cw-066', @ProjectId, 0, N'R10', N'Rainwater drainage systems', N'', N'', 0, N'ROOF-GRU', N'Hopper heads', N'nr', 1.0000, 130.0000, 130.0000, N'', 66),
        (N'sp-cw-067', @ProjectId, 0, N'R10', N'Rainwater drainage systems', N'', N'', 0, N'ROOF-GRU', N'Guttering', N'm', 82.0000, 32.0000, 2624.0000, N'', 67),
        (N'sp-cw-068', @ProjectId, 0, N'R10', N'Rainwater drainage systems', N'', N'', 0, N'ROOF-GRU', N'Rainwater pipework', N'm', 50.0000, 34.0000, 1700.0000, N'', 68),
        (N'sp-cw-069', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'CARP-1FX', N'38 x 89 mm timber internal stud walls', N'm2', 70.0000, 56.0000, 3920.0000, N'', 69),
        (N'sp-cw-070', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'CARP-1FX', N'18 mm OSB board as required to stud walls', N'm2', 34.0000, 28.0000, 952.0000, N'', 70),
        (N'sp-cw-071', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INC', N'120 mm Kingspan insulation to flat roof deck joists', N'm2', 80.0000, 38.0000, 3040.0000, N'', 71),
        (N'sp-cw-072', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INC', N'100 mm Kingspan insulation between roof rafters', N'm2', 52.0000, 36.0000, 1872.0000, N'', 72),
        (N'sp-cw-073', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INC', N'30 mm Kingspan insulation under rafters', N'm2', 52.0000, 22.0000, 1144.0000, N'', 73),
        (N'sp-cw-074', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-PLB', N'12.5mm plasterboard to ceilings (throughout)', N'm2', 268.0000, 18.0000, 4824.0000, N'', 74),
        (N'sp-cw-075', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'CARP-1FX', N'50 mm rockwool insulation between stud walls', N'm2', 70.0000, 16.0000, 1120.0000, N'', 75),
        (N'sp-cw-076', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-PLB', N'12.5 mm plasterboard to blockwork & studs', N'm2', 208.0000, 16.0000, 3328.0000, N'', 76),
        (N'sp-cw-077', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-UPV', N'1450 x 1050 mm upvc window - tonys bedroom', N'nr', 1.0000, 915.0000, 915.0000, N'', 77),
        (N'sp-cw-078', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-UPV', N'1000 x 1050 mm upvc window - WC', N'nr', 1.0000, 630.0000, 630.0000, N'', 78),
        (N'sp-cw-079', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-UPV', N'1810 x 1050 mm upvc window - kitchen', N'nr', 1.0000, 1140.0000, 1140.0000, N'', 79),
        (N'sp-cw-080', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-UPV', N'850 x 1050 mm upvc window - utility', N'nr', 1.0000, 535.0000, 535.0000, N'', 80),
        (N'sp-cw-081', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-UPV', N'1200 x 1350 mm upvc window - bathrom', N'nr', 1.0000, 980.0000, 980.0000, N'', 81),
        (N'sp-cw-082', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-UPV', N'990 x 1100 mm upvc window - wet room', N'nr', 1.0000, 580.0000, 580.0000, N'', 82),
        (N'sp-cw-083', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-UPV', N'600 x 1000 mm upvc window - kitchenette', N'nr', 1.0000, 440.0000, 440.0000, N'Omit Item V18', 83),
        (N'sp-cw-084', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-SPG', N'Velux 780 x 1400 mm roof lights - tonys bedroom', N'nr', 2.0000, 1220.0000, 2440.0000, N'Omit Item V15', 84),
        (N'sp-cw-085', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-SPG', N'Velux 780 x 980 mm roof lights - kitchen / dinner', N'nr', 4.0000, 988.0000, 3952.0000, N'Omit Item V15', 85),
        (N'sp-cw-086', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'ROOF-FLT', N'1500 x 750 mm flat roof lights', N'nr', 2.0000, 1450.0000, 2900.0000, N'Omit Item V15', 86),
        (N'sp-cw-087', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'WDR-TIM', N'Composite entrance door with side lights', N'nr', 1.0000, 2150.0000, 2150.0000, N'Omit Item V27', 87),
        (N'sp-cw-088', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'WDR-ALU', N'2600 x 2100 mm bifolding doors - out building', N'nr', 1.0000, 4200.0000, 4200.0000, N'', 88),
        (N'sp-cw-089', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'WDR-UPV', N'3835 x 2100 mm upvc French doors with side lights', N'nr', 1.0000, 6855.0000, 6855.0000, N'Omit Item V20', 89),
        (N'sp-cw-090', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'WDR-UPV', N'950 x 2100 mm upvc external door to utility room', N'nr', 1.0000, 1550.0000, 1550.0000, N'', 90),
        (N'sp-cw-091', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'SUP-DOR', N'926 mm Internal door lining & single door ( £120 supply )', N'nr', 11.0000, 325.0000, 3575.0000, N'', 91),
        (N'sp-cw-092', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'SUP-DOR', N'762 mm Internal door lining & single door (£100 supply)', N'nr', 7.0000, 305.0000, 2135.0000, N'', 92),
        (N'sp-cw-093', @ProjectId, 0, N'L30', N'Stairs/Ladders/walkways/handrails/balustrades', N'', N'', 0, N'STAIR-TIM', N'New softwood staircase to loft, newels, balustrade, etc', N'nr', 1.0000, 4755.0000, 4755.0000, N'Omit Item V01', 93),
        (N'sp-cw-094', @ProjectId, 0, N'L30', N'Stairs/Ladders/walkways/handrails/balustrades', N'', N'', 0, N'STAIR-TIM', N'Section of new balustrading to ground floor staircase', N'nr', 1.0000, 800.0000, 800.0000, N'Omit Item V34', 94),
        (N'sp-cw-095', @ProjectId, 0, N'L30', N'Stairs/Ladders/walkways/handrails/balustrades', N'', N'', 0, N'CARP-1FX', N'22 mm T&G chipboard flooring to loft', N'm2', 50.0000, 32.0000, 1600.0000, N'', 95),
        (N'sp-cw-096', @ProjectId, 0, N'M10', N'Cement based levelling screeds', N'', N'', 0, N'FLR-SCR', N'75 mm sand / cement floor screed', N'm2', 60.0000, 44.0000, 2640.0000, N'', 96),
        (N'sp-cw-097', @ProjectId, 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'EXT-STC', N'Matching render to blockwork (through colour)', N'm2', 52.0000, 75.0000, 3900.0000, N'', 97),
        (N'sp-cw-098', @ProjectId, 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-PLS', N'3 mm skim to ceilings', N'm2', 216.0000, 28.0000, 6048.0000, N'', 98),
        (N'sp-cw-099', @ProjectId, 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-PLS', N'3 mm skim to new & existing walls', N'm2', 382.0000, 26.0000, 9932.0000, N'', 99),
        (N'sp-cw-100', @ProjectId, 0, N'P10', N'Sundry insulation', N'', N'', 0, N'CARP-1FX', N'Plywood boxing & insulation to internal pipes', N'item', 1.0000, 600.0000, 600.0000, N'', 100),
        (N'sp-cw-101', @ProjectId, 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF architraves to new door frames', N'm', 190.0000, 12.0000, 2280.0000, N'', 101),
        (N'sp-cw-102', @ProjectId, 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF skirting to walls to new walls (£8/m supply)', N'm', 183.0000, 24.0000, 4392.0000, N'', 102),
        (N'sp-cw-103', @ProjectId, 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF window boards', N'm', 24.0000, 36.0000, 864.0000, N'', 103),
        (N'sp-cw-104', @ProjectId, 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'MEC-PLM', N'Hot & cold water supply', N'nr', 26.0000, 155.0000, 4030.0000, N'', 104),
        (N'sp-cw-105', @ProjectId, 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'MEC-PLM', N'Water softner', N'nr', 1.0000, 855.0000, 855.0000, N'', 105),
        (N'sp-cw-106', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-BLR', N'New central heating boiler & associated pipework', N'nr', 1.0000, 4450.0000, 4450.0000, N'', 106),
        (N'sp-cw-107', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'UTIL-STD', N'Relocate gas meter', N'nr', 1.0000, 750.0000, 750.0000, N'', 107),
        (N'sp-cw-108', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-PLM', N'Megaflor hot water cylinder', N'nr', 1.0000, 2980.0000, 2980.0000, N'', 108),
        (N'sp-cw-109', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-PLM', N'Radiators with TRVs', N'nr', 8.0000, 525.0000, 4200.0000, N'', 109),
        (N'sp-cw-110', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-PLM', N'Towel rails with TRVS', N'nr', 5.0000, 550.0000, 2750.0000, N'', 110),
        (N'sp-cw-111', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-UFH', N'Wet underfloor heating', N'm2', 58.0000, 128.0000, 7424.0000, N'', 111),
        (N'sp-cw-112', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-UFH', N'Manifold & thermostats', N'nr', 4.0000, 450.0000, 1800.0000, N'', 112),
        (N'sp-cw-113', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 1, N'SUP-SAN', N'Supply of sanitaryware', N'item', 1.0000, 14000.0000, 14000.0000, N'Omit Item V16', 113),
        (N'sp-cw-114', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-APP', N'Fix only - WC', N'nr', 5.0000, 278.0000, 1390.0000, N'', 114),
        (N'sp-cw-115', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-APP', N'Fix only - Wash hand basin', N'nr', 5.0000, 260.0000, 1300.0000, N'', 115),
        (N'sp-cw-116', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-APP', N'Fix only - Showers / baths', N'nr', 4.0000, 480.0000, 1920.0000, N'', 116),
        (N'sp-cw-117', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-APP', N'Fix only - Mirrors, towel rails, hooks, etc', N'item', 1.0000, 300.0000, 300.0000, N'', 117),
        (N'sp-cw-118', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'WPF-INT', N'Wet room system / tanking', N'item', 1.0000, 1200.0000, 1200.0000, N'', 118),
        (N'sp-cw-119', @ProjectId, 0, N'U90', N'General ventilation', N'', N'', 0, N'MEC-VNT', N'Extract fan', N'nr', 8.0000, 275.0000, 2200.0000, N'', 119),
        (N'sp-cw-120', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Consumer unit', N'nr', 1.0000, 1150.0000, 1150.0000, N'', 120),
        (N'sp-cw-121', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'UTIL-STD', N'Relocate electric meter', N'nr', 1.0000, 750.0000, 750.0000, N'', 121),
        (N'sp-cw-122', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Double socket outlet', N'nr', 40.0000, 118.0000, 4720.0000, N'', 122),
        (N'sp-cw-123', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'External double socket', N'nr', 1.0000, 120.0000, 120.0000, N'', 123),
        (N'sp-cw-124', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Fused spurs', N'nr', 8.0000, 100.0000, 800.0000, N'', 124),
        (N'sp-cw-125', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Cooker switch', N'nr', 1.0000, 120.0000, 120.0000, N'', 125),
        (N'sp-cw-126', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Shavers socket', N'nr', 4.0000, 95.0000, 380.0000, N'', 126),
        (N'sp-cw-127', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Recessed light fitting', N'nr', 82.0000, 108.0000, 8856.0000, N'', 127),
        (N'sp-cw-128', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Pendant lights', N'nr', 8.0000, 78.0000, 624.0000, N'', 128),
        (N'sp-cw-129', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'External lights', N'nr', 6.0000, 175.0000, 1050.0000, N'', 129),
        (N'sp-cw-130', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 26.0000, 40.0000, 1040.0000, N'', 130),
        (N'sp-cw-131', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Fan isolator switches', N'nr', 8.0000, 95.0000, 760.0000, N'', 131),
        (N'sp-cw-132', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Smoke/heat detector', N'nr', 4.0000, 150.0000, 600.0000, N'', 132),
        (N'sp-cw-133', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Carbon monoxide detector', N'nr', 2.0000, 118.0000, 236.0000, N'', 133),
        (N'sp-cw-134', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Builders work in connection with electrical installation', N'item', 1.0000, 500.0000, 500.0000, N'', 134),
        (N'sp-cw-135', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'MEC-SOL', N'PV solar panels', N'item', 1.0000, 3500.0000, 3500.0000, N'', 135),
        (N'sp-cw-136', @ProjectId, 0, N'W90', N'Communications & security systems', N'', N'', 0, N'PRELIMS-SEC', N'All assoisated works', N'nr', 1.0000, 3000.0000, 3000.0000, N'', 136),
        (N'sp-cw-137', @ProjectId, 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-SLF', N'Self leveling screed to existing ground floor', N'm2', 72.0000, 28.0000, 2016.0000, N'', 137),
        (N'sp-cw-138', @ProjectId, 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'CARP-1FX', N'9 mm plywood to existing first floor', N'm2', 72.0000, 18.0000, 1296.0000, N'', 138),
        (N'sp-cw-139', @ProjectId, 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-LVT', N'Karndean vinyl flooring (£40 supply)', N'm2', 148.0000, 95.0000, 14060.0000, N'', 139),
        (N'sp-cw-140', @ProjectId, 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-LVT', N'Altro vinyl flooring (£40 supply)', N'm2', 38.0000, 95.0000, 3610.0000, N'', 140),
        (N'sp-cw-141', @ProjectId, 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-CPT', N'Underlay & carpet (£40 supply)', N'm2', 62.0000, 60.0000, 3720.0000, N'Omit Item V38', 141),
        (N'sp-cw-142', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 264.0000, 18.0000, 4752.0000, N'', 142),
        (N'sp-cw-143', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Ditto walls', N'm2', 428.0000, 16.0000, 6848.0000, N'', 143),
        (N'sp-cw-144', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Prepare & decorate doors', N'm2', 62.0000, 32.0000, 1984.0000, N'', 144),
        (N'sp-cw-145', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Frames, architrave, window board & skirtings', N'm', 398.0000, 7.0000, 2786.0000, N'', 145),
        (N'sp-cw-146', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Prepare & decorate new staircase', N'item', 1.0000, 1200.0000, 1200.0000, N'Omit Item V34', 146),
        (N'sp-cw-147', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Prepare & decorate all external render', N'm2', 190.0000, 28.0000, 5320.0000, N'', 147),
        (N'sp-cw-148', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Prepare & decorate tudor timber', N'item', 1.0000, 450.0000, 450.0000, N'', 148),
        (N'sp-cw-149', @ProjectId, 0, N'Q20', N'Granular sub-bases to roads/ pavings', N'', N'', 0, N'EXTW-DEK', N'Form new raised deck to rear & forming ramp / path', N'item', 1.0000, 3000.0000, 3000.0000, N'', 149),
        (N'sp-cw-150', @ProjectId, 0, N'Q20', N'Granular sub-bases to roads/ pavings', N'', N'', 0, N'EXTW-PAV', N'Sub base & paving slabs to external areas', N'm2', 80.0000, 145.0000, 11600.0000, N'', 150),
        (N'sp-cw-151', @ProjectId, 0, N'Q20', N'Granular sub-bases to roads/ pavings', N'', N'', 0, N'EXTW-LND', N'External planters', N'item', 1.0000, 3500.0000, 3500.0000, N'Omit Item V28', 151),
        (N'sp-cw-152', @ProjectId, 0, N'Q20', N'Granular sub-bases to roads/ pavings', N'', N'', 0, N'EXTW-PAV', N'Alterations / relay to front driveway block paving', N'item', 1.0000, 750.0000, 750.0000, N'', 152),
        (N'sp-cw-153', @ProjectId, 0, N'Q50', N'Site/street furniture/equipment', N'', N'', 0, N'EXTW-FEN', N'Balustrading to rear patio (spec tbc)', N'm', 12.0000, 135.0000, 1620.0000, N'Omit Item V28', 153),
        (N'sp-cw-154', @ProjectId, 0, N'Q50', N'Site/street furniture/equipment', N'', N'', 0, N'EXTW-FEN', N'1m timber fencing to front garden', N'm', 11.0000, 85.0000, 935.0000, N'', 154),
        (N'sp-cw-155', @ProjectId, 0, N'Q50', N'Site/street furniture/equipment', N'', N'', 0, N'EXTW-FEN', N'6ft close board fencing', N'm', 12.0000, 140.0000, 1680.0000, N'Omit Item V24', 155),
        (N'sp-ps-01', @ProjectId, 1, N'PC06', N'Provisional Sums', N'', N'', 1, N'ENABLE-ASB', N'Asbestos removal', N'item', 1.0000, 5000.0000, 5000.0000, N'Omit Item V02', 1),
        (N'sp-ps-02', @ProjectId, 1, N'PC07', N'Provisional Sums', N'', N'', 1, N'MEC-DRN', N'Remedial and new drainage work', N'item', 1.0000, 10000.0000, 10000.0000, N'Omit Item V04', 2),
        (N'sp-ps-03', @ProjectId, 1, N'PC14', N'Provisional Sums', N'', N'', 1, N'STR-STL', N'Structural work', N'item', 1.0000, 15000.0000, 15000.0000, N'Omit Item V05', 3),
        (N'sp-ps-04', @ProjectId, 1, N'PC05', N'Provisional Sums', N'', N'', 1, N'ROOF-TLO', N'Works to existing roof', N'item', 1.0000, 6500.0000, 6500.0000, N'Omit Item V03', 4),
        (N'sp-ps-05', @ProjectId, 1, N'PC10', N'Provisional Sums', N'', N'', 1, N'ELE-ALM', N'Alarm System', N'item', 1.0000, 2200.0000, 2200.0000, N'Omit Item V22', 5),
        (N'sp-ps-06', @ProjectId, 1, N'PC11', N'Provisional Sums', N'', N'', 1, N'ELE-FIR', N'Fire & Smoke detection', N'item', 1.0000, 2950.0000, 2950.0000, N'Omit Item V21', 6),
        (N'sp-ps-07', @ProjectId, 1, N'PC01', N'Provisional Sums', N'', N'', 1, N'SUP-TIL', N'Wall tiling', N'item', 1.0000, 6200.0000, 6200.0000, N'Omit Item V37', 7),
        (N'sp-ps-08', @ProjectId, 1, N'PC02', N'Provisional Sums', N'', N'', 1, N'SUP-IRO', N'Internal door ironmongery', N'item', 1.0000, 1780.0000, 1780.0000, N'Omit Item V43', 8),
        (N'sp-ps-09', @ProjectId, 1, N'PC03', N'Provisional Sums', N'', N'', 1, N'SUP-KIT', N'New kitchen units & appliances', N'item', 1.0000, 26000.0000, 26000.0000, N'Omit item V19', 9),
        (N'sp-ps-10', @ProjectId, 1, N'PC04', N'Provisional Sums', N'', N'', 1, N'SUP-KIT', N'New utility units & appliances', N'item', 1.0000, 6500.0000, 6500.0000, N'Omit item V19', 10),
        (N'sp-ps-11', @ProjectId, 1, N'PC08', N'Provisional Sums', N'', N'', 1, N'WIN-BLD', N'Blinds & Curtains', N'item', 1.0000, 6500.0000, 6500.0000, N'Omit item V56', 11),
        (N'sp-ps-12', @ProjectId, 1, N'PC09', N'Provisional Sums', N'', N'', 1, N'CARP-WRD', N'Wardrobes & storage', N'item', 1.0000, 8000.0000, 8000.0000, N'Omit item V38', 12),
        (N'sp-ps-13', @ProjectId, 1, N'PC13', N'Provisional Sums', N'', N'', 1, N'CARP-WRD', N'Wardrobes', N'item', 1.0000, 5300.0000, 5300.0000, N'Omit Item V60', 13),
        (N'sp-ps-14', @ProjectId, 1, N'PC12', N'Provisional Sums', N'', N'', 1, N'EXTW-LND', N'Soft landscaping', N'item', 1.0000, 4800.0000, 4800.0000, N'Omit Item V28', 14),
        (N'sp-ps-15', @ProjectId, 1, N'PC15', N'Provisional Sums', N'', N'', 1, N'SPEC-SPA', N'Supply and installation of Hot Tub', N'item', 1.0000, 12500.0000, 12500.0000, N'Omit Item V25', 15),
        (N'sp-ps-16', @ProjectId, 1, N'PC16', N'Provisional Sums', N'', N'', 1, N'SPEC-GAZ', N'Supply and installation of garden gazebo', N'item', 1.0000, 7500.0000, 7500.0000, N'Omit Item V26', 16)
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
    PRINT '24 Sherwood Park: valuation lines merged.';
    COMMIT TRAN;

    -- Sanity check: the seeded blocks should reconcile to the workbook.
    SELECT
        SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 436408.00
        SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         -- 126730.00
        SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --      0.00
        SUM(LineAmount) AS ContractSum                                               -- 563138.00
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType IN (0, 1, 2)
      AND LineType NOT IN (3, 4);
END
GO
