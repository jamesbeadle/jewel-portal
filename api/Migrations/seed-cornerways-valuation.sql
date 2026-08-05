-- ============================================================================
-- NOTE: CostCode values use the JBB Cost Code Master codes (trade-prefixed, per
-- JBB_CostCode_Master v2.1) seeded by seed-cost-centers.sql.
-- Seed: Cornerways East -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : Cornerways East, Ewell KT17 3ER
-- ProjectId: resolved at run time by site-name matcher 'cornerwayseastewellkt173er'
--
-- Seeds the ORIGINAL contract scope only, taken from the "Valuation 22 -
-- Retention Release" workbook. Three blocks make up the Contract Sum, as per
-- the Albany Mews pattern:
--
--     Contract works    GBP 394,389.00
--     Provisional Sums  GBP 197,495.00
--     Contingency        GBP 50,000.00
--     ----------------------------------
--     Contract Sum      GBP 641,884.00
--
-- Variations (V01..V84, net GBP 112,817.16, of which 14 VOs declined) are NOT
-- seeded here -- they belong in seed-cornerways-variations.sql. Per-valuation
-- claim history (Valuation 01..22, retention release) is claim data
-- (ValuationClaims/ClaimLines), not bill structure.
--
-- SectionCode/SectionName retain the workbook's NRM2 references; PS lines
-- retain their PC codes (PC1..PC18, workbook's own unpadded refs). The
-- workbook's own numeric codes (0001..0044) are dropped. CostCode maps each
-- section to the Jewel cost-centre master (seed-cost-centers.sql), consistent
-- with the Ravenswood / Albany seeds.
--
-- Two inline provisional sums stay in the contract-works block (LineType 1):
-- P30 "All associated work - Provisional sum" and N13 "Supply of sanitary
-- items - Provisional sum" (qty/rate given as 'PS' -> Quantity 1, Rate =
-- amount). "Omit item Vnn" comments are informational: those lines are
-- omitted by variations in the register, so they stay Priced/ProvisionalSum
-- here, comments copied verbatim (note: the workbook tags the sanitary PS
-- "Omit Item V42" although the register's actual omission is in V41; kept
-- verbatim).
--
-- Skipped rows: none -- every workbook contract row carries a value, and the
-- block reconciles to the stated Contract Sum exactly.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (ce-cw-NNN / ce-ps-NN
-- / ce-cont-NN). A re-run refreshes every field via MERGE. Variation lines for
-- this project are left untouched. Safe to run repeatedly.
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
    MERGE INTO [dbo].[ValuationLineItems] AS target
    USING (VALUES
    (N'ce-cw-001', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'SCAFF-STD', N'Scaffolding', N'm2', 150.0000, 34.0000, 5100.0000, N'', 1),
    (N'ce-cw-002', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WPR', N'Temporary covering / weather protection', N'item', 1.0000, 750.0000, 750.0000, N'', 2),
    (N'ce-cw-003', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site Supervision', N'week', 40.0000, 1250.0000, 50000.0000, N'', 3),
    (N'ce-cw-004', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Rubbish removal', N'week', 40.0000, 345.0000, 13800.0000, N'', 4),
    (N'ce-cw-005', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-PRO', N'General protection', N'item', 1.0000, 760.0000, 760.0000, N'', 5),
    (N'ce-cw-006', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'ELE-STD', N'Temporary plumbing & electrics', N'item', 1.0000, 1500.0000, 1500.0000, N'', 6),
    (N'ce-cw-007', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-HSC', N'CDM', N'item', 1.0000, 1200.0000, 1200.0000, N'', 7),
    (N'ce-cw-008', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'item', 40.0000, 125.0000, 5000.0000, N'', 8),
    (N'ce-cw-009', @ProjectId, 0, N'A10', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Clean on completion', N'item', 1.0000, 600.0000, 600.0000, N'', 9),
    (N'ce-cw-010', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Isolate electrical installation in connection with demolitions & strip out as required', N'item', 1.0000, 750.0000, 750.0000, N'', 10),
    (N'ce-cw-011', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Ditto plumbing & heating installation', N'item', 1.0000, 900.0000, 900.0000, N'', 11),
    (N'ce-cw-012', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'UTIL-STD', N'Remove existing electric / gas meters', N'item', 1.0000, 750.0000, 750.0000, N'', 12),
    (N'ce-cw-013', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove flooring, tiles, skirtings, picture rail, cove, fittings etc.', N'item', 1.0000, 600.0000, 600.0000, N'', 13),
    (N'ce-cw-014', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove kitchen & utility units, worktops & appliances', N'item', 1.0000, 295.0000, 295.0000, N'', 14),
    (N'ce-cw-015', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove sanitary items from WCs & bathrooms', N'item', 1.0000, 450.0000, 450.0000, N'', 15),
    (N'ce-cw-016', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove internal doors & frames', N'nr', 26.0000, 22.0000, 572.0000, N'', 16),
    (N'ce-cw-017', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal single skin walls', N'm2', 42.0000, 44.0000, 1848.0000, N'', 17),
    (N'ce-cw-018', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal double skin walls', N'm2', 12.0000, 82.0000, 984.0000, N'', 18),
    (N'ce-cw-019', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove section of plasterboard ceiling', N'm2', 25.0000, 14.0000, 350.0000, N'', 19),
    (N'ce-cw-020', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove existing staircase, balustrade, etc', N'nr', 1.0000, 375.0000, 375.0000, N'', 20),
    (N'ce-cw-021', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove windows & external doors throughout', N'item', 1.0000, 775.0000, 775.0000, N'', 21),
    (N'ce-cw-022', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-STS', N'Erect temporary propping to existing construction', N'm', 15.0000, 80.0000, 1200.0000, N'', 22),
    (N'ce-cw-023', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish external walls to form new layout', N'm2', 16.0000, 110.0000, 1760.0000, N'', 23),
    (N'ce-cw-024', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove section of roof covering & construction', N'm2', 10.0000, 24.0000, 240.0000, N'', 24),
    (N'ce-cw-025', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Strip back existing flat roof covering', N'm2', 38.0000, 16.0000, 608.0000, N'', 25),
    (N'ce-cw-026', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish outbuilding - complete', N'item', 1.0000, 499.0000, 499.0000, N'', 26),
    (N'ce-cw-027', @ProjectId, 0, N'C20', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove existing paving, shrubs, etc to areas of new work', N'm2', 30.0000, 12.0000, 360.0000, N'', 27),
    (N'ce-cw-028', @ProjectId, 0, N'D20', N'Excavation & filling', N'', N'', 0, N'SUB-EXC', N'Excavate to reduce levels & remove spoil : to new areas', N'm3', 8.0000, 125.0000, 1000.0000, N'Omit Item V07', 28),
    (N'ce-cw-029', @ProjectId, 0, N'D20', N'Excavation & filling', N'', N'', 0, N'SUB-EXC', N'Excavate foundations 600 x 1000 mm & remove spoil', N'm3', 14.0000, 170.0000, 2380.0000, N'Omit Item V07', 29),
    (N'ce-cw-030', @ProjectId, 0, N'P30', N'Trenches, pipeways for engineering services', N'', N'', 1, N'UTIL-TRN', N'All associated work - Provisional sum', N'item', 1.0000, 1500.0000, 1500.0000, N'Omit Item V08', 30),
    (N'ce-cw-031', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-DRN', N'Aco slot drains', N'm', 20.0000, 132.0000, 2640.0000, N'', 31),
    (N'ce-cw-032', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 3000.0000, 3000.0000, N'', 32),
    (N'ce-cw-033', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-DRN', N'Make connection into existing runs', N'item', 1.0000, 250.0000, 250.0000, N'', 33),
    (N'ce-cw-034', @ProjectId, 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'SUB-DRN', N'New soakaway', N'nr', 1.0000, 1150.0000, 1150.0000, N'', 34),
    (N'ce-cw-035', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Stub stack & durgo valve', N'nr', 4.0000, 130.0000, 520.0000, N'', 35),
    (N'ce-cw-036', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Soil vent pipework', N'item', 1.0000, 750.0000, 750.0000, N'', 36),
    (N'ce-cw-037', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'Wastes connections', N'nr', 18.0000, 88.0000, 1584.0000, N'', 37),
    (N'ce-cw-038', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 3500.0000, 3500.0000, N'', 38),
    (N'ce-cw-039', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'SUB-CON', N'Precast lintels over drainage runs', N'item', 1.0000, 400.0000, 400.0000, N'', 39),
    (N'ce-cw-040', @ProjectId, 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'MEC-DRN', N'New manhole', N'nr', 1.0000, 725.0000, 725.0000, N'', 40),
    (N'ce-cw-041', @ProjectId, 0, N'E10', N'In situ concrete', N'', N'', 0, N'SUB-CON', N'Concrete in foundations', N'm3', 12.0000, 180.0000, 2160.0000, N'Omit Item V07', 41),
    (N'ce-cw-042', @ProjectId, 0, N'E60', N'Pre cast concrete floors', N'', N'', 0, N'SUB-CON', N'100 mm concrete oversite', N'm2', 25.0000, 24.0000, 600.0000, N'', 42),
    (N'ce-cw-043', @ProjectId, 0, N'E60', N'Pre cast concrete floors', N'', N'', 0, N'INT-INF', N'165 mm polystyrene insulation', N'm2', 25.0000, 36.0000, 900.0000, N'', 43),
    (N'ce-cw-044', @ProjectId, 0, N'E60', N'Pre cast concrete floors', N'', N'', 0, N'SUB-CON', N'150 mm beam & block flooring', N'm2', 25.0000, 108.0000, 2700.0000, N'', 44),
    (N'ce-cw-045', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'WPF-DMP', N'Cavity walls below dpc in two skins of 100 mm blockwork & lean mix cavity fill', N'm2', 6.0000, 182.0000, 1092.0000, N'', 45),
    (N'ce-cw-046', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'INT-INW', N'Cavity wall in two skins of 100 mm blockwork with 60 mm Kingspan insulation to cavity', N'm2', 46.0000, 192.0000, 8832.0000, N'', 46),
    (N'ce-cw-047', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'INT-INW', N'Cavity wall in matching facing brickwork, 60 mm Kingspan insulation & 100 mm blockwork internal skin', N'm2', 8.0000, 214.0000, 1712.0000, N'', 47),
    (N'ce-cw-048', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'Separate cost for supply of facing bricks (£2.50 each)', N'm2', 8.0000, 150.0000, 1200.0000, N'', 48),
    (N'ce-cw-049', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'Alterations to existing openings & making good reveals', N'item', 1.0000, 400.0000, 400.0000, N'', 49),
    (N'ce-cw-050', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'Wall extension profiles', N'm', 22.0000, 32.0000, 704.0000, N'', 50),
    (N'ce-cw-051', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'Thermabate cavity closers', N'm', 30.0000, 22.0000, 660.0000, N'', 51),
    (N'ce-cw-052', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'IG L1/S lintel & tray over new openings', N'm', 8.0000, 122.0000, 976.0000, N'', 52),
    (N'ce-cw-053', @ProjectId, 0, N'F1', N'Masonry walling', N'', N'', 0, N'MASON-BRK', N'Air vent bricks', N'item', 1.0000, 300.0000, 300.0000, N'', 53),
    (N'ce-cw-054', @ProjectId, 0, N'J40', N'Flexible sheet waterproofing', N'', N'', 0, N'WPF-DMP', N'Damp proof membranes 1200 g', N'm2', 25.0000, 14.0000, 350.0000, N'', 54),
    (N'ce-cw-055', @ProjectId, 0, N'K1', N'Floors', N'', N'', 0, N'CARP-1FX', N'50 x 100 mm timber floor joists to old garage', N'm', 82.0000, 34.0000, 2788.0000, N'', 55),
    (N'ce-cw-056', @ProjectId, 0, N'K1', N'Floors', N'', N'', 0, N'INT-INF', N'100 mm Kingspan insulation to floor', N'm2', 25.0000, 36.0000, 900.0000, N'', 56),
    (N'ce-cw-057', @ProjectId, 0, N'K1', N'Floors', N'', N'', 0, N'CARP-1FX', N'Joist hangers', N'nr', 28.0000, 5.0000, 140.0000, N'', 57),
    (N'ce-cw-058', @ProjectId, 0, N'K1', N'Floors', N'', N'', 0, N'CARP-1FX', N'Galvanised restraint straps', N'nr', 12.0000, 16.0000, 192.0000, N'', 58),
    (N'ce-cw-059', @ProjectId, 0, N'K11', N'Rigid sheet flooring/sheathing/decking', N'', N'', 0, N'ROOF-FLT', N'18 mm plywood over firings to flat roof deck', N'm2', 24.0000, 46.0000, 1104.0000, N'', 59),
    (N'ce-cw-060', @ProjectId, 0, N'K11', N'Rigid sheet flooring/sheathing/decking', N'', N'', 0, N'CARP-1FX', N'22 mm T&G chipboard flooring to old garage', N'm2', 25.0000, 28.0000, 700.0000, N'', 60),
    (N'ce-cw-061', @ProjectId, 0, N'K11', N'Rigid sheet flooring/sheathing/decking', N'', N'', 0, N'CARP-1FX', N'18 mm plywood to stud walls', N'm2', 18.0000, 22.0000, 396.0000, N'', 61),
    (N'ce-cw-062', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'CARP-CUT', N'50 x 150 mm timber roof rafters', N'm', 68.0000, 30.0000, 2040.0000, N'', 62),
    (N'ce-cw-063', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'CARP-CUT', N'50 x 100 mm timber roof rafters', N'm', 20.0000, 24.0000, 480.0000, N'', 63),
    (N'ce-cw-064', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'CARP-CUT', N'50 x 100 mm timber stud with 9 mm OSB to dormer', N'm2', 3.0000, 92.0000, 276.0000, N'', 64),
    (N'ce-cw-065', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'CARP-CUT', N'Timber lintel over openings', N'm', 2.0000, 32.0000, 64.0000, N'', 65),
    (N'ce-cw-066', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-TLN', N'Breatherable membrane, battens & matching tiles', N'm2', 28.0000, 102.0000, 2856.0000, N'', 66),
    (N'ce-cw-067', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-TLN', N'Ridge / hip tiles', N'm', 8.0000, 65.0000, 520.0000, N'', 67),
    (N'ce-cw-068', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-TLN', N'Separate cost for supply of tiles (£1.20 each)', N'm2', 28.0000, 72.0000, 2016.0000, N'', 68),
    (N'ce-cw-069', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-RFR', N'Roof ventilators', N'nr', 2.0000, 85.0000, 170.0000, N'', 69),
    (N'ce-cw-070', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-LED', N'Lead flashing / valley lining', N'm', 20.0000, 56.0000, 1120.0000, N'', 70),
    (N'ce-cw-071', @ProjectId, 0, N'H1', N'Roofing', N'', N'', 0, N'ROOF-FSU', N'Fascia / soffit', N'm', 16.0000, 46.0000, 736.0000, N'', 71),
    (N'ce-cw-072', @ProjectId, 0, N'R10', N'Rainwater drainage systems', N'', N'', 0, N'ROOF-GRU', N'Hopper heads', N'nr', 2.0000, 130.0000, 260.0000, N'', 72),
    (N'ce-cw-073', @ProjectId, 0, N'R10', N'Rainwater drainage systems', N'', N'', 0, N'ROOF-GRU', N'Guttering', N'm', 28.0000, 32.0000, 896.0000, N'', 73),
    (N'ce-cw-074', @ProjectId, 0, N'R10', N'Rainwater drainage systems', N'', N'', 0, N'ROOF-GRU', N'Rainwater pipework', N'm', 22.0000, 34.0000, 748.0000, N'', 74),
    (N'ce-cw-075', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'CARP-1FX', N'Timber wall plate bolted to wall', N'm', 10.0000, 34.0000, 340.0000, N'', 75),
    (N'ce-cw-076', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'ROOF-FLT', N'50 x 150 mm timber roof joists', N'm', 68.0000, 30.0000, 2040.0000, N'', 76),
    (N'ce-cw-077', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'CARP-1FX', N'Joist hangers', N'nr', 16.0000, 5.0000, 80.0000, N'', 77),
    (N'ce-cw-078', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'CARP-1FX', N'Galvanised restraint straps', N'nr', 8.0000, 16.0000, 128.0000, N'', 78),
    (N'ce-cw-079', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'ROOF-FLT', N'Form secret gutter', N'm', 5.0000, 40.0000, 200.0000, N'', 79),
    (N'ce-cw-080', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'ROOF-FLT', N'Sarnifli single ply membrane', N'm2', 24.0000, 132.0000, 3168.0000, N'', 80),
    (N'ce-cw-081', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'ROOF-LED', N'Lead flashing / valley lining', N'm', 6.0000, 56.0000, 336.0000, N'', 81),
    (N'ce-cw-082', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'ROOF-FLT', N'Single ply to secret gutter', N'm', 5.0000, 50.0000, 250.0000, N'', 82),
    (N'ce-cw-083', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'EXTW-DEK', N'Pedestal system & decking to terrace', N'm2', 24.0000, 140.0000, 3360.0000, N'Omit Item V26', 83),
    (N'ce-cw-084', @ProjectId, 0, N'J42', N'Flat roofing', N'', N'', 0, N'STR-GRL', N'1100 mm glass balustrade to terrace', N'm', 14.0000, 275.0000, 3850.0000, N'Omit Item V26', 84),
    (N'ce-cw-085', @ProjectId, 0, N'H21', N'Timber weatherboarding', N'', N'', 0, N'CARP-1FX', N'Form timber & plywood upstand', N'm2', 6.0000, 58.0000, 348.0000, N'', 85),
    (N'ce-cw-086', @ProjectId, 0, N'H21', N'Timber weatherboarding', N'', N'', 0, N'CARP-1FX', N'Form timber & plywood upstand', N'm2', 50.0000, 118.0000, 5900.0000, N'Omit Item V40', 86),
    (N'ce-cw-087', @ProjectId, 0, N'H72', N'Aluminium strip / sheet covering', N'', N'', 0, N'EXT-MCP', N'Alumasc skyline aluminium copping', N'm', 13.0000, 90.0000, 1170.0000, N'', 87),
    (N'ce-cw-088', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W01 - 450 x 1015 mm powder coated', N'nr', 1.0000, 295.0000, 295.0000, N'', 88),
    (N'ce-cw-089', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W02 & 04 - 550 x 560 mm powder coated', N'nr', 2.0000, 185.0000, 370.0000, N'', 89),
    (N'ce-cw-090', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W03 - 1055 x 560 mm powder coated', N'nr', 1.0000, 354.0000, 354.0000, N'', 90),
    (N'ce-cw-091', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W05 - 1360 x 550 mm powder coated', N'nr', 1.0000, 450.0000, 450.0000, N'', 91),
    (N'ce-cw-092', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W06 - 1090 x 1015 mm powder coated', N'nr', 1.0000, 665.0000, 665.0000, N'', 92),
    (N'ce-cw-093', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W07 & 08 - 695 x 1510 mm powder coated', N'nr', 2.0000, 630.0000, 1260.0000, N'', 93),
    (N'ce-cw-094', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W09 - 12 - 1610 x 130 mm powder coated', N'nr', 4.0000, 1365.0000, 5460.0000, N'', 94),
    (N'ce-cw-095', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W13 - 2145 x 1310 mm powder coated', N'nr', 1.0000, 1685.0000, 1685.0000, N'', 95),
    (N'ce-cw-096', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W14 - 955 x 3325 mm powder coated', N'nr', 1.0000, 1905.0000, 1905.0000, N'', 96),
    (N'ce-cw-097', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W15 - 2485 x 2000 mm powder coated', N'nr', 1.0000, 2982.0000, 2982.0000, N'', 97),
    (N'ce-cw-098', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W16 & 17 - 450 x 1015 mm powder coated', N'nr', 2.0000, 274.0000, 548.0000, N'', 98),
    (N'ce-cw-099', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W18 - 1575 x 1310 mm powder coated', N'nr', 1.0000, 1238.0000, 1238.0000, N'', 99),
    (N'ce-cw-100', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W19 - 1115 x 1310 mm powder coated', N'nr', 1.0000, 904.0000, 904.0000, N'', 100),
    (N'ce-cw-101', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W20 - 740 x 1310 mm powder coated', N'nr', 1.0000, 585.0000, 585.0000, N'', 101),
    (N'ce-cw-102', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W21 - 2270 x 1310 mm powder coated', N'nr', 1.0000, 1784.0000, 1784.0000, N'', 102),
    (N'ce-cw-103', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W22 & 23 - 1610 x 1310 mm powder coated', N'nr', 2.0000, 1265.0000, 2530.0000, N'', 103),
    (N'ce-cw-104', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-ALU', N'W24 - 2145 x 1310 mm powder coated', N'nr', 1.0000, 1685.0000, 1685.0000, N'', 104),
    (N'ce-cw-105', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-SPG', N'Velux 1200 x 1200 mm flat roof light - RL01/RL02', N'nr', 2.0000, 1655.0000, 3310.0000, N'Omit Item V12', 105),
    (N'ce-cw-106', @ProjectId, 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'WDR-SPG', N'780 x 980 mm roof light - RL03', N'nr', 1.0000, 1200.0000, 1200.0000, N'Omit item V82', 106),
    (N'ce-cw-107', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'WDR-ALU', N'EXD01 - 2485 x 2400 mm aluminium door', N'nr', 1.0000, 5070.0000, 5070.0000, N'Omit Item V43', 107),
    (N'ce-cw-108', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'WDR-ALU', N'EXD02 - 1022 x 2110 mm aluminium door', N'nr', 1.0000, 1835.0000, 1835.0000, N'Omit Item V43', 108),
    (N'ce-cw-109', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'WDR-ALU', N'EXD03 - 1820 x 2110 mm aluminium door', N'nr', 1.0000, 3250.0000, 3250.0000, N'', 109),
    (N'ce-cw-110', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'WDR-ALU', N'EXD04 - 3395 x 2135 mm aluminium door', N'nr', 1.0000, 6150.0000, 6150.0000, N'', 110),
    (N'ce-cw-111', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'WDR-ALU', N'EXD05 - 2180 x 2205 mm aluminium door', N'nr', 1.0000, 4085.0000, 4085.0000, N'', 111),
    (N'ce-cw-112', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'SUP-DOR', N'926 mm Internal door lining & single door ( £120 supply )', N'nr', 12.0000, 325.0000, 3900.0000, N'', 112),
    (N'ce-cw-113', @ProjectId, 0, N'L20', N'Doors', N'', N'', 0, N'SUP-DOR', N'Internal door lining & double door', N'nr', 2.0000, 675.0000, 1350.0000, N'', 113),
    (N'ce-cw-114', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'CARP-1FX', N'38 x 89 mm timber internal stud walls', N'm2', 62.0000, 56.0000, 3472.0000, N'', 114),
    (N'ce-cw-115', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INC', N'120 mm Kingspan insulation to flat roof deck joists', N'm2', 24.0000, 38.0000, 912.0000, N'', 115),
    (N'ce-cw-116', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INC', N'100 mm Kingspan insulation between roof rafters', N'm2', 26.0000, 36.0000, 936.0000, N'', 116),
    (N'ce-cw-117', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INC', N'30 mm Kingspan insulation under rafters', N'm2', 26.0000, 22.0000, 572.0000, N'', 117),
    (N'ce-cw-118', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INC', N'300 mm mineral insulation to eaves space', N'm2', 75.0000, 40.0000, 3000.0000, N'', 118),
    (N'ce-cw-119', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INF', N'100 mm mineral insulation between floor joists', N'm2', 5.0000, 28.0000, 140.0000, N'', 119),
    (N'ce-cw-120', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-PLB', N'15mm plasterboard to ceilings', N'm2', 155.0000, 22.0000, 3410.0000, N'', 120),
    (N'ce-cw-121', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INW', N'100 mm Kingspan to dormer walls', N'm2', 2.0000, 36.0000, 72.0000, N'', 121),
    (N'ce-cw-122', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INW', N'50 mm rockwool insulation between stud walls', N'm2', 62.0000, 16.0000, 992.0000, N'', 122),
    (N'ce-cw-123', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INW', N'92.5 mm Kingspan insulation to garage walls', N'm2', 22.0000, 36.0000, 792.0000, N'', 123),
    (N'ce-cw-124', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-INW', N'52.5 mm Kingspan insulation to dormer walls', N'm2', 2.0000, 32.0000, 64.0000, N'', 124),
    (N'ce-cw-125', @ProjectId, 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'INT-PLB', N'15 mm plasterboard to walls', N'm2', 268.0000, 22.0000, 5896.0000, N'', 125),
    (N'ce-cw-126', @ProjectId, 0, N'M10', N'Cement based levelling screeds', N'', N'', 0, N'FLR-SCR', N'75 mm sand / cement floor screed', N'm2', 25.0000, 75.0000, 1875.0000, N'', 126),
    (N'ce-cw-127', @ProjectId, 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-PLS', N'3 mm skim to ceilings', N'm2', 155.0000, 28.0000, 4340.0000, N'', 127),
    (N'ce-cw-128', @ProjectId, 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'INT-PLS', N'3 mm skim to new & existing walls', N'm2', 268.0000, 26.0000, 6968.0000, N'', 128),
    (N'ce-cw-129', @ProjectId, 0, N'P10', N'Sundry insulation', N'', N'', 0, N'CARP-1FX', N'Plywood boxing & insulation to internal pipes', N'item', 1.0000, 500.0000, 500.0000, N'', 129),
    (N'ce-cw-130', @ProjectId, 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF architraves to new door frames', N'm', 120.0000, 12.0000, 1440.0000, N'', 130),
    (N'ce-cw-131', @ProjectId, 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF skirting to walls to new walls (£8/m supply)', N'm', 122.0000, 24.0000, 2928.0000, N'', 131),
    (N'ce-cw-132', @ProjectId, 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'CARP-2FX', N'MDF window boards', N'm', 28.0000, 36.0000, 1008.0000, N'', 132),
    (N'ce-cw-133', @ProjectId, 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'MEC-PLM', N'Hot & cold water supply', N'nr', 30.0000, 155.0000, 4650.0000, N'', 133),
    (N'ce-cw-134', @ProjectId, 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'MEC-PLM', N'Water softener', N'nr', 1.0000, 855.0000, 855.0000, N'', 134),
    (N'ce-cw-135', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-BLR', N'New central heating boiler & associated pipework', N'nr', 1.0000, 4450.0000, 4450.0000, N'', 135),
    (N'ce-cw-136', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'UTIL-STD', N'Relocate gas meter', N'nr', 1.0000, 750.0000, 750.0000, N'', 136),
    (N'ce-cw-137', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-PLM', N'Megaflow hot water cylinder', N'nr', 1.0000, 2980.0000, 2980.0000, N'', 137),
    (N'ce-cw-138', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-PLM', N'Radiators with TRVs', N'nr', 5.0000, 525.0000, 2625.0000, N'', 138),
    (N'ce-cw-139', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-PLM', N'Towel rails with TRVS', N'nr', 2.0000, 550.0000, 1100.0000, N'', 139),
    (N'ce-cw-140', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-UFH', N'Wet underfloor heating', N'm2', 68.0000, 145.0000, 9860.0000, N'', 140),
    (N'ce-cw-141', @ProjectId, 0, N'T90', N'Heating systems', N'', N'', 0, N'MEC-UFH', N'Manifold & thermostats', N'nr', 3.0000, 450.0000, 1350.0000, N'', 141),
    (N'ce-cw-142', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 1, N'SUP-SAN', N'Supply of sanitary items - Provisional sum', N'item', 1.0000, 6000.0000, 6000.0000, N'Omit Item V42', 142),
    (N'ce-cw-143', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-SAN', N'Fix only - WC', N'nr', 2.0000, 278.0000, 556.0000, N'', 143),
    (N'ce-cw-144', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-SAN', N'Fix only - Wash hand basin', N'nr', 2.0000, 260.0000, 520.0000, N'', 144),
    (N'ce-cw-145', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-SAN', N'Fix only - Showers / baths', N'nr', 2.0000, 480.0000, 960.0000, N'', 145),
    (N'ce-cw-146', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'SUP-SAN', N'Fix only - Mirrors, towel rails, hooks, etc', N'item', 1.0000, 300.0000, 300.0000, N'', 146),
    (N'ce-cw-147', @ProjectId, 0, N'N13', N'Sanitary appliances and fittings', N'', N'', 0, N'WPF-INT', N'Wet room system / tanking', N'item', 1.0000, 800.0000, 800.0000, N'', 147),
    (N'ce-cw-148', @ProjectId, 0, N'U90', N'General ventilation', N'', N'', 0, N'MEC-VNT', N'Extract fan', N'nr', 7.0000, 275.0000, 1925.0000, N'', 148),
    (N'ce-cw-149', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Consumer unit', N'nr', 1.0000, 1150.0000, 1150.0000, N'', 149),
    (N'ce-cw-150', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'UTIL-STD', N'Relocate electric meter', N'nr', 1.0000, 750.0000, 750.0000, N'', 150),
    (N'ce-cw-151', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Double socket outlet', N'nr', 70.0000, 118.0000, 8260.0000, N'', 151),
    (N'ce-cw-152', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Double socket outlet with USB', N'nr', 18.0000, 124.0000, 2232.0000, N'', 152),
    (N'ce-cw-153', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'External double socket', N'nr', 2.0000, 120.0000, 240.0000, N'', 153),
    (N'ce-cw-154', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Fused spurs', N'nr', 10.0000, 100.0000, 1000.0000, N'', 154),
    (N'ce-cw-155', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Cooker switch', N'nr', 1.0000, 120.0000, 120.0000, N'', 155),
    (N'ce-cw-156', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Shavers socket', N'nr', 5.0000, 95.0000, 475.0000, N'', 156),
    (N'ce-cw-157', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Recessed light fitting', N'nr', 122.0000, 108.0000, 13176.0000, N'', 157),
    (N'ce-cw-158', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Ip batten light', N'nr', 1.0000, 125.0000, 125.0000, N'', 158),
    (N'ce-cw-159', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Pendant lights', N'nr', 2.0000, 455.0000, 910.0000, N'', 159),
    (N'ce-cw-160', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'External lights', N'nr', 12.0000, 175.0000, 2100.0000, N'', 160),
    (N'ce-cw-161', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 42.0000, 40.0000, 1680.0000, N'', 161),
    (N'ce-cw-162', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Fan isolator switches', N'nr', 7.0000, 95.0000, 665.0000, N'', 162),
    (N'ce-cw-163', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Smoke/heat detector', N'nr', 5.0000, 150.0000, 750.0000, N'', 163),
    (N'ce-cw-164', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Carbon monoxide detector', N'nr', 1.0000, 118.0000, 118.0000, N'', 164),
    (N'ce-cw-165', @ProjectId, 0, N'V90', N'Electrical systems', N'', N'', 0, N'ELE-STD', N'Builders work in connection with electrical installation', N'item', 1.0000, 500.0000, 500.0000, N'', 165),
    (N'ce-cw-166', @ProjectId, 0, N'W90', N'Communications & security systems', N'', N'', 0, N'PRELIMS-SEC', N'All associated works', N'nr', 1.0000, 7500.0000, 7500.0000, N'', 166),
    (N'ce-cw-167', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 200.0000, 18.0000, 3600.0000, N'', 167),
    (N'ce-cw-168', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Ditto walls', N'm2', 350.0000, 16.0000, 5600.0000, N'', 168),
    (N'ce-cw-169', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'WDR-TIM', N'Prepare & decorate doors', N'm2', 54.0000, 32.0000, 1728.0000, N'', 169),
    (N'ce-cw-170', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'CARP-2FX', N'Frames, architrave, window board & skirtings', N'm', 322.0000, 7.0000, 2254.0000, N'', 170),
    (N'ce-cw-171', @ProjectId, 0, N'M60', N'Painting', N'', N'', 0, N'DEC-STD', N'Prepare & decorate new staircase', N'item', 1.0000, 1200.0000, 1200.0000, N'', 171),
    (N'ce-cw-172', @ProjectId, 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-LVT', N'Karndean vinyl flooring (£40 supply)', N'm2', 168.0000, 95.0000, 15960.0000, N'Omit item V55', 172),
    (N'ce-cw-173', @ProjectId, 0, N'M50', N'Rubber/ plastics/ cork/ lino/ carpet tiling/ sheeting', N'', N'', 0, N'FLR-LVT', N'Altro vinyl flooring (£40 supply)', N'm2', 32.0000, 95.0000, 3040.0000, N'', 173),
    (N'ce-cw-174', @ProjectId, 0, N'M51', N'Carpet', N'', N'', 0, N'FLR-CPT', N'Underlay & carpet (£40 supply)', N'm2', 10.0000, 60.0000, 600.0000, N'Omit item V55', 174),
    (N'ce-cw-175', @ProjectId, 0, N'Q22', N'Ashpalt roads/pavings', N'', N'', 0, N'EXTW-PAV', N'Grade tarmac drive to give level entrance', N'item', 1.0000, 750.0000, 750.0000, N'', 175),
    (N'ce-cw-176', @ProjectId, 0, N'Q22', N'Ashpalt roads/pavings', N'', N'', 0, N'EXTW-DEK', N'Timber decking', N'm2', 45.0000, 155.0000, 6975.0000, N'Omit item V57', 176),
    (N'ce-cw-177', @ProjectId, 0, N'Q22', N'Ashpalt roads/pavings', N'', N'', 0, N'EXTW-DEK', N'Balustrade & steps', N'item', 1.0000, 3500.0000, 3500.0000, N'Omit item V57', 177),
    (N'ce-ps-01', @ProjectId, 1, N'PC1', N'Provisional Sums', N'', N'', 1, N'ENABLE-ASB', N'Asbestos removal', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit Item V01', 1),
    (N'ce-ps-02', @ProjectId, 1, N'PC2', N'Provisional Sums', N'', N'', 1, N'MEC-DRN', N'Drainage remedial work', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit Item V03', 2),
    (N'ce-ps-03', @ProjectId, 1, N'PC3', N'Provisional Sums', N'', N'', 1, N'STR-STL', N'Structural enginneering items', N'item', 1.0000, 33000.0000, 33000.0000, N'Omit Item V10', 3),
    (N'ce-ps-04', @ProjectId, 1, N'PC4', N'Provisional Sums', N'', N'', 1, N'SUB-UND', N'Underpinning', N'item', 1.0000, 6500.0000, 6500.0000, N'Omit Item V05', 4),
    (N'ce-ps-05', @ProjectId, 1, N'PC5', N'Provisional Sums', N'', N'', 1, N'WPF-DMP', N'Remedial damp proofing work', N'item', 1.0000, 1650.0000, 1650.0000, N'Omit item V60', 5),
    (N'ce-ps-06', @ProjectId, 1, N'PC6', N'Provisional Sums', N'', N'', 1, N'HAND-CLE', N'Cleaning exitsing roof coverings', N'item', 1.0000, 2150.0000, 2150.0000, N'Omit item V60', 6),
    (N'ce-ps-07', @ProjectId, 1, N'PC7', N'Provisional Sums', N'', N'', 1, N'SUP-TIL', N'Wall tiling', N'item', 1.0000, 7800.0000, 7800.0000, N'Omit Item V49', 7),
    (N'ce-ps-08', @ProjectId, 1, N'PC8', N'Provisional Sums', N'', N'', 1, N'SUP-IRO', N'Internal door ironmongery', N'item', 1.0000, 1895.0000, 1895.0000, N'Omit item V67', 8),
    (N'ce-ps-09', @ProjectId, 1, N'PC9', N'Provisional Sums', N'', N'', 1, N'SUP-KIT', N'New kitchen units & appliances', N'item', 1.0000, 26000.0000, 26000.0000, N'Omit Item V36', 9),
    (N'ce-ps-10', @ProjectId, 1, N'PC10', N'Provisional Sums', N'', N'', 1, N'SUP-KIT', N'New utility units & appliances', N'item', 1.0000, 6500.0000, 6500.0000, N'Omit Item V36', 10),
    (N'ce-ps-11', @ProjectId, 1, N'PC11', N'Provisional Sums', N'', N'', 1, N'WIN-BLD', N'Blinds & Curtains', N'item', 1.0000, 17500.0000, 17500.0000, N'Omit item V56', 11),
    (N'ce-ps-12', @ProjectId, 1, N'PC12', N'Provisional Sums', N'', N'', 1, N'ELE-ALM', N'Alarm System', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit item V16', 12),
    (N'ce-ps-13', @ProjectId, 1, N'PC13', N'Provisional Sums', N'', N'', 1, N'ELE-FIR', N'Fire & Smoke detection', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit Item V14', 13),
    (N'ce-ps-14', @ProjectId, 1, N'PC14', N'Provisional Sums', N'', N'', 1, N'EXTW-LND', N'Soft landscaping', N'item', 1.0000, 7500.0000, 7500.0000, N'Omit item V57', 14),
    (N'ce-ps-15', @ProjectId, 1, N'PC15', N'Provisional Sums', N'', N'', 1, N'CARP-WRD', N'Wardrobes, shelving, storage', N'item', 1.0000, 20000.0000, 20000.0000, N'Omit item V75', 15),
    (N'ce-ps-16', @ProjectId, 1, N'PC16', N'Provisional Sums', N'', N'', 1, N'ELE-CCT', N'CCTV', N'item', 1.0000, 5500.0000, 5500.0000, N'Omit Item V19', 16),
    (N'ce-ps-17', @ProjectId, 1, N'PC17', N'Provisional Sums', N'', N'', 1, N'STAIR-TIM', N'Stairs', N'item', 1.0000, 12000.0000, 12000.0000, N'Omit Item V48', 17),
    (N'ce-ps-18', @ProjectId, 1, N'PC18', N'Provisional Sums', N'', N'', 1, N'SPEC-LFT', N'Supply & install ceiling hoists', N'item', 1.0000, 27500.0000, 27500.0000, N'Omit item V29', 18),
    (N'ce-cont-01', @ProjectId, 2, N'', N'Contingency', N'', N'', 0, N'HAND-MSC', N'Contingency Budget', N'item', 1.0000, 50000.0000, 50000.0000, N'Omit item V58', 1)
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

    PRINT 'Cornerways East: contract valuation lines merged.';
    COMMIT TRAN;

    -- Sanity check: the three seeded blocks should reconcile to the workbook.
    SELECT
        SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 394389.00
        SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         -- 197495.00
        SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --  50000.00
        SUM(LineAmount) AS ContractSum                                               -- 641884.00
    FROM [dbo].[ValuationLineItems]
    WHERE ProjectId = @ProjectId AND ElementType IN (0, 1, 2)
      AND LineType NOT IN (3, 4);
END
GO
