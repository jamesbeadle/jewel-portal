-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per JBB_CostCode_Master v2.1) seeded
-- by seed-cost-centers.sql.
-- Seed: Windy Ridge Godalming -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : Windy Ridge, Primrose Ridge, Godalming, GU7 2ND
-- ProjectId: resolved at run time by site-name matcher 'windyridgegodalming'
--
-- Seeds the CONTRACT SCOPE only, taken from the "Valuation 10 - Retention"
-- workbook. A single Contract Works block makes up the Contract Sum; the
-- workbook's "Glazing PS" section holds inline provisional sums (LineType 1)
-- rather than a separate PC block:
--
--     Contract works (incl. GBP 27,520.00 inline Glazing PS lines)
--     Contract Sum      GBP 176,784.55
--
-- Variations (V01..V21, net GBP 36,748.59; Revised Contract Sum
-- GBP 213,533.14) are NOT seeded here -- they belong in
-- seed-windyridge-variations.sql. Per-valuation claim history (Valuation
-- 01..09, retention release) is claim data, not bill structure.
--
-- The workbook has no NRM2 numbering (its own 0001/0002/... codes are a
-- legacy numbering and are dropped); SectionCode is assigned sequentially
-- (01..14) in workbook order, per the Ravenswood pattern. CostCode maps each
-- line to the Jewel cost-centre master (seed-cost-centers.sql).
--
-- RECONCILIATION NOTE: the workbook's 127 contract lines sum to
-- GBP 176,784.56, one penny over the stated Contract Sum of GBP 176,784.55
-- (the workbook's own claim columns round the two 50%-claimed supply lines
-- down). Per convention we reconcile to the STATED Contract Sum: the
-- "Supply of Sanitary ware including OH&P" line is seeded at 7,710.64
-- (workbook 7,710.65), noted in its Comments.
--
-- Skipped rows: none -- every contract line in the workbook carries a value.
-- "Omit item Vnn" comments are informational: those lines are omitted by
-- variations in the register, so they stay Priced/ProvisionalSum here.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (wr-cw-NNN). A re-run
-- refreshes every field via MERGE. Variation lines for this project are left
-- untouched. Safe to run repeatedly.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ProjectId nvarchar(64) = (
    SELECT TOP 1 ProjectId FROM Projects
    WHERE LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'windyridgegodalming'
       OR LOWER(REPLACE(Name, ' ', '')) = 'windyridgegodalming'
    ORDER BY CASE WHEN LOWER(REPLACE(COALESCE(XeroSiteName, ''), ' ', '')) = 'windyridgegodalming' THEN 0 ELSE 1 END);
IF @ProjectId IS NULL
BEGIN
    PRINT 'SKIP  Windy Ridge Godalming -- no project matches this site name; nothing touched.';
    ROLLBACK TRAN;
