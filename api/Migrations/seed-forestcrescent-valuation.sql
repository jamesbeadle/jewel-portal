-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per
-- JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: 6 Forest Crescent -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : Forest Crescent, Ashtead KT21 1JU
-- ProjectId: resolved at run time by site-name matcher '6forestcrescent'
--
-- Seeds the CONTRACT SCOPE only, taken from the "Valuation 10 - Retention
-- Release" workbook. A single Contract Works block makes up the Contract Sum;
-- the workbook's "Glazing - Provisional Sum" section sits inline in the bill
-- (ElementType 0, LineType 1) rather than in a separate PC block. There is no
-- separate PS block and no Contingency block in this workbook:
--
--     Contract works (incl. GBP 21,812.00 inline provisional sums - Glazing)
--     Contract Sum      GBP 174,706.00
--
-- Variations (V01..V39, net GBP 44,434.50) are NOT seeded here -- they belong
-- in seed-forestcrescent-variations.sql. Per-valuation claim history
-- (Aug-24..Apr-25 and the 6m retention release) is claim data
-- (ValuationClaims/ClaimLines), not bill structure.
--
-- The workbook has no NRM2 numbering (its own 00NN codes are cost buckets, not
-- sections); SectionCode is assigned sequentially (01..18) in workbook order
-- (Ravenswood pattern), including the "New ground floor shower room (WC1)"
-- group as its own section (18). CostCode maps each line to the JBB Cost Code
-- Master (seed-cost-centers.sql).
--
-- "Omit item ..." comments are informational: those lines are omitted by
-- variations in the register (the Glazing PS by V04, and V14/V15/V23/V24/V25/
-- V26 for individual lines), so they stay Priced/ProvisionalSum here with the
-- workbook comment copied verbatim.
--
-- Skipped rows: none -- every workbook contract row carries a value and is
-- transcribed (170 lines). All amounts equal Quantity x Rate.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (fc-cw-NNN). A re-run
-- refreshes every field via MERGE. Variation lines for this project are left
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
    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
        (N'fc-cw-001', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site manager', N'week', 20.0000, 750.0000, 15000.0000, N'', 1),
        (N'fc-cw-002', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Rubbish removal', N'week', 10.0000, 380.0000, 3800.0000, N'', 2),
        (N'fc-cw-003', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WC', N'Temporary toilet', N'week', 20.0000, 90.0000, 1800.0000, N'', 3),
        (N'fc-cw-004', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-HSC', N'CDM', N'item', 1.0000, 1200.0000, 1200.0000, N'', 4),
        (N'fc-cw-005', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'item', 20.0000, 125.0000, 2500.0000, N'', 5),
        (N'fc-cw-006', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'SCAFF-STD', N'Scaffolding', N'm2', 75.0000, 42.0000, 3150.0000, N'', 6),
        (N'fc-cw-007', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Clean on completion', N'item', 1.0000, 350.0000, 350.0000, N'', 7),
        (N'fc-cw-008', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Isolate electrical installation in connection with demolitions & strip out as required', N'item', 1.0000, 350.0000, 350.0000, N'', 8),
        (N'fc-cw-009', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Ditto plumbing & heating installation', N'item', 1.0000, 300.0000, 300.0000, N'', 9),
        (N'fc-cw-010', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove flooring, tiles, skirtings, picture rail, cove, fittings etc.', N'item', 1.0000, 200.0000, 200.0000, N'', 10),
        (N'fc-cw-011', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove sanitary items as required', N'item', 1.0000, 140.0000, 140.0000, N'', 11),
        (N'fc-cw-012', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove internal doors & frames', N'nr', 6.0000, 20.0000, 120.0000, N'', 12),
        (N'fc-cw-013', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Strip wall paper to reception room 2', N'item', 1.0000, 200.0000, 200.0000, N'', 13),
        (N'fc-cw-014', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal walls to form new layout', N'm2', 18.0000, 38.0000, 684.0000, N'', 14),
        (N'fc-cw-015', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove section of hallway ceiling & joists to form void', N'item', 1.0000, 195.0000, 195.0000, N'', 15),
        (N'fc-cw-016', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove windows & external doors - throughout', N'item', 1.0000, 400.0000, 400.0000, N'', 16),
        (N'fc-cw-017', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-STS', N'Erect temporary propping to existing construction', N'm', 8.0000, 72.0000, 576.0000, N'', 17),
        (N'fc-cw-018', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Demolish external walls to form new layout', N'm2', 4.0000, 95.0000, 380.0000, N'', 18),
        (N'fc-cw-019', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Demolish single skin garage walls', N'm2', 12.0000, 40.0000, 480.0000, N'', 19),
        (N'fc-cw-020', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove roof light to main roof', N'nr', 1.0000, 55.0000, 55.0000, N'', 20),
        (N'fc-cw-021', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove section of pitched roof covering & construction', N'm2', 36.0000, 24.0000, 864.0000, N'', 21),
        (N'fc-cw-022', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove existing front porch', N'item', 1.0000, 150.0000, 150.0000, N'', 22),
        (N'fc-cw-023', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Break up section of existing garage floor slab', N'm2', 12.0000, 42.0000, 504.0000, N'', 23),
        (N'fc-cw-024', @ProjectId, 0, N'03', N'Drainage', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 2500.0000, 2500.0000, N'', 24),
        (N'fc-cw-025', @ProjectId, 0, N'03', N'Drainage', N'', N'', 0, N'MEC-DRN', N'Provide drainage from new rainwater pipe, back inlet gulley & connection to existing drainage', N'item', 1.0000, 240.0000, 240.0000, N'', 25),
        (N'fc-cw-026', @ProjectId, 0, N'03', N'Drainage', N'', N'', 0, N'MEC-DRN', N'Make connection of new drainage to existing runs', N'item', 1.0000, 250.0000, 250.0000, N'', 26),
        (N'fc-cw-027', @ProjectId, 0, N'03', N'Drainage', N'', N'', 0, N'MEC-DRN', N'Make good damaged areas', N'item', 1.0000, 550.0000, 550.0000, N'', 27),
        (N'fc-cw-028', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-EXC', N'Excavate to reduce levels & remove spoil', N'm3', 5.0000, 125.0000, 625.0000, N'', 28),
        (N'fc-cw-029', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-PIL', N'Sleeved piled foundations as per engineers detail', N'm', 36.0000, 122.0000, 4392.0000, N'', 29),
        (N'fc-cw-030', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-PIL', N'Removal of spoil from piling', N'm3', 14.0000, 75.0000, 1050.0000, N'', 30),
        (N'fc-cw-031', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-PIL', N'Mobilisation', N'item', 1.0000, 3500.0000, 3500.0000, N'', 31),
        (N'fc-cw-032', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-PIL', N'Attendance on piling', N'week', 1.0000, 1500.0000, 1500.0000, N'', 32),
        (N'fc-cw-033', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-PIL', N'Piling mat', N'm2', 10.0000, 36.0000, 360.0000, N'', 33),
        (N'fc-cw-034', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-CON', N'Concrete to ground beams & formwork (500 x 500 mm)', N'm3', 3.0000, 325.0000, 975.0000, N'', 34),
        (N'fc-cw-035', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-CON', N'Concrete to ground beams & formwork (300 x 300 mm)', N'm3', 1.0000, 325.0000, 325.0000, N'', 35),
        (N'fc-cw-036', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-CON', N'Steel reinforcement bar to ground beams', N'kg', 600.0000, 2.4000, 1440.0000, N'', 36),
        (N'fc-cw-037', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-CON', N'220 mm Cellcore to underside of ground beam', N'm2', 8.0000, 48.0000, 384.0000, N'', 37),
        (N'fc-cw-038', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-CON', N'Corex to ground floor slab', N'm2', 10.0000, 40.0000, 400.0000, N'', 38),
        (N'fc-cw-039', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-CON', N'150 mm hardcore blinded with sand', N'm2', 16.0000, 36.0000, 576.0000, N'', 39),
        (N'fc-cw-040', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-CON', N'150 mm bed of concrete', N'm2', 16.0000, 80.0000, 1280.0000, N'', 40),
        (N'fc-cw-041', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'SUB-CON', N'2 layers of A393 mesh to slab', N'm2', 16.0000, 44.0000, 704.0000, N'', 41),
        (N'fc-cw-042', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'FLR-SCR', N'65 mm screed to porch slab', N'm2', 6.0000, 56.0000, 336.0000, N'', 42),
        (N'fc-cw-043', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'MASON-BRK', N'215 mm blockwork walls', N'm2', 1.0000, 118.0000, 118.0000, N'', 43),
        (N'fc-cw-044', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'WPF-DMP', N'Cavity walls below dpc in facing brickwork, 100 mm blockwork & lean mix cavity gill', N'm2', 3.0000, 232.0000, 696.0000, N'', 44),
        (N'fc-cw-045', @ProjectId, 0, N'04', N'Foundations & sub structure - All piling costs provisional', N'', N'', 0, N'WPF-DMP', N'Damp proof course', N'm', 14.0000, 16.0000, 224.0000, N'', 45),
        (N'fc-cw-046', @ProjectId, 0, N'05', N'Masonry Walls & Lintels', N'', N'', 0, N'INT-INW', N'Cavity wall in facing brickwork, 90 mm Kingspan insulation & 100 mm blockwork internal skin', N'm2', 22.0000, 208.0000, 4576.0000, N'', 46),
        (N'fc-cw-047', @ProjectId, 0, N'05', N'Masonry Walls & Lintels', N'', N'', 0, N'MASON-BRK', N'Facing brick piers to porch', N'm2', 1.0000, 180.0000, 180.0000, N'', 47),
        (N'fc-cw-048', @ProjectId, 0, N'05', N'Masonry Walls & Lintels', N'', N'', 0, N'MASON-BRK', N'Separate cost for supply of facing bricks (£1.50 each)', N'm2', 23.0000, 90.0000, 2070.0000, N'', 48),
        (N'fc-cw-049', @ProjectId, 0, N'05', N'Masonry Walls & Lintels', N'', N'', 0, N'MASON-BRK', N'Rake out & repoint section of brickwork - area tbc', N'm2', 10.0000, 125.0000, 1250.0000, N'', 49),
        (N'fc-cw-050', @ProjectId, 0, N'05', N'Masonry Walls & Lintels', N'', N'', 0, N'INT-INW', N'Cavity wall in two skins of 100 mm blockwork & 90 mm Kingspan insulation to cavity', N'm2', 24.0000, 188.0000, 4512.0000, N'', 50),
        (N'fc-cw-051', @ProjectId, 0, N'05', N'Masonry Walls & Lintels', N'', N'', 0, N'MASON-BRK', N'Form new window / door openings & make good reveals', N'item', 1.0000, 300.0000, 300.0000, N'', 51),
        (N'fc-cw-052', @ProjectId, 0, N'05', N'Masonry Walls & Lintels', N'', N'', 0, N'MASON-BRK', N'Wall extension profiles', N'm', 20.0000, 32.0000, 640.0000, N'', 52),
        (N'fc-cw-053', @ProjectId, 0, N'05', N'Masonry Walls & Lintels', N'', N'', 0, N'MASON-BRK', N'Thermabate cavity closers', N'm', 4.0000, 22.0000, 88.0000, N'', 53),
        (N'fc-cw-054', @ProjectId, 0, N'05', N'Masonry Walls & Lintels', N'', N'', 0, N'MASON-BRK', N'Naylor lintel over internal opening', N'nr', 5.0000, 125.0000, 625.0000, N'', 54),
        (N'fc-cw-055', @ProjectId, 0, N'05', N'Masonry Walls & Lintels', N'', N'', 0, N'MASON-BRK', N'Catnic CG90 / 100 lintels & trays', N'nr', 1.0000, 146.0000, 146.0000, N'', 55),
        (N'fc-cw-056', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'152 x 152 x 23 kg steel beams', N'kg', 440.0000, 8.0000, 3520.0000, N'', 56),
        (N'fc-cw-057', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'152 x 152 x 30 kg steel beams', N'kg', 210.0000, 8.0000, 1680.0000, N'', 57),
        (N'fc-cw-058', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'152 x 152 x 37 kg steel beam with plate', N'kg', 155.0000, 8.0000, 1240.0000, N'', 58),
        (N'fc-cw-059', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'100 x 100 x 5 SHS steel columns - galv (powder coat)', N'kg', 220.0000, 10.0000, 2200.0000, N'', 59),
        (N'fc-cw-060', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'SUB-CON', N'Cut out & cast concrete padstones', N'nr', 14.0000, 95.0000, 1330.0000, N'', 60),
        (N'fc-cw-061', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'STR-STL', N'Base plate & hold down brackets', N'nr', 4.0000, 65.0000, 260.0000, N'', 61),
        (N'fc-cw-062', @ProjectId, 0, N'06', N'Structural steels', N'', N'', 0, N'PRELIMS-PRO', N'Fireline protection to steels', N'item', 1.0000, 400.0000, 400.0000, N'', 62),
        (N'fc-cw-063', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Timber wall plate bolted to wall', N'm', 20.0000, 36.0000, 720.0000, N'', 63),
        (N'fc-cw-064', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'50 x 150 mm timber floor joists', N'm', 36.0000, 30.0000, 1080.0000, N'', 64),
        (N'fc-cw-065', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'22 mm T&G chipboard flooring', N'm2', 8.0000, 30.0000, 240.0000, N'', 65),
        (N'fc-cw-066', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-CUT', N'50 x 150 mm timber roof rafters', N'm', 156.0000, 30.0000, 4680.0000, N'', 66),
        (N'fc-cw-067', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-CUT', N'50 x 175 mm timber hips', N'm', 14.0000, 34.0000, 476.0000, N'', 67),
        (N'fc-cw-068', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Joist hangers', N'nr', 32.0000, 7.0000, 224.0000, N'', 68),
        (N'fc-cw-069', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Galvanised restraint straps', N'nr', 18.0000, 18.0000, 324.0000, N'', 69),
        (N'fc-cw-070', @ProjectId, 0, N'07', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'50 x 100 mm timber internal stud walls', N'm2', 22.0000, 70.0000, 1540.0000, N'', 70),
        (N'fc-cw-071', @ProjectId, 0, N'08', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-TLN', N'Breatherable membrane, battens & plain tiles', N'm2', 48.0000, 86.0000, 4128.0000, N'', 71),
        (N'fc-cw-072', @ProjectId, 0, N'08', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-TLN', N'Separate cost for supply of tiles (£30/m2)', N'm2', 48.0000, 30.0000, 1440.0000, N'', 72),
        (N'fc-cw-073', @ProjectId, 0, N'08', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-TLN', N'Ridge / hip tiles', N'm', 12.0000, 58.0000, 696.0000, N'', 73),
        (N'fc-cw-074', @ProjectId, 0, N'08', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-LED', N'Lead flashing / valley lining', N'm', 14.0000, 55.0000, 770.0000, N'', 74),
        (N'fc-cw-075', @ProjectId, 0, N'08', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-LED', N'Lead apron to chimney stack', N'item', 1.0000, 495.0000, 495.0000, N'', 75),
        (N'fc-cw-076', @ProjectId, 0, N'08', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-FSU', N'Upvc fascia / soffit', N'm', 26.0000, 44.0000, 1144.0000, N'', 76),
        (N'fc-cw-077', @ProjectId, 0, N'08', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-GRU', N'Guttering', N'm', 20.0000, 32.0000, 640.0000, N'', 77),
        (N'fc-cw-078', @ProjectId, 0, N'08', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-GRU', N'Rainwater pipework', N'm', 14.0000, 34.0000, 476.0000, N'', 78),
        (N'fc-cw-079', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'1100 x 2113 mm aluminium external door with side light', N'nr', 2.0000, 2025.0000, 4050.0000, N'Omit item 04', 79),
        (N'fc-cw-080', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-GAR', N'Refit existing garage door', N'nr', 1.0000, 280.0000, 280.0000, N'Omit item 04', 80),
        (N'fc-cw-081', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'686 x 2113 mm aluminium window', N'nr', 1.0000, 870.0000, 870.0000, N'Omit item 04', 81),
        (N'fc-cw-082', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'2200 x 1391 mm aluminium window', N'nr', 1.0000, 1836.0000, 1836.0000, N'Omit item 04', 82),
        (N'fc-cw-083', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'1462 x 1536 mm aluminium window', N'nr', 1.0000, 1348.0000, 1348.0000, N'Omit item 04', 83),
        (N'fc-cw-084', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'442 x 1599 mm aluminium window', N'nr', 2.0000, 408.0000, 816.0000, N'Omit item 04', 84),
        (N'fc-cw-085', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'1448 x 1536 mm aluminium window', N'nr', 1.0000, 1336.0000, 1336.0000, N'Omit item 04', 85),
        (N'fc-cw-086', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'2200 x 1791 mm aluminium window', N'nr', 1.0000, 2364.0000, 2364.0000, N'Omit item 04', 86),
        (N'fc-cw-087', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'2200 x 1728 mm aluminium window', N'nr', 1.0000, 2302.0000, 2302.0000, N'Omit item 04', 87),
        (N'fc-cw-088', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'1803 x 1726 mm aluminium window', N'nr', 1.0000, 1868.0000, 1868.0000, N'Omit item 04', 88),
        (N'fc-cw-089', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'2215 x 1726 mm aluminium window', N'nr', 1.0000, 2294.0000, 2294.0000, N'Omit item 04', 89),
        (N'fc-cw-090', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'600 x 1370 mm aluminium window', N'nr', 1.0000, 494.0000, 494.0000, N'Omit item 04', 90),
        (N'fc-cw-091', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'600 x 1280 mm aluminium window', N'nr', 1.0000, 468.0000, 468.0000, N'Omit item 04', 91),
        (N'fc-cw-092', @ProjectId, 0, N'09', N'Glazing - Provisional Sum', N'', N'', 1, N'WDR-ALU', N'1719 x 1440 mm aluminium window', N'nr', 1.0000, 1486.0000, 1486.0000, N'Omit item 04', 92),
        (N'fc-cw-093', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Earthing & bonding of new installation', N'nr', 1.0000, 250.0000, 250.0000, N'', 93),
        (N'fc-cw-094', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Double socket outlet', N'nr', 18.0000, 110.0000, 1980.0000, N'', 94),
        (N'fc-cw-095', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Shavers socket with mirror light', N'nr', 1.0000, 280.0000, 280.0000, N'', 95),
        (N'fc-cw-096', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Recessed light fittings', N'nr', 20.0000, 108.0000, 2160.0000, N'', 96),
        (N'fc-cw-097', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Pendant lights', N'nr', 5.0000, 84.0000, 420.0000, N'', 97),
        (N'fc-cw-098', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Fix only - Entrance hall pendant', N'nr', 1.0000, 100.0000, 100.0000, N'', 98),
        (N'fc-cw-099', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Garage strip light', N'nr', 1.0000, 290.0000, 290.0000, N'', 99),
        (N'fc-cw-100', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'External lights', N'nr', 2.0000, 128.0000, 256.0000, N'', 100),
        (N'fc-cw-101', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 10.0000, 38.0000, 380.0000, N'', 101),
        (N'fc-cw-102', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'MEC-VNT', N'Extractor fans', N'nr', 1.0000, 275.0000, 275.0000, N'', 102),
        (N'fc-cw-103', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Fan isolator switches', N'nr', 1.0000, 100.0000, 100.0000, N'', 103),
        (N'fc-cw-104', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Smoke/heat detector', N'nr', 2.0000, 145.0000, 290.0000, N'', 104),
        (N'fc-cw-105', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'MEC-UFH', N'Electric underfloor heating to ensutite - bedroom 1', N'm2', 4.0000, 165.0000, 660.0000, N'', 105),
        (N'fc-cw-106', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Carbon monoxide detector', N'nr', 1.0000, 160.0000, 160.0000, N'', 106),
        (N'fc-cw-107', @ProjectId, 0, N'10', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Builders work in connection with electrical installation', N'item', 1.0000, 500.0000, 500.0000, N'', 107),
        (N'fc-cw-108', @ProjectId, 0, N'11', N'Plumbing installation', N'', N'', 0, N'MEC-PLM', N'Radiators & towel rails with TRVs', N'nr', 3.0000, 400.0000, 1200.0000, N'', 108),
        (N'fc-cw-109', @ProjectId, 0, N'11', N'Plumbing installation', N'', N'', 0, N'MEC-DRN', N'Stub stack & durgo valve', N'nr', 1.0000, 130.0000, 130.0000, N'', 109),
        (N'fc-cw-110', @ProjectId, 0, N'11', N'Plumbing installation', N'', N'', 0, N'MEC-DRN', N'Soil vent pipework', N'item', 1.0000, 350.0000, 350.0000, N'', 110),
        (N'fc-cw-111', @ProjectId, 0, N'11', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Hot & cold supplies to sanitary fittings', N'nr', 7.0000, 165.0000, 1155.0000, N'', 111),
        (N'fc-cw-112', @ProjectId, 0, N'11', N'Plumbing installation', N'', N'', 0, N'MEC-DRN', N'Wastes to ditto', N'nr', 4.0000, 88.0000, 352.0000, N'', 112),
        (N'fc-cw-113', @ProjectId, 0, N'11', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Fix only - WC - 1st floor ensuite', N'nr', 1.0000, 320.0000, 320.0000, N'', 113),
        (N'fc-cw-114', @ProjectId, 0, N'11', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Fix only - Wash hand basin - first floor ensuite', N'nr', 1.0000, 310.0000, 310.0000, N'', 114),
        (N'fc-cw-115', @ProjectId, 0, N'11', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Fix only - Showers - 1st floor ensuite & WC1', N'nr', 1.0000, 495.0000, 495.0000, N'', 115),
        (N'fc-cw-116', @ProjectId, 0, N'11', N'Plumbing installation', N'', N'', 0, N'MEC-PLM', N'Builders work in connection with plumbing & heating installation', N'item', 1.0000, 500.0000, 500.0000, N'', 116),
        (N'fc-cw-117', @ProjectId, 0, N'12', N'Insulation & Plasterboard', N'', N'', 0, N'INT-INC', N'150 mm Kingspan insulation between roof rafters', N'm2', 32.0000, 52.0000, 1664.0000, N'', 117),
        (N'fc-cw-118', @ProjectId, 0, N'12', N'Insulation & Plasterboard', N'', N'', 0, N'INT-PLB', N'37.5 mm Kingspan plasterboard to rafters', N'm2', 32.0000, 40.0000, 1280.0000, N'', 118),
        (N'fc-cw-119', @ProjectId, 0, N'12', N'Insulation & Plasterboard', N'', N'', 0, N'INT-INC', N'300 mm mineral insulation over ceiling joists', N'm2', 10.0000, 46.0000, 460.0000, N'', 119),
        (N'fc-cw-120', @ProjectId, 0, N'12', N'Insulation & Plasterboard', N'', N'', 0, N'INT-INF', N'100 mm mineral insulation between floor joists', N'm2', 8.0000, 28.0000, 224.0000, N'', 120),
        (N'fc-cw-121', @ProjectId, 0, N'12', N'Insulation & Plasterboard', N'', N'', 0, N'INT-PLB', N'12.5mm plasterboard to ceilings', N'm2', 8.0000, 20.0000, 160.0000, N'', 121),
        (N'fc-cw-122', @ProjectId, 0, N'12', N'Insulation & Plasterboard', N'', N'', 0, N'INT-INW', N'50 mm rockwool insulation between stud walls', N'm2', 22.0000, 14.0000, 308.0000, N'', 122),
        (N'fc-cw-123', @ProjectId, 0, N'12', N'Insulation & Plasterboard', N'', N'', 0, N'INT-PLB', N'12.5 mm plasterboard to blockwork & studs', N'm2', 98.0000, 18.0000, 1764.0000, N'', 123),
        (N'fc-cw-124', @ProjectId, 0, N'13', N'Plastering & Render', N'', N'', 0, N'INT-PLS', N'3 mm skim to new & existing walls', N'm2', 192.0000, 18.0000, 3456.0000, N'', 124),
        (N'fc-cw-125', @ProjectId, 0, N'13', N'Plastering & Render', N'', N'', 0, N'INT-PLS', N'3 mm skim to ceilings', N'm2', 40.0000, 18.0000, 720.0000, N'', 125),
        (N'fc-cw-126', @ProjectId, 0, N'13', N'Plastering & Render', N'', N'', 0, N'INT-RDR', N'K - rend to blockwork & existing front of house', N'm2', 25.0000, 120.0000, 3000.0000, N'', 126),
        (N'fc-cw-127', @ProjectId, 0, N'14', N'Carpentry - Internal', N'', N'', 0, N'CARP-DOR', N'Internal door lining, door & ironmongery (£150 supply)', N'nr', 6.0000, 395.0000, 2370.0000, N'', 127),
        (N'fc-cw-128', @ProjectId, 0, N'14', N'Carpentry - Internal', N'', N'', 0, N'CARP-DOR', N'Internal door lining, double door & ironmongery', N'nr', 1.0000, 845.0000, 845.0000, N'Omit item V15', 128),
        (N'fc-cw-129', @ProjectId, 0, N'14', N'Carpentry - Internal', N'', N'', 0, N'CARP-2FX', N'MDF architraves to new door frames', N'm', 75.0000, 14.0000, 1050.0000, N'', 129),
        (N'fc-cw-130', @ProjectId, 0, N'14', N'Carpentry - Internal', N'', N'', 0, N'CARP-2FX', N'MDF skirting to walls to new walls (£8/m supply)', N'm', 32.0000, 28.0000, 896.0000, N'Omit item V15', 130),
        (N'fc-cw-131', @ProjectId, 0, N'14', N'Carpentry - Internal', N'', N'', 0, N'CARP-2FX', N'MDF window boards', N'm', 4.0000, 36.0000, 144.0000, N'', 131),
        (N'fc-cw-132', @ProjectId, 0, N'14', N'Carpentry - Internal', N'', N'', 0, N'CARP-1FX', N'Plywood boxing to internal pipework', N'item', 1.0000, 250.0000, 250.0000, N'', 132),
        (N'fc-cw-133', @ProjectId, 0, N'14', N'Carpentry - Internal', N'', N'', 0, N'CARP-2FX', N'New ceiling access hatch (infill existing)', N'item', 1.0000, 455.0000, 455.0000, N'', 133),
        (N'fc-cw-134', @ProjectId, 0, N'14', N'Carpentry - Internal', N'', N'', 0, N'CARP-2FX', N'Make good to area of utility as required', N'item', 1.0000, 200.0000, 200.0000, N'Omit item V25', 134),
        (N'fc-cw-135', @ProjectId, 0, N'15', N'Staircase', N'', N'', 0, N'STR-GRL', N'Glazed balustrade to first floor landing', N'item', 1.0000, 1200.0000, 1200.0000, N'Omit item V23', 135),
        (N'fc-cw-136', @ProjectId, 0, N'16', N'Joinery', N'', N'', 0, N'CARP-JNR', N'Under stairs storage doors / cupboard - design tbc', N'nr', 1.0000, 550.0000, 550.0000, N'Omit item V24', 136),
        (N'fc-cw-137', @ProjectId, 0, N'17', N'Decorations & finishes', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 124.0000, 16.0000, 1984.0000, N'', 137),
        (N'fc-cw-138', @ProjectId, 0, N'17', N'Decorations & finishes', N'', N'', 0, N'DEC-STD', N'Ditto walls', N'm2', 248.0000, 14.0000, 3472.0000, N'', 138),
        (N'fc-cw-139', @ProjectId, 0, N'17', N'Decorations & finishes', N'', N'', 0, N'CARP-2FX', N'Timber battens & vertical oak cladding', N'm2', 6.0000, 120.0000, 720.0000, N'Omit item V14', 139),
        (N'fc-cw-140', @ProjectId, 0, N'17', N'Decorations & finishes', N'', N'', 0, N'WDR-TIM', N'Prepare & decorate doors', N'm2', 25.0000, 30.0000, 750.0000, N'', 140),
        (N'fc-cw-141', @ProjectId, 0, N'17', N'Decorations & finishes', N'', N'', 0, N'DEC-STD', N'Prepare & decorate existing staircase', N'item', 1.0000, 850.0000, 850.0000, N'Omit item V26', 141),
        (N'fc-cw-142', @ProjectId, 0, N'17', N'Decorations & finishes', N'', N'', 0, N'CARP-2FX', N'Frames, architrave, window board & skirtings', N'm', 172.0000, 8.0000, 1376.0000, N'', 142),
        (N'fc-cw-143', @ProjectId, 0, N'17', N'Decorations & finishes', N'', N'', 0, N'TIL-STD', N'Fix only - Floor & wall tiles to ensuites / bathrooms', N'm2', 16.0000, 80.0000, 1280.0000, N'', 143),
        (N'fc-cw-144', @ProjectId, 0, N'17', N'Decorations & finishes', N'', N'', 0, N'FLR-WD', N'Reconfigure hallway flooring into reception room 1', N'item', 1.0000, 440.0000, 440.0000, N'', 144),
        (N'fc-cw-145', @ProjectId, 0, N'17', N'Decorations & finishes', N'', N'', 0, N'FLR-WD', N'Sand & treat existing timber floor to reception room 1 & 2', N'm2', 32.0000, 52.0000, 1664.0000, N'', 145),
        (N'fc-cw-146', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'ENABLE-DEM', N'Remove section of utility units, worktops & appliances', N'item', 1.0000, 80.0000, 80.0000, N'', 146),
        (N'fc-cw-147', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'ENABLE-DEM', N'Remove sanitary items as required', N'item', 1.0000, 100.0000, 100.0000, N'', 147),
        (N'fc-cw-148', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal walls to form new layout', N'm2', 4.0000, 38.0000, 152.0000, N'', 148),
        (N'fc-cw-149', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'MASON-BRK', N'Naylor lintel over internal opening', N'nr', 1.0000, 125.0000, 125.0000, N'', 149),
        (N'fc-cw-150', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'CARP-1FX', N'50 x 100 mm timber internal stud walls', N'm2', 4.0000, 70.0000, 280.0000, N'', 150),
        (N'fc-cw-151', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'CARP-DOR', N'Internal door lining, door & ironmongery (£150 supply)', N'nr', 1.0000, 395.0000, 395.0000, N'', 151),
        (N'fc-cw-152', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'CARP-2FX', N'MDF architraves to new door frames', N'm', 10.0000, 14.0000, 140.0000, N'', 152),
        (N'fc-cw-153', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'INT-INW', N'50 mm rockwool insulation between stud walls', N'm2', 4.0000, 14.0000, 56.0000, N'', 153),
        (N'fc-cw-154', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'INT-PLB', N'12.5 mm plasterboard to blockwork & studs', N'm2', 8.0000, 18.0000, 144.0000, N'', 154),
        (N'fc-cw-155', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'ELE-STD', N'Recessed light fittings', N'nr', 4.0000, 108.0000, 432.0000, N'', 155),
        (N'fc-cw-156', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 1.0000, 38.0000, 38.0000, N'', 156),
        (N'fc-cw-157', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'MEC-VNT', N'Extractor fans', N'nr', 1.0000, 275.0000, 275.0000, N'', 157),
        (N'fc-cw-158', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'ELE-STD', N'Fan isolator switches', N'nr', 1.0000, 100.0000, 100.0000, N'', 158),
        (N'fc-cw-159', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'MEC-PLM', N'Radiators & towel rails with TRVs', N'nr', 1.0000, 400.0000, 400.0000, N'', 159),
        (N'fc-cw-160', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'MEC-DRN', N'Stub stack & durgo valve', N'nr', 1.0000, 130.0000, 130.0000, N'', 160),
        (N'fc-cw-161', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'MEC-DRN', N'Soil vent pipework', N'item', 1.0000, 350.0000, 350.0000, N'', 161),
        (N'fc-cw-162', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'SUP-SAN', N'Hot & cold supplies to sanitary fittings', N'nr', 5.0000, 165.0000, 825.0000, N'', 162),
        (N'fc-cw-163', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'MEC-DRN', N'Wastes to ditto', N'nr', 4.0000, 88.0000, 352.0000, N'', 163),
        (N'fc-cw-164', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'SUP-SAN', N'Fix only - WC', N'nr', 1.0000, 320.0000, 320.0000, N'', 164),
        (N'fc-cw-165', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'SUP-SAN', N'Fix only - Wash hand basin', N'nr', 1.0000, 310.0000, 310.0000, N'', 165),
        (N'fc-cw-166', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'SUP-SAN', N'Fix only - Showers', N'nr', 1.0000, 495.0000, 495.0000, N'', 166),
        (N'fc-cw-167', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'TIL-STD', N'Fix only - Floor & wall tiles to ensuites / bathrooms', N'm2', 10.0000, 80.0000, 800.0000, N'', 167),
        (N'fc-cw-168', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 3.0000, 16.0000, 48.0000, N'', 168),
        (N'fc-cw-169', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'DEC-STD', N'Ditto walls', N'm2', 6.0000, 14.0000, 84.0000, N'', 169),
        (N'fc-cw-170', @ProjectId, 0, N'18', N'New ground floor shower room (WC1)', N'', N'', 0, N'WDR-TIM', N'Prepare & decorate doors', N'm2', 3.5000, 30.0000, 105.0000, N'', 170)
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

    PRINT '6 Forest Crescent: contract valuation lines merged.';
    COMMIT TRAN;
END
GO

-- Sanity check: the seeded block should reconcile to the workbook.
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '6forestcrescent'
       OR LOWER(REPLACE(Name, ' ', '')) = '6forestcrescent'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = '6forestcrescent' THEN 0 ELSE 1 END);
SELECT
    SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 174706.00 (incl. 21812.00 inline Glazing PS)
    SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         --      0.00 (none in this workbook)
    SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --      0.00 (none in this workbook)
    SUM(LineAmount) AS ContractSum                                               -- 174706.00
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = @ProjectId AND ElementType IN (0, 1, 2)
  AND LineType NOT IN (3, 4);
GO
