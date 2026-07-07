-- ============================================================================
-- NOTE: CostCode values use the canonical JBB Cost Code Master codes (v2.1, trade-prefixed) seeded
-- by seed-cost-centers.sql. Remapped from the original JBB-* buckets on
-- 2026-07-07 -- audit trail: scripts/cost-code-remap-review.csv.
-- Seed: Ravenswood Ave -- contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : 64 Ravenswood Avenue, KT6 7NP  (JBB-2026-003)
-- ProjectId: 3bf6dcfa81764a248138fb5fd357aa84
--
-- Seeds the CONTRACT SCOPE only, taken from the Valuation No.1 workbook.
-- A single Contract Works block makes up the Contract Sum; the workbook's
-- three "Provisional Sum" lines sit inline in the bill (LineType 1) rather
-- than in a separate PC block:
--
--     Contract works (incl. GBP 14,500.00 inline provisional sums)
--     Contract Sum      GBP 261,218.00
--
-- Variations (V01..V02, net GBP 5,915.00) are NOT seeded here -- they belong
-- in a separate variations seed, as per the By France pattern. Per-valuation
-- claim history (Valuation 01..09) and the 20% deposit mechanics are claim
-- data (ValuationClaims/ClaimLines), not bill structure.
--
-- The workbook has no NRM2 or section numbering; SectionCode is assigned
-- sequentially (01..20) in workbook order. The "External" sub-group is folded
-- into Electrics. CostCode maps each section to the Jewel cost-centre master
-- (JBB-*, from seed-cost-centers.sql).
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (rv-cw-NNN). A re-run
-- refreshes every field via MERGE. Variation lines for this project are left
-- untouched. Safe to run repeatedly.
-- ============================================================================