END
ELSE
BEGIN
    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
    (N'wr-cw-001', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'SCAFF-STD', N'Scaffolding', N'm2', 60.0000, 36.0000, 2160.0000, N'', 1),
    (N'wr-cw-002', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site manager', N'week', 12.0000, 300.0000, 3600.0000, N'', 2),
    (N'wr-cw-003', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Rubbish removal', N'week', 6.0000, 380.0000, 2280.0000, N'', 3),
    (N'wr-cw-004', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-PRO', N'General protection', N'item', 1.0000, 500.0000, 500.0000, N'', 4),
    (N'wr-cw-005', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WC', N'Temporary toilet', N'week', 12.0000, 90.0000, 1080.0000, N'', 5),
    (N'wr-cw-006', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'item', 12.0000, 80.0000, 960.0000, N'', 6),
    (N'wr-cw-007', @ProjectId, 0, N'01', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Clean on completion', N'item', 1.0000, 350.0000, 350.0000, N'', 7),
    (N'wr-cw-008', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Isolate electrical installation in connection with demolitions & strip out as required', N'item', 1.0000, 200.0000, 200.0000, N'', 8),
    (N'wr-cw-009', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Ditto plumbing & heating installation - including boiler', N'item', 1.0000, 250.0000, 250.0000, N'', 9),
    (N'wr-cw-010', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove flooring, tiles, skirtings, picture rail, cove, fittings etc.', N'item', 1.0000, 150.0000, 150.0000, N'', 10),
    (N'wr-cw-011', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove utility units, worktops & appliances', N'item', 1.0000, 120.0000, 120.0000, N'', 11),
    (N'wr-cw-012', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove internal door & frame', N'nr', 2.0000, 20.0000, 40.0000, N'', 12),
    (N'wr-cw-013', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal walls', N'm2', 9.0000, 42.0000, 378.0000, N'', 13),
    (N'wr-cw-014', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-STS', N'Erect temporary propping to existing construction', N'm', 3.0000, 80.0000, 240.0000, N'', 14),
    (N'wr-cw-015', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Remove section of roof covering & construction', N'm2', 12.0000, 32.0000, 384.0000, N'', 15),
    (N'wr-cw-016', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Demolish external walls to form new openings', N'm2', 4.0000, 85.0000, 340.0000, N'', 16),
    (N'wr-cw-017', @ProjectId, 0, N'02', N'Demolition & striping out', N'', N'', 0, N'ENABLE-DEM', N'Break up, remove slabs, demolish garden walls, etc', N'm2', 22.0000, 24.0000, 528.0000, N'', 17),
    (N'wr-cw-018', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-EXC', N'Excavate to reduce levels & remove spoil', N'm3', 25.0000, 125.0000, 3125.0000, N'', 18),
    (N'wr-cw-019', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-EXC', N'Excavate for strip foundation 600 x 1000 mm & remove spoil', N'm3', 3.0000, 160.0000, 480.0000, N'', 19),
    (N'wr-cw-020', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-EXC', N'Excavate for retaing wall foundations & remove spoil', N'm3', 10.0000, 160.0000, 1600.0000, N'', 20),
    (N'wr-cw-021', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-EXC', N'Earthwork support', N'm2', 14.0000, 82.0000, 1148.0000, N'', 21),
    (N'wr-cw-022', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'Concrete in strip foundations', N'm3', 10.0000, 170.0000, 1700.0000, N'', 22),
    (N'wr-cw-023', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'200 mm concrete to retaining walls', N'm3', 3.0000, 295.0000, 885.0000, N'', 23),
    (N'wr-cw-024', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'Formwork to face of retaining walls', N'm2', 14.0000, 72.0000, 1008.0000, N'', 24),
    (N'wr-cw-025', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'STR-MSH', N'Steel reinforcement bar to retaining wall & foundations', N'kg', 580.0000, 2.4000, 1392.0000, N'', 25),
    (N'wr-cw-026', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'WPF-DMP', N'Cavity walls below dpc in two skins of 100 mm blockwork & lean mix cavity gill', N'm2', 4.0000, 202.0000, 808.0000, N'', 26),
    (N'wr-cw-027', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'WPF-DMP', N'Damp proof course', N'm', 18.0000, 16.0000, 288.0000, N'', 27),
    (N'wr-cw-028', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'150 mm hardcore blinded with sand', N'm2', 25.0000, 38.0000, 950.0000, N'', 28),
    (N'wr-cw-029', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'SUB-CON', N'150 mm bed of concrete', N'm3', 4.0000, 295.0000, 1180.0000, N'', 29),
    (N'wr-cw-030', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'STR-MSH', N'a layers of A393 mesh to floor slab', N'm2', 25.0000, 70.0000, 1750.0000, N'', 30),
    (N'wr-cw-031', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'WPF-DMP', N'Damp proof membranes 1200 g', N'm2', 25.0000, 18.0000, 450.0000, N'', 31),
    (N'wr-cw-032', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'INT-INF', N'90 mm Celotex floor insulation', N'm2', 25.0000, 36.0000, 900.0000, N'', 32),
    (N'wr-cw-033', @ProjectId, 0, N'03', N'Foundations & sub structure', N'', N'', 0, N'FLR-SCR', N'65 mm sand / cement floor screed', N'm2', 25.0000, 62.0000, 1550.0000, N'', 33),
    (N'wr-cw-034', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'MEC-DRN', N'Break out floor & remove existing draina runs', N'item', 1.0000, 375.0000, 375.0000, N'', 34),
    (N'wr-cw-035', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'MEC-DRN', N'Grub out & backfill existing inspection chamber', N'item', 1.0000, 380.0000, 380.0000, N'', 35),
    (N'wr-cw-036', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'MEC-DRN', N'Remove existing drain & gulley', N'item', 1.0000, 150.0000, 150.0000, N'', 36),
    (N'wr-cw-037', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'm', 30.0000, 135.0000, 4050.0000, N'', 37),
    (N'wr-cw-038', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'MEC-DRN', N'Provide drainage from new rainwater pipe, back inlet gulley & connection to existing drainage', N'item', 1.0000, 340.0000, 340.0000, N'', 38),
    (N'wr-cw-039', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'MEC-DRN', N'Connect into existing drainage run', N'item', 1.0000, 300.0000, 300.0000, N'', 39),
    (N'wr-cw-040', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'SUB-DRN', N'New soakaway', N'nr', 1.0000, 980.0000, 980.0000, N'', 40),
    (N'wr-cw-041', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'MEC-DRN', N'New inspection chamber', N'nr', 2.0000, 625.0000, 1250.0000, N'', 41),
    (N'wr-cw-042', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'SUB-DRN', N'French drain to perimeter', N'm', 19.0000, 132.0000, 2508.0000, N'', 42),
    (N'wr-cw-043', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'WPF-EXT', N'Sika slurry to floor & retaining wall (waterproofing)', N'm2', 31.0000, 46.0000, 1426.0000, N'', 43),
    (N'wr-cw-044', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'SUB-CON', N'Cast external steps with brick ledge', N'item', 1.0000, 2500.0000, 2500.0000, N'', 44),
    (N'wr-cw-045', @ProjectId, 0, N'04', N'Drainage & external works', N'', N'', 0, N'SUB-GWK', N'Make good damaged areas', N'item', 1.0000, 500.0000, 500.0000, N'', 45),
    (N'wr-cw-046', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Single skin of facing brickwork (external)', N'm2', 32.0000, 102.0000, 3264.0000, N'', 46),
    (N'wr-cw-047', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Single skin of facing brickwork (internal)', N'm2', 8.0000, 102.0000, 816.0000, N'', 47),
    (N'wr-cw-048', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Single skin of facing brickwork (to retaining / dwarf wall)', N'm2', 6.0000, 102.0000, 612.0000, N'', 48),
    (N'wr-cw-049', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Separate cost for supply of facing bricks (£1.50 each)', N'm2', 46.0000, 90.0000, 4140.0000, N'', 49),
    (N'wr-cw-050', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Capping stones to dwarf wall', N'm', 7.0000, 90.0000, 630.0000, N'', 50),
    (N'wr-cw-051', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'100 mm blockwork to top of retaining walls', N'm2', 2.0000, 82.0000, 164.0000, N'', 51),
    (N'wr-cw-052', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Two skins of 100 mm blockwork internally', N'm2', 5.0000, 164.0000, 820.0000, N'', 52),
    (N'wr-cw-053', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Solid wall lintel over external openings', N'm', 6.0000, 145.0000, 870.0000, N'', 53),
    (N'wr-cw-054', @ProjectId, 0, N'05', N'Masonry walls & lintels', N'', N'', 0, N'MASON-BRK', N'Wall ties, movement joinets, etc', N'item', 1.0000, 400.0000, 400.0000, N'', 54),
    (N'wr-cw-055', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'50 x 100 mm timber framed external wall', N'm2', 32.0000, 72.0000, 2304.0000, N'', 55),
    (N'wr-cw-056', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'12 mm plywood with breatherable mebrane', N'm2', 32.0000, 32.0000, 1024.0000, N'', 56),
    (N'wr-cw-057', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'75 x 100 mm timber vertical studs', N'm', 22.0000, 30.0000, 660.0000, N'', 57),
    (N'wr-cw-058', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'50 x 200 mm timber vertical studs', N'm', 42.0000, 34.0000, 1428.0000, N'', 58),
    (N'wr-cw-059', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'50 x 150 mm timber plate', N'm', 16.0000, 32.0000, 512.0000, N'', 59),
    (N'wr-cw-060', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'75 x 200 mm timber frame', N'm', 102.0000, 40.0000, 4080.0000, N'', 60),
    (N'wr-cw-061', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Extra over cost for European Oak', N'm', 102.0000, 8.0000, 816.0000, N'', 61),
    (N'wr-cw-062', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Extra over cost for Western Red Cedar', N'm', 102.0000, 10.0000, 1020.0000, N'Omit item V04', 62),
    (N'wr-cw-063', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Extra over cost for Douglas Fir', N'm', 102.0000, 10.0000, 1020.0000, N'Omit item V04', 63),
    (N'wr-cw-064', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Extra over cost for Accoya', N'm', 102.0000, 12.0000, 1224.0000, N'Omit item V04', 64),
    (N'wr-cw-065', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Extra over cost for European Larch', N'm', 102.0000, 12.0000, 1224.0000, N'Omit item V04', 65),
    (N'wr-cw-066', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-CUT', N'50 x 200 mm timber ridge / valley', N'm', 6.0000, 34.0000, 204.0000, N'', 66),
    (N'wr-cw-067', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-CUT', N'50 x 150 mm timber roof rafters', N'm', 176.0000, 32.0000, 5632.0000, N'', 67),
    (N'wr-cw-068', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Hangers, restraint straps, connection plates, etc', N'item', 1.0000, 1500.0000, 1500.0000, N'', 68),
    (N'wr-cw-069', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'Timber battening to walls', N'm2', 6.0000, 32.0000, 192.0000, N'', 69),
    (N'wr-cw-070', @ProjectId, 0, N'06', N'Timber carcassing', N'', N'', 0, N'CARP-1FX', N'50 x 100 mm timber stud internal walls', N'm2', 18.0000, 72.0000, 1296.0000, N'', 70),
    (N'wr-cw-071', @ProjectId, 0, N'07', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-TLN', N'Breatherable membrane, battens & plain clay tiles', N'm2', 52.0000, 155.0000, 8060.0000, N'', 71),
    (N'wr-cw-072', @ProjectId, 0, N'07', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-TLN', N'Ridge tiles', N'm', 10.0000, 62.0000, 620.0000, N'', 72),
    (N'wr-cw-073', @ProjectId, 0, N'07', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-LED', N'Lead flashings', N'm', 6.0000, 64.0000, 384.0000, N'', 73),
    (N'wr-cw-074', @ProjectId, 0, N'07', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-FSU', N'Fascia / soffit', N'm', 22.0000, 58.0000, 1276.0000, N'', 74),
    (N'wr-cw-075', @ProjectId, 0, N'07', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-GRU', N'Guttering', N'm', 15.0000, 34.0000, 510.0000, N'', 75),
    (N'wr-cw-076', @ProjectId, 0, N'07', N'Roof covering & rainwater goods', N'', N'', 0, N'ROOF-GRU', N'Rainwater pipe', N'm', 6.0000, 36.0000, 216.0000, N'', 76),
    (N'wr-cw-077', @ProjectId, 0, N'08', N'Glazing PS', N'', N'', 1, N'WDR-ALU', N'2350 x 2100 mm Crittal sliding doors - ED01 (tinted glass)', N'nr', 1.0000, 6980.0000, 6980.0000, N'Omit item V01', 77),
    (N'wr-cw-078', @ProjectId, 0, N'08', N'Glazing PS', N'', N'', 1, N'WDR-ALU', N'1275 x 1275 mm Crittal window - W01 (tinted glass)', N'nr', 1.0000, 1720.0000, 1720.0000, N'Omit item V01', 78),
    (N'wr-cw-079', @ProjectId, 0, N'08', N'Glazing PS', N'', N'', 1, N'WDR-ALU', N'1097 x 1275 mm Crittal window - W02 (tinted glass)', N'nr', 1.0000, 1680.0000, 1680.0000, N'Omit item V01', 79),
    (N'wr-cw-080', @ProjectId, 0, N'08', N'Glazing PS', N'', N'', 1, N'WDR-SPG', N'Velux GGL FC08 - RL01 (with electric blinds)', N'nr', 5.0000, 2880.0000, 14400.0000, N'Omit item V02 - No.1', 80),
    (N'wr-cw-081', @ProjectId, 0, N'08', N'Glazing PS', N'', N'', 1, N'WDR-SPG', N'Velux GGL UK04 - RL02 (with electric blinds)', N'nr', 1.0000, 2740.0000, 2740.0000, N'', 81),
    (N'wr-cw-082', @ProjectId, 0, N'09', N'Insulation & Plastering', N'', N'', 0, N'INT-INC', N'2 layers of 75 mm Kingspan insulation to rafters', N'm2', 52.0000, 64.0000, 3328.0000, N'', 82),
    (N'wr-cw-083', @ProjectId, 0, N'09', N'Insulation & Plastering', N'', N'', 0, N'INT-PLB', N'12.5mm plasterboard to ceilings', N'm2', 52.0000, 20.0000, 1040.0000, N'', 83),
    (N'wr-cw-084', @ProjectId, 0, N'09', N'Insulation & Plastering', N'', N'', 0, N'INT-PLS', N'3 mm skim to ceilings', N'm2', 52.0000, 18.0000, 936.0000, N'', 84),
    (N'wr-cw-085', @ProjectId, 0, N'09', N'Insulation & Plastering', N'', N'', 0, N'INT-INW', N'100 mm Kingspan insulation between battens', N'm2', 6.0000, 38.0000, 228.0000, N'', 85),
    (N'wr-cw-086', @ProjectId, 0, N'09', N'Insulation & Plastering', N'', N'', 0, N'INT-INW', N'60 mm Kingspan insulation between external studs', N'm2', 32.0000, 28.0000, 896.0000, N'', 86),
    (N'wr-cw-087', @ProjectId, 0, N'09', N'Insulation & Plastering', N'', N'', 0, N'INT-INW', N'50 mm mineral insulation between internal studs', N'm2', 18.0000, 16.0000, 288.0000, N'', 87),
    (N'wr-cw-088', @ProjectId, 0, N'09', N'Insulation & Plastering', N'', N'', 0, N'INT-PLB', N'12.5 mm plasterboard to blockwork', N'm2', 68.0000, 18.0000, 1224.0000, N'', 88),
    (N'wr-cw-089', @ProjectId, 0, N'09', N'Insulation & Plastering', N'', N'', 0, N'INT-PLS', N'3 mm skim to walls', N'm2', 68.0000, 18.0000, 1224.0000, N'', 89),
    (N'wr-cw-090', @ProjectId, 0, N'10', N'Internal joinery', N'', N'', 0, N'WDR-INT', N'1194 x 2100 mm Crittal pocket door - ID01', N'nr', 1.0000, 1650.0000, 1650.0000, N'Omit item V01', 90),
    (N'wr-cw-091', @ProjectId, 0, N'10', N'Internal joinery', N'', N'', 0, N'WDR-INT', N'1156 x 2100 mm Crittal pocket door - ID02', N'nr', 1.0000, 1590.0000, 1590.0000, N'Omit item V01', 91),
    (N'wr-cw-092', @ProjectId, 0, N'10', N'Internal joinery', N'', N'', 0, N'WDR-INT', N'1471 x 2100 mm Crital pocket door - ID03', N'nr', 1.0000, 3450.0000, 3450.0000, N'Omit item V01', 92),
    (N'wr-cw-093', @ProjectId, 0, N'10', N'Internal joinery', N'', N'', 0, N'CARP-2FX', N'Arcitraves', N'm', 35.0000, 14.0000, 490.0000, N'', 93),
    (N'wr-cw-094', @ProjectId, 0, N'10', N'Internal joinery', N'', N'', 0, N'CARP-2FX', N'Skirting boards', N'm', 22.0000, 28.0000, 616.0000, N'', 94),
    (N'wr-cw-095', @ProjectId, 0, N'10', N'Internal joinery', N'', N'', 0, N'CARP-2FX', N'Window boards', N'm', 2.0000, 42.0000, 84.0000, N'', 95),
    (N'wr-cw-096', @ProjectId, 0, N'10', N'Internal joinery', N'', N'', 0, N'SUP-APP', N'Fix only - Utility appliances', N'item', 1.0000, 300.0000, 300.0000, N'', 96),
    (N'wr-cw-097', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'New consumer unit', N'nr', 1.0000, 975.0000, 975.0000, N'', 97),
    (N'wr-cw-098', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Double socket outlet', N'nr', 5.0000, 110.0000, 550.0000, N'', 98),
    (N'wr-cw-099', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'External double socket outlet', N'nr', 1.0000, 120.0000, 120.0000, N'', 99),
    (N'wr-cw-100', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Fused spurs', N'nr', 5.0000, 100.0000, 500.0000, N'', 100),
    (N'wr-cw-101', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Recessed light fittings', N'nr', 9.0000, 105.0000, 945.0000, N'', 101),
    (N'wr-cw-102', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'LED strip lights', N'm', 14.0000, 75.0000, 1050.0000, N'', 102),
    (N'wr-cw-103', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 6.0000, 38.0000, 228.0000, N'', 103),
    (N'wr-cw-104', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'MEC-VNT', N'Extractor fans', N'nr', 2.0000, 275.0000, 550.0000, N'', 104),
    (N'wr-cw-105', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Fan isolator switches', N'nr', 2.0000, 100.0000, 200.0000, N'', 105),
    (N'wr-cw-106', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'Smoke/heat detector', N'nr', 1.0000, 125.0000, 125.0000, N'', 106),
    (N'wr-cw-107', @ProjectId, 0, N'11', N'Electrical installation', N'', N'', 0, N'ELE-STD', N'BWIC', N'item', 1.0000, 250.0000, 250.0000, N'', 107),
    (N'wr-cw-108', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'MEC-BLR', N'New boiler & associated pipework', N'nr', 1.0000, 3250.0000, 3250.0000, N'', 108),
    (N'wr-cw-109', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'MEC-UFH', N'Wet underfloor heating', N'm2', 31.0000, 155.0000, 4805.0000, N'', 109),
    (N'wr-cw-110', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'MEC-UFH', N'Manifold / thermostat', N'nr', 1.0000, 400.0000, 400.0000, N'', 110),
    (N'wr-cw-111', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'MEC-PLM', N'Towel rail', N'nr', 1.0000, 425.0000, 425.0000, N'', 111),
    (N'wr-cw-112', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'MEC-DRN', N'Stub stack & SVP', N'nr', 3.0000, 125.0000, 375.0000, N'', 112),
    (N'wr-cw-113', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'MEC-PLM', N'Hot & cold supplies', N'nr', 8.0000, 165.0000, 1320.0000, N'', 113),
    (N'wr-cw-114', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'MEC-DRN', N'Wastes to ditto', N'nr', 5.0000, 88.0000, 440.0000, N'', 114),
    (N'wr-cw-115', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Fix only - WC', N'nr', 1.0000, 320.0000, 320.0000, N'', 115),
    (N'wr-cw-116', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Fix only - Wash hand basin', N'nr', 2.0000, 310.0000, 620.0000, N'', 116),
    (N'wr-cw-117', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Fix only - Shower', N'nr', 1.0000, 320.0000, 320.0000, N'', 117),
    (N'wr-cw-118', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'MEC-PLM', N'External tap', N'nr', 1.0000, 145.0000, 145.0000, N'', 118),
    (N'wr-cw-119', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'MEC-PLM', N'BWIC', N'nr', 5.0000, 250.0000, 1250.0000, N'', 119),
    (N'wr-cw-120', @ProjectId, 0, N'12', N'Plumbing installation', N'', N'', 0, N'SUP-SAN', N'Supply of Sanitary ware including OH&P', N'item', 1.0000, 7710.6400, 7710.6400, N'Workbook amount 7,710.65 reduced by 0.01 to reconcile to the stated Contract Sum', 120),
    (N'wr-cw-121', @ProjectId, 0, N'13', N'Tiles & Floor Finishes', N'', N'', 0, N'FLR-SLF', N'Self leveling floor screed', N'm2', 31.0000, 30.0000, 930.0000, N'', 121),
    (N'wr-cw-122', @ProjectId, 0, N'13', N'Tiles & Floor Finishes', N'', N'', 0, N'TIL-STD', N'Floor tiles (£60/m supply)', N'm2', 31.0000, 140.0000, 4340.0000, N'', 122),
    (N'wr-cw-123', @ProjectId, 0, N'13', N'Tiles & Floor Finishes', N'', N'', 0, N'SUP-TIL', N'Supply of Tiles Bathroom', N'item', 1.0000, 1599.9100, 1599.9100, N'', 123),
    (N'wr-cw-124', @ProjectId, 0, N'13', N'Tiles & Floor Finishes', N'', N'', 0, N'TIL-STD', N'Installation of shower room tiles', N'm2', 21.0000, 70.0000, 1470.0000, N'', 124),
    (N'wr-cw-125', @ProjectId, 0, N'14', N'Decorations', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 52.0000, 16.0000, 832.0000, N'', 125),
    (N'wr-cw-126', @ProjectId, 0, N'14', N'Decorations', N'', N'', 0, N'DEC-STD', N'Ditto walls', N'm2', 68.0000, 14.0000, 952.0000, N'', 126),
    (N'wr-cw-127', @ProjectId, 0, N'14', N'Decorations', N'', N'', 0, N'CARP-2FX', N'Frames, architrave, window board & skirtings', N'm', 59.0000, 8.0000, 472.0000, N'', 127)
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

    PRINT 'Windy Ridge Godalming: valuation lines merged.';
    COMMIT TRAN;

    -- Sanity check: the seeded block should reconcile to the workbook.
    SELECT
        SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 176784.55 (incl. 27520.00 inline Glazing PS)
        SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         --      0.00
        SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --      0.00
        SUM(LineAmount) AS ContractSum                                               -- 176784.55
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType IN (0, 1, 2)
      AND LineType NOT IN (3, 4);
END
GO