MERGE INTO [dbo].[ValuationLineItems] AS target
USING (VALUES
    (N'rv-cw-001', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SMG', N'Site manager', N'week', 18.0000, 1500.0000, 27000.0000, N'', 1),
    (N'rv-cw-002', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-PMG', N'Project manager', N'week', 18.0000, 550.0000, 9900.0000, N'', 2),
    (N'rv-cw-003', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-LAB', N'Site labour', N'week', 18.0000, 450.0000, 8100.0000, N'', 3),
    (N'rv-cw-004', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'ENABLE-SKP', N'Rubbish removal', N'skips', 9.0000, 380.0000, 3420.0000, N'', 4),
    (N'rv-cw-005', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-HSC', N'CDM', N'item', 1.0000, 1200.0000, 1200.0000, N'', 5),
    (N'rv-cw-006', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Health, safety & welfare', N'nr', 18.0000, 125.0000, 2250.0000, N'', 6),
    (N'rv-cw-007', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-HRD', N'Hoarding & protection', N'week', 32.0000, 125.0000, 4000.0000, N'', 7),
    (N'rv-cw-008', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'ELE-STD', N'Temporary plumbing & electrics', N'item', 1.0000, 1500.0000, 1500.0000, N'', 8),
    (N'rv-cw-009', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WC', N'Temporary toilet', N'week', 18.0000, 90.0000, 1620.0000, N'', 9),
    (N'rv-cw-010', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-WEL', N'Temporary welfare', N'week', 18.0000, 110.0000, 1980.0000, N'', 10),
    (N'rv-cw-011', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SET', N'Temporary kitchen set up', N'item', 1.0000, 350.0000, 350.0000, N'', 11),
    (N'rv-cw-012', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'PRELIMS-SEC', N'Site security', N'week', 18.0000, 75.0000, 1350.0000, N'', 12),
    (N'rv-cw-013', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'ELE-STD', N'Plant, lighting & machinery', N'week', 18.0000, 55.0000, 990.0000, N'', 13),
    (N'rv-cw-014', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'SCAFF-STD', N'Scaffolding', N'm2', 42.0000, 44.0000, 1848.0000, N'', 14),
    (N'rv-cw-015', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'01', N'Preliminaries', N'', N'', 0, N'HAND-CLI', N'Clean on completion', N'item', 1.0000, 1000.0000, 1000.0000, N'', 15),
    (N'rv-cw-016', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Isolate electrical installation in connection with demolitions & strip out as required', N'item', 1.0000, 500.0000, 500.0000, N'', 16),
    (N'rv-cw-017', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Ditto plumbing & heating installation', N'item', 1.0000, 650.0000, 650.0000, N'', 17),
    (N'rv-cw-018', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove tiles, skirtings, picture rail, cove, fittings etc.', N'item', 1.0000, 400.0000, 400.0000, N'', 18),
    (N'rv-cw-019', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove floor finishes', N'm2', 44.0000, 5.0000, 220.0000, N'', 19),
    (N'rv-cw-020', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove kitchen units, worktops & appliances', N'item', 1.0000, 180.0000, 180.0000, N'', 20),
    (N'rv-cw-021', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove sanitary items', N'item', 1.0000, 275.0000, 275.0000, N'', 21),
    (N'rv-cw-022', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove internal doors & frames', N'nr', 9.0000, 18.0000, 162.0000, N'', 22),
    (N'rv-cw-023', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-ASB', N'Asbestos removal', N'item', 1.0000, 1500.0000, 1500.0000, N'', 23),
    (N'rv-cw-024', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish internal walls to form new layout', N'm2', 10.0000, 42.0000, 420.0000, N'', 24),
    (N'rv-cw-025', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove section of plasterboard ceiling', N'm2', 48.0000, 12.0000, 576.0000, N'', 25),
    (N'rv-cw-026', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove windows & external doors', N'item', 1.0000, 240.0000, 240.0000, N'', 26),
    (N'rv-cw-027', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-STS', N'Erect temporary propping to existing construction', N'm', 8.0000, 125.0000, 1000.0000, N'', 27),
    (N'rv-cw-028', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Demolish external walls', N'm2', 9.0000, 85.0000, 765.0000, N'', 28),
    (N'rv-cw-029', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove garden shed & green house', N'item', 1.0000, 300.0000, 300.0000, N'', 29),
    (N'rv-cw-030', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'MEC-DRN', N'Remove redundant drainage', N'item', 1.0000, 420.0000, 420.0000, N'', 30),
    (N'rv-cw-031', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove existing paving / steps', N'm2', 25.0000, 12.0000, 300.0000, N'', 31),
    (N'rv-cw-032', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Remove external chimney stack & make good', N'item', 1.0000, 550.0000, 550.0000, N'', 32),
    (N'rv-cw-033', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'02', N'Demolition', N'', N'', 0, N'ENABLE-DEM', N'Internal wall opening up', N'item', 1.0000, 150.0000, 150.0000, N'', 33),
    (N'rv-cw-034', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'03', N'Excavation', N'', N'', 0, N'SUB-EXC', N'Trial pits', N'nr', 2.0000, 275.0000, 550.0000, N'', 34),
    (N'rv-cw-035', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'03', N'Excavation', N'', N'', 0, N'SUB-EXC', N'Excavate to reduce levels & remove spoil', N'm3', 7.0000, 125.0000, 875.0000, N'', 35),
    (N'rv-cw-036', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'03', N'Excavation', N'', N'', 0, N'SUB-EXC', N'Excavate foundations 450 x 1000 mm & remove spoil', N'm3', 1.5000, 160.0000, 240.0000, N'', 36),
    (N'rv-cw-037', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'03', N'Excavation', N'', N'', 0, N'SUB-EXC', N'Excavate foundations 600 x 1000 mm & remove spoil', N'm3', 9.0000, 160.0000, 1440.0000, N'', 37),
    (N'rv-cw-038', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'03', N'Excavation', N'', N'', 0, N'SUB-EXC', N'Excavate pad foundations  & remove spoil', N'm3', 2.0000, 210.0000, 420.0000, N'', 38),
    (N'rv-cw-039', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'03', N'Excavation', N'', N'', 0, N'SUB-EXC', N'Concrete in foundations', N'm3', 9.0000, 185.0000, 1665.0000, N'', 39),
    (N'rv-cw-040', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'03', N'Excavation', N'', N'', 0, N'SUB-EXC', N'A393 mesh to foundation base', N'm2', 12.0000, 34.0000, 408.0000, N'', 40),
    (N'rv-cw-041', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'04', N'Drainagae & Civil', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 2050.0000, 2050.0000, N'', 41),
    (N'rv-cw-042', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'04', N'Drainagae & Civil', N'', N'', 0, N'MEC-DRN', N'Make connection of new drainage to existing runs', N'item', 1.0000, 250.0000, 250.0000, N'', 42),
    (N'rv-cw-043', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'04', N'Drainagae & Civil', N'', N'', 0, N'MEC-DRN', N'New inspection chambers', N'nr', 1.0000, 645.0000, 645.0000, N'', 43),
    (N'rv-cw-044', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'04', N'Drainagae & Civil', N'', N'', 0, N'MEC-DRN', N'Stub stack & durgo valve', N'nr', 2.0000, 130.0000, 260.0000, N'', 44),
    (N'rv-cw-045', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'04', N'Drainagae & Civil', N'', N'', 0, N'MEC-DRN', N'Soil vent pipework', N'item', 1.0000, 300.0000, 300.0000, N'', 45),
    (N'rv-cw-046', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'04', N'Drainagae & Civil', N'', N'', 0, N'MEC-DRN', N'Wastes to ditto', N'nr', 9.0000, 88.0000, 792.0000, N'', 46),
    (N'rv-cw-047', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'04', N'Drainagae & Civil', N'', N'', 0, N'SUB-EXC', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 2000.0000, 2000.0000, N'', 47),
    (N'rv-cw-048', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'04', N'Drainagae & Civil', N'', N'', 0, N'MEC-DRN', N'Provide drainage from new rainwater pipe, back inlet gulley & connection to existing drainage', N'item', 1.0000, 240.0000, 240.0000, N'', 48),
    (N'rv-cw-049', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'04', N'Drainagae & Civil', N'', N'', 0, N'SUB-DRN', N'New soakaway', N'nr', 1.0000, 1325.0000, 1325.0000, N'Omit item V02', 49),
    (N'rv-cw-050', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'04', N'Drainagae & Civil', N'', N'', 0, N'SUB-DRN', N'Public sewer confirmation', N'item', 1.0000, 200.0000, 200.0000, N'', 50),
    (N'rv-cw-051', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'05', N'Insulation & Screed', N'', N'', 0, N'SUB-CON', N'Concrete blinding under floor slab', N'm2', 23.0000, 32.0000, 736.0000, N'', 51),
    (N'rv-cw-052', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'05', N'Insulation & Screed', N'', N'', 0, N'SUB-CON', N'155 mm beam & block flooring', N'm2', 23.0000, 122.0000, 2806.0000, N'', 52),
    (N'rv-cw-053', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'05', N'Insulation & Screed', N'', N'', 0, N'WPF-DMP', N'Damp proof membranes 1200 g', N'm2', 23.0000, 16.0000, 368.0000, N'', 53),
    (N'rv-cw-054', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'05', N'Insulation & Screed', N'', N'', 0, N'INT-INF', N'100 mm Celotex floor insulation', N'm2', 23.0000, 40.0000, 920.0000, N'', 54),
    (N'rv-cw-055', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'05', N'Insulation & Screed', N'', N'', 0, N'CARP-1FX', N'Vapour control layer', N'm2', 23.0000, 12.0000, 276.0000, N'', 55),
    (N'rv-cw-056', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'05', N'Insulation & Screed', N'', N'', 0, N'INT-INF', N'75 mm sand / cement screed', N'm2', 23.0000, 60.0000, 1380.0000, N'', 56),
    (N'rv-cw-057', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'06', N'Masonry', N'', N'', 0, N'MASON-BRK', N'Wall extension profiles', N'm', 20.0000, 32.0000, 640.0000, N'', 57),
    (N'rv-cw-058', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'06', N'Masonry', N'', N'', 0, N'WPF-DMP', N'Cavity walls below dpc in facing brickwork, 100 mm blockwork & lean mix cavity fill', N'm2', 6.0000, 236.0000, 1416.0000, N'', 58),
    (N'rv-cw-059', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'06', N'Masonry', N'', N'', 0, N'WPF-DMP', N'Damp proof course', N'm', 14.0000, 16.0000, 224.0000, N'', 59),
    (N'rv-cw-060', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'06', N'Masonry', N'', N'', 0, N'INT-INW', N'Cavity wall in two skins of 100 mm blockwork with 100 mm Celotex insulation to cavity', N'm2', 32.0000, 192.0000, 6144.0000, N'', 60),
    (N'rv-cw-061', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'06', N'Masonry', N'', N'', 0, N'MASON-BRK', N'215 mm blockwork wall infills', N'm2', 3.0000, 126.0000, 378.0000, N'', 61),
    (N'rv-cw-062', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'06', N'Masonry', N'', N'', 0, N'MASON-BRK', N'BSD 100 lintel over opening', N'nr', 2.0000, 120.0000, 240.0000, N'', 62),
    (N'rv-cw-063', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'06', N'Masonry', N'', N'', 0, N'MASON-BRK', N'Catnic CG150/100 lintel', N'm', 6.0000, 175.0000, 1050.0000, N'', 63),
    (N'rv-cw-064', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'06', N'Masonry', N'', N'', 0, N'MASON-BRK', N'BSD 140 lintel over openings', N'nr', 1.0000, 267.0000, 267.0000, N'', 64),
    (N'rv-cw-065', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'06', N'Masonry', N'', N'', 0, N'MASON-BRK', N'Catnic CN71A', N'nr', 1.0000, 180.0000, 180.0000, N'', 65),
    (N'rv-cw-066', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'06', N'Masonry', N'', N'', 0, N'MASON-BRK', N'Infill existing masonry openings', N'm2', 2.0000, 192.0000, 384.0000, N'', 66),
    (N'rv-cw-067', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'07', N'Structural Steel', N'', N'', 0, N'STR-STL', N'152 x 152 x 23 kg steel columns', N'kg', 125.0000, 7.0000, 875.0000, N'', 67),
    (N'rv-cw-068', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'07', N'Structural Steel', N'', N'', 0, N'STR-STL', N'203 x 203 x 46 kg steel beam with 10 mm plate', N'kg', 320.0000, 7.0000, 2240.0000, N'', 68),
    (N'rv-cw-069', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'07', N'Structural Steel', N'', N'', 0, N'STR-STL', N'152 x 152 x 37 kg steel beam with 10 mm plate', N'kg', 180.0000, 7.0000, 1260.0000, N'', 69),
    (N'rv-cw-070', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'07', N'Structural Steel', N'', N'', 0, N'STR-STL', N'125 x 70 x 4 WP', N'kg', 35.0000, 7.0000, 245.0000, N'', 70),
    (N'rv-cw-071', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'07', N'Structural Steel', N'', N'', 0, N'SUB-CON', N'Cut out & case concrete padstones', N'nr', 2.0000, 122.0000, 244.0000, N'', 71),
    (N'rv-cw-072', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'07', N'Structural Steel', N'', N'', 0, N'STR-STL', N'Base plate & hold down brackets', N'nr', 2.0000, 65.0000, 130.0000, N'', 72),
    (N'rv-cw-073', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'08', N'Carpentry 1st Fix', N'', N'', 0, N'CARP-1FX', N'Timber wall plate bolted to wall', N'm', 23.0000, 34.0000, 782.0000, N'', 73),
    (N'rv-cw-074', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'08', N'Carpentry 1st Fix', N'', N'', 0, N'CARP-1FX', N'50 x 175 mm timber floor joists', N'm', 140.0000, 32.0000, 4480.0000, N'', 74),
    (N'rv-cw-075', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'08', N'Carpentry 1st Fix', N'', N'', 0, N'CARP-1FX', N'50 x 100 mm timber stud wall', N'm2', 32.0000, 70.0000, 2240.0000, N'', 75),
    (N'rv-cw-076', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'08', N'Carpentry 1st Fix', N'', N'', 0, N'CARP-1FX', N'18 mm plywood', N'm2', 94.0000, 32.0000, 3008.0000, N'', 76),
    (N'rv-cw-077', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'08', N'Carpentry 1st Fix', N'', N'', 0, N'CARP-1FX', N'18 mm T & G plywood to floor deck', N'm2', 12.0000, 32.0000, 384.0000, N'', 77),
    (N'rv-cw-078', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'08', N'Carpentry 1st Fix', N'', N'', 0, N'ENABLE-DEM', N'Remove floorboards & clean void', N'm2', 44.0000, 8.0000, 352.0000, N'', 78),
    (N'rv-cw-079', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'08', N'Carpentry 1st Fix', N'', N'', 0, N'CARP-1FX', N'Repair damaged joists', N'm', 5.0000, 40.0000, 200.0000, N'', 79),
    (N'rv-cw-080', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'08', N'Carpentry 1st Fix', N'', N'', 0, N'CARP-1FX', N'Woodworm treatment', N'm', 10.0000, 55.0000, 550.0000, N'', 80),
    (N'rv-cw-081', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'08', N'Carpentry 1st Fix', N'', N'', 0, N'CARP-1FX', N'Timber battening', N'm2', 44.0000, 32.0000, 1408.0000, N'', 81),
    (N'rv-cw-082', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'09', N'Roofing & Rainwater Goods', N'', N'', 0, N'SUB-CON', N'Breatherable membrane, battens & concrete plain tiles', N'm2', 28.0000, 94.0000, 2632.0000, N'', 82),
    (N'rv-cw-083', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'09', N'Roofing & Rainwater Goods', N'', N'', 0, N'ROOF-GRU', N'Separate cost for supply of tiles', N'm2', 28.0000, 38.0000, 1064.0000, N'', 83),
    (N'rv-cw-084', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'09', N'Roofing & Rainwater Goods', N'', N'', 0, N'ROOF-LED', N'Lead flashing / valley lining', N'm', 8.0000, 62.0000, 496.0000, N'', 84),
    (N'rv-cw-085', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'09', N'Roofing & Rainwater Goods', N'', N'', 0, N'ROOF-FSU', N'Fascia, soffit & barge boards', N'm', 15.0000, 48.0000, 720.0000, N'', 85),
    (N'rv-cw-086', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'09', N'Roofing & Rainwater Goods', N'', N'', 0, N'ROOF-GRU', N'Upvc guttering', N'm', 8.0000, 34.0000, 272.0000, N'', 86),
    (N'rv-cw-087', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'09', N'Roofing & Rainwater Goods', N'', N'', 0, N'ROOF-GRU', N'Upvc rainwater pipe', N'm', 6.0000, 36.0000, 216.0000, N'', 87),
    (N'rv-cw-088', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'09', N'Roofing & Rainwater Goods', N'', N'', 0, N'ROOF-GRU', N'Vapour barrier', N'm2', 84.0000, 10.0000, 840.0000, N'', 88),
    (N'rv-cw-089', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'09', N'Roofing & Rainwater Goods', N'', N'', 1, N'ROOF-GRU', N'Main roof remedial works', N'item', 1.0000, 1500.0000, 1500.0000, N'Provisional Sum', 89),
    (N'rv-cw-090', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'10', N'Insulation & Dry Lining', N'', N'', 0, N'INT-PLB', N'150 mm Celotex insulation on battens', N'm2', 12.0000, 54.0000, 648.0000, N'', 90),
    (N'rv-cw-091', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'10', N'Insulation & Dry Lining', N'', N'', 0, N'INT-PLB', N'120 mm Celotex insulation', N'm2', 72.0000, 46.0000, 3312.0000, N'', 91),
    (N'rv-cw-092', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'10', N'Insulation & Dry Lining', N'', N'', 0, N'CARP-CUT', N'37.5 mm Kingspan plasterboard to rafters', N'm2', 28.0000, 46.0000, 1288.0000, N'', 92),
    (N'rv-cw-093', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'10', N'Insulation & Dry Lining', N'', N'', 0, N'INT-INC', N'100 mm rockwool insulation between ceilings', N'm2', 44.0000, 36.0000, 1584.0000, N'', 93),
    (N'rv-cw-094', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'10', N'Insulation & Dry Lining', N'', N'', 0, N'INT-PLB', N'100 mm rockwool insulation between studs', N'm2', 32.0000, 36.0000, 1152.0000, N'', 94),
    (N'rv-cw-095', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'10', N'Insulation & Dry Lining', N'', N'', 0, N'INT-PLB', N'12.5mm acoustic plasterboard to walls', N'm2', 114.0000, 22.0000, 2508.0000, N'', 95),
    (N'rv-cw-096', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'10', N'Insulation & Dry Lining', N'', N'', 0, N'INT-PLB', N'12.5mm tile backer board', N'm2', 19.0000, 16.0000, 304.0000, N'', 96),
    (N'rv-cw-097', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'10', N'Insulation & Dry Lining', N'', N'', 0, N'INT-PLB', N'12.5mm acoustic plasterboard to ceilings', N'm2', 98.0000, 22.0000, 2156.0000, N'', 97),
    (N'rv-cw-098', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'10', N'Insulation & Dry Lining', N'', N'', 0, N'PRELIMS-PRO', N'Fireline protection to steels', N'item', 1.0000, 300.0000, 300.0000, N'', 98),
    (N'rv-cw-099', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'11', N'Windows & Doors', N'', N'', 0, N'WDR-SPG', N'Velux 980 x 1340 mm roof lights', N'nr', 2.0000, 1382.0000, 2764.0000, N'', 99),
    (N'rv-cw-100', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'11', N'Windows & Doors', N'', N'', 0, N'WDR-SPG', N'Electric blinds to roof lights', N'nr', 2.0000, 490.0000, 980.0000, N'', 100),
    (N'rv-cw-101', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'11', N'Windows & Doors', N'', N'', 0, N'WDR-SPG', N'Assistance to glazing supplier / installer', N'item', 1.0000, 250.0000, 250.0000, N'', 101),
    (N'rv-cw-102', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'12', N'Plastering', N'', N'', 0, N'INT-PLS', N'3mm skim to ceilings', N'm2', 72.0000, 20.0000, 1440.0000, N'', 102),
    (N'rv-cw-103', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'12', N'Plastering', N'', N'', 0, N'INT-PLS', N'3 mm skim to walls', N'm2', 102.0000, 20.0000, 2040.0000, N'', 103),
    (N'rv-cw-104', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'12', N'Plastering', N'', N'', 1, N'WPF-DMP', N'Damp proofing & re plaster - Provisional sum', N'item', 1.0000, 1000.0000, 1000.0000, N'Provisional Sum', 104),
    (N'rv-cw-105', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'13', N'Render', N'', N'', 0, N'INT-RDR', N'Three coat render to external walls', N'm2', 36.0000, 155.0000, 5580.0000, N'', 105),
    (N'rv-cw-106', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'13', N'Render', N'', N'', 0, N'INT-RDR', N'Make good existing render', N'item', 1.0000, 750.0000, 750.0000, N'', 106),
    (N'rv-cw-107', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'13', N'Render', N'', N'', 0, N'INT-RDR', N'Rendered plinth detail', N'm2', 10.0000, 122.0000, 1220.0000, N'', 107),
    (N'rv-cw-108', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'New consumer unit', N'nr', 1.0000, 1150.0000, 1150.0000, N'', 108),
    (N'rv-cw-109', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Double socket outlet', N'nr', 46.0000, 125.0000, 5750.0000, N'', 109),
    (N'rv-cw-110', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Single sockets', N'nr', 7.0000, 115.0000, 805.0000, N'', 110),
    (N'rv-cw-111', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Telephone point', N'nr', 1.0000, 110.0000, 110.0000, N'', 111),
    (N'rv-cw-112', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Fused spurs', N'nr', 5.0000, 110.0000, 550.0000, N'', 112),
    (N'rv-cw-113', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Shavers socket', N'nr', 2.0000, 110.0000, 220.0000, N'', 113),
    (N'rv-cw-114', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Cat 6', N'nr', 1.0000, 98.0000, 98.0000, N'', 114),
    (N'rv-cw-115', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'External sockets', N'nr', 2.0000, 135.0000, 270.0000, N'', 115),
    (N'rv-cw-116', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Pendant lights', N'nr', 11.0000, 95.0000, 1045.0000, N'', 116),
    (N'rv-cw-117', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Recessed lights', N'nr', 65.0000, 116.0000, 7540.0000, N'', 117),
    (N'rv-cw-118', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'LED strip lights', N'm', 35.0000, 80.0000, 2800.0000, N'', 118),
    (N'rv-cw-119', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Niche lights', N'nr', 2.0000, 105.0000, 210.0000, N'', 119),
    (N'rv-cw-120', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Wall lights', N'nr', 7.0000, 140.0000, 980.0000, N'', 120),
    (N'rv-cw-121', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Light switch', N'nr', 13.0000, 54.0000, 702.0000, N'', 121),
    (N'rv-cw-122', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'MEC-VNT', N'Extractor fans', N'nr', 3.0000, 275.0000, 825.0000, N'', 122),
    (N'rv-cw-123', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'INT-COV', N'For coving for LED strip lighting', N'm', 10.0000, 30.0000, 300.0000, N'', 123),
    (N'rv-cw-124', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Smoke / heat detector', N'nr', 3.0000, 155.0000, 465.0000, N'', 124),
    (N'rv-cw-125', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'CO2 detector', N'nr', 1.0000, 180.0000, 180.0000, N'', 125),
    (N'rv-cw-126', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Attendance on specialist subcontractors', N'item', 1.0000, 100.0000, 100.0000, N'', 126),
    (N'rv-cw-127', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Test on completion', N'item', 1.0000, 350.0000, 350.0000, N'', 127),
    (N'rv-cw-128', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'EXTW-LND', N'Armoured cable to rear garden with fuse board', N'item', 1.0000, 1500.0000, 1500.0000, N'', 128),
    (N'rv-cw-129', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Double socket outlet', N'nr', 3.0000, 125.0000, 375.0000, N'', 129),
    (N'rv-cw-130', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Light fittings', N'nr', 1.0000, 120.0000, 120.0000, N'', 130),
    (N'rv-cw-131', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'14', N'Electrics', N'', N'', 0, N'ELE-STD', N'Light switches', N'nr', 1.0000, 54.0000, 54.0000, N'', 131),
    (N'rv-cw-132', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'MEC-BLR', N'New boiler & associated pipework (spec tbc)', N'nr', 1.0000, 3975.0000, 3975.0000, N'', 132),
    (N'rv-cw-133', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'MEC-PLM', N'Megaflow hot water cylinder', N'nr', 1.0000, 2980.0000, 2980.0000, N'', 133),
    (N'rv-cw-134', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'MEC-PLM', N'Thermostat', N'item', 1.0000, 450.0000, 450.0000, N'', 134),
    (N'rv-cw-135', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'MEC-PLM', N'Fix only - radiators', N'nr', 11.0000, 225.0000, 2475.0000, N'', 135),
    (N'rv-cw-136', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'MEC-PLM', N'External taps', N'nr', 2.0000, 155.0000, 310.0000, N'', 136),
    (N'rv-cw-137', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'MEC-PLM', N'Test on completion', N'item', 1.0000, 350.0000, 350.0000, N'', 137),
    (N'rv-cw-138', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'SUP-SAN', N'Fix only - WC', N'nr', 1.0000, 330.0000, 330.0000, N'', 138),
    (N'rv-cw-139', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'SUP-SAN', N'Hot & cold supplies to sanitary fittings', N'nr', 5.0000, 178.0000, 890.0000, N'', 139),
    (N'rv-cw-140', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'SUP-SAN', N'Fix only - Wash hand basin', N'nr', 1.0000, 320.0000, 320.0000, N'', 140),
    (N'rv-cw-141', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'SUP-SAN', N'Fix only - Shower', N'nr', 1.0000, 505.0000, 505.0000, N'', 141),
    (N'rv-cw-142', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'MEC-PLM', N'Fix only - Accessories', N'nr', 1.0000, 100.0000, 100.0000, N'', 142),
    (N'rv-cw-143', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'SUP-SAN', N'Fix only - WC', N'nr', 1.0000, 330.0000, 330.0000, N'', 143),
    (N'rv-cw-144', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'SUP-SAN', N'Hot & cold supplies to sanitary fittings', N'nr', 5.0000, 178.0000, 890.0000, N'', 144),
    (N'rv-cw-145', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'SUP-SAN', N'Fix only - Wash hand basin', N'nr', 1.0000, 320.0000, 320.0000, N'', 145),
    (N'rv-cw-146', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'SUP-SAN', N'Fix only - Shower', N'nr', 1.0000, 505.0000, 505.0000, N'', 146),
    (N'rv-cw-147', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'15', N'Plumbing & Heating', N'', N'', 0, N'MEC-PLM', N'Fix only - Accessories', N'nr', 1.0000, 100.0000, 100.0000, N'', 147),
    (N'rv-cw-148', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'16', N'Carpentry 2nd Fix', N'', N'', 0, N'SUP-IRO', N'Internal door lining, double pocket door & ironmongery', N'nr', 1.0000, 1975.0000, 1975.0000, N'', 148),
    (N'rv-cw-149', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'16', N'Carpentry 2nd Fix', N'', N'', 0, N'SUP-IRO', N'Internal door lining, panel door & ironmongery (£150 supply)', N'nr', 8.0000, 455.0000, 3640.0000, N'', 149),
    (N'rv-cw-150', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'16', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'Oak door thresholds', N'nr', 6.0000, 125.0000, 750.0000, N'', 150),
    (N'rv-cw-151', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'16', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'MDF skirting to walls to new walls (£8/m supply)', N'm', 142.0000, 28.0000, 3976.0000, N'', 151),
    (N'rv-cw-152', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'16', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'MDF architraves to new door frames (£4/m supply)', N'm', 95.0000, 14.0000, 1330.0000, N'', 152),
    (N'rv-cw-153', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'16', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'MDF window boards', N'm', 5.0000, 36.0000, 180.0000, N'', 153),
    (N'rv-cw-154', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'16', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-1FX', N'Plywood boxing to internal pipework', N'item', 1.0000, 500.0000, 500.0000, N'', 154),
    (N'rv-cw-155', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'16', N'Carpentry 2nd Fix', N'', N'', 0, N'SUP-SAN', N'Removable access to shower rooms', N'item', 1.0000, 200.0000, 200.0000, N'', 155),
    (N'rv-cw-156', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'16', N'Carpentry 2nd Fix', N'', N'', 0, N'SUP-KIT', N'Fix only - Kitchen units & appliances', N'item', 1.0000, 2500.0000, 2500.0000, N'', 156),
    (N'rv-cw-157', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'16', N'Carpentry 2nd Fix', N'', N'', 0, N'SUP-KIT', N'Fix only - Kitchen worktop & splashback', N'item', 1.0000, 1500.0000, 1500.0000, N'', 157),
    (N'rv-cw-158', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'16', N'Carpentry 2nd Fix', N'', N'', 0, N'CARP-2FX', N'Fix only - Blinds', N'item', 1.0000, 200.0000, 200.0000, N'', 158),
    (N'rv-cw-159', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'17', N'Joinery', N'', N'', 1, N'CARP-JNR', N'Bespoke joinery - Provisional sum', N'item', 1.0000, 12000.0000, 12000.0000, N'Provisional Sum', 159),
    (N'rv-cw-160', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'18', N'Tiling', N'', N'', 0, N'TIL-STD', N'Fix only - Tiles', N'm2', 25.0000, 80.0000, 2000.0000, N'', 160),
    (N'rv-cw-161', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'18', N'Tiling', N'', N'', 0, N'TIL-STD', N'Fix only - Tiles', N'm2', 21.0000, 80.0000, 1680.0000, N'', 161),
    (N'rv-cw-162', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'19', N'Decoration', N'', N'', 0, N'DEC-STD', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 110.0000, 22.0000, 2420.0000, N'', 162),
    (N'rv-cw-163', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'19', N'Decoration', N'', N'', 0, N'DEC-STD', N'Ditto walls', N'm2', 248.0000, 20.0000, 4960.0000, N'', 163),
    (N'rv-cw-164', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'19', N'Decoration', N'', N'', 0, N'WDR-TIM', N'Prepare & decorate doors', N'm2', 32.0000, 32.0000, 1024.0000, N'', 164),
    (N'rv-cw-165', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'19', N'Decoration', N'', N'', 0, N'CARP-2FX', N'Frames, architrave, window board & skirtings', N'm', 220.0000, 8.0000, 1760.0000, N'', 165),
    (N'rv-cw-166', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'19', N'Decoration', N'', N'', 0, N'INT-RDR', N'Prepare & decorate all render', N'm2', 110.0000, 32.0000, 3520.0000, N'', 166),
    (N'rv-cw-167', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'19', N'Decoration', N'', N'', 0, N'HAND-MSC', N'Mastic finishing', N'item', 1.0000, 250.0000, 250.0000, N'', 167),
    (N'rv-cw-168', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'20', N'External Works', N'', N'', 0, N'EXTW-PAV', N'Sub base & fix only paving slabs', N'm2', 50.0000, 154.0000, 7700.0000, N'', 168),
    (N'rv-cw-169', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'20', N'External Works', N'', N'', 0, N'EXTW-FEN', N'Reinstate fencing on completion', N'item', 1.0000, 400.0000, 400.0000, N'', 169),
    (N'rv-cw-170', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'20', N'External Works', N'', N'', 0, N'EXTW-FEN', N'Supply & fit new fencing - area tbc', N'm', 15.0000, 180.0000, 2700.0000, N'', 170),
    (N'rv-cw-171', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'20', N'External Works', N'', N'', 0, N'SUB-CON', N'Concrete shed base', N'm2', 4.0000, 175.0000, 700.0000, N'Omit item V01', 171),
    (N'rv-cw-172', N'3bf6dcfa81764a248138fb5fd357aa84', 0, N'20', N'External Works', N'', N'', 0, N'EXTW-SHD', N'Fix only - Shed', N'item', 1.0000, 425.0000, 425.0000, N'Omit item V01', 172)
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
GO

-- Sanity check: the seeded block should reconcile to the workbook.
SELECT
    SUM(LineAmount) AS ContractSum   -- 261218.00
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = N'3bf6dcfa81764a248138fb5fd357aa84' AND ElementType = 0
  AND LineType NOT IN (3, 4);
GO
