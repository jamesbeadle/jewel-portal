-- ============================================================================
-- NOTE: CostCode values use the Jewel master cost codes (00001..00137) seeded
-- by seed-cost-centers.sql. Remapped from the original JBB-* buckets on
-- 2026-07-07 -- audit trail: scripts/cost-code-remap-review.csv.
-- Seed: By France -- initial contract values (Valuation Report line items)
-- ----------------------------------------------------------------------------
-- Project : By France, Leas Green, Chislehurst, BR7 6HD  (JBB-2026-001)
-- ProjectId: 3490f944b29545c4b8d5a04130f42ab8
--
-- Seeds the ORIGINAL contract scope only, taken from the By France valuation
-- bill (the priced Bill of Quantities, Provisional Sums, and the contingency
-- budget). These three blocks make up the original Contract Sum:
--
--     Contract works    GBP 1,432,573.00
--     Provisional Sums    GBP 297,882.00
--     Contingency          GBP 50,000.00
--     ----------------------------------
--     Contract Sum      GBP 1,780,455.00
--
-- Variation orders (V01..V73), agreed AFTER the contract, are NOT seeded here.
--
-- CostCode holds the Jewel cost-centre code (JBB-*, from the seeded CostCenters
-- master), mapped from each NRM2 bill section. The Valuation Report CODE column
-- shows this Jewel code. SectionCode/SectionName retain the NRM2 reference.
--
-- ElementType: 0=ContractWorks 1=PcSum 2=Contingency 3=Variation
-- LineType   : 0=Priced 1=ProvisionalSum 2=Omit 3=Declined 4=Tbc
--
-- Idempotent: keyed on stable ValuationLineItemId values (bf-cw-NNN / bf-pc-NN
-- / bf-cont-01). A re-run refreshes every field via MERGE. Variation lines for
-- this project are left untouched. Safe to run repeatedly.
-- ============================================================================

MERGE INTO [dbo].[ValuationLineItems] AS target
USING (VALUES
    (N'bf-cw-001', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'A10', N'Preliminaries', N'', N'', 0, N'00016', N'Site Supervision', N'week', 52.0000, 1250.0000, 65000.0000, N'', 1),
    (N'bf-cw-002', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'A10', N'Preliminaries', N'', N'', 0, N'00131', N'Rubbish removal', N'week', 26.0000, 380.0000, 9880.0000, N'', 2),
    (N'bf-cw-003', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'A10', N'Preliminaries', N'', N'', 0, N'00008', N'General protection', N'item', 1.0000, 600.0000, 600.0000, N'', 3),
    (N'bf-cw-004', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'A10', N'Preliminaries', N'', N'', 0, N'00007', N'Temporary toilet', N'week', 52.0000, 90.0000, 4680.0000, N'', 4),
    (N'bf-cw-005', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'A10', N'Preliminaries', N'', N'', 0, N'00006', N'CDM', N'item', 1.0000, 1200.0000, 1200.0000, N'', 5),
    (N'bf-cw-006', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'A10', N'Preliminaries', N'', N'', 0, N'00004', N'Health, safety & welfare', N'item', 52.0000, 125.0000, 6500.0000, N'', 6),
    (N'bf-cw-007', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'A10', N'Preliminaries', N'', N'', 0, N'00020', N'Scaffolding', N'item', 1.0000, 30940.0000, 30940.0000, N'', 7),
    (N'bf-cw-008', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'A10', N'Preliminaries', N'', N'', 0, N'00130', N'Clean on completion', N'item', 1.0000, 500.0000, 500.0000, N'', 8),
    (N'bf-cw-009', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Isolate electrical installation in connection with demolitions & strip out as required', N'item', 1.0000, 600.0000, 600.0000, N'', 9),
    (N'bf-cw-010', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Ditto plumbing & heating installation', N'item', 1.0000, 750.0000, 750.0000, N'', 10),
    (N'bf-cw-011', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Remove flooring, tiles, skirtings, picture rail, cove, fittings etc.', N'item', 1.0000, 500.0000, 500.0000, N'', 11),
    (N'bf-cw-012', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Remove kitchen & utility units, worktops & appliances', N'item', 1.0000, 240.0000, 240.0000, N'', 12),
    (N'bf-cw-013', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Remove sanitary items from WCs & bathrooms', N'item', 1.0000, 520.0000, 520.0000, N'', 13),
    (N'bf-cw-014', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Remove internal doors & frames', N'nr', 20.0000, 18.0000, 360.0000, N'', 14),
    (N'bf-cw-015', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Demolish internal single skin walls', N'item', 36.0000, 112.0000, 4032.0000, N'', 15),
    (N'bf-cw-016', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Remove existing plasterboard ceiling - throughout', N'm2', 260.0000, 12.0000, 3120.0000, N'', 16),
    (N'bf-cw-017', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Remove staircase', N'item', 1.0000, 324.0000, 324.0000, N'', 17),
    (N'bf-cw-018', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Remove external doors & windows', N'item', 1.0000, 520.0000, 520.0000, N'', 18),
    (N'bf-cw-019', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00019', N'Temporary propping & needling', N'item', 1.0000, 3500.0000, 3500.0000, N'', 19),
    (N'bf-cw-020', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Demolish external walls', N'item', 80.0000, 184.0000, 14720.0000, N'', 20),
    (N'bf-cw-021', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Demolish chimney breasts & stack', N'item', 1.0000, 2800.0000, 2800.0000, N'', 21),
    (N'bf-cw-022', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Remove existing floor build up throughout GF', N'item', 1.0000, 2200.0000, 2200.0000, N'', 22),
    (N'bf-cw-023', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Remove all roof construction & covering', N'item', 22.0000, 266.0000, 5852.0000, N'', 23),
    (N'bf-cw-024', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00024', N'Grub out redundnant drainage', N'item', 1.0000, 450.0000, 450.0000, N'', 24),
    (N'bf-cw-025', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Demolish bomb shelter & remove trees', N'item', 1.0000, 1000.0000, 1000.0000, N'', 25),
    (N'bf-cw-026', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C20', N'Demolition', N'', N'', 0, N'00018', N'Remove existing paving, shrubs, etc to areas of new work', N'item', 12.0000, 200.0000, 2400.0000, N'', 26),
    (N'bf-cw-027', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'D20', N'Excavation & filling', N'', N'', 0, N'00021', N'Trial pits', N'item', 1.0000, 750.0000, 750.0000, N'', 27),
    (N'bf-cw-028', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'D20', N'Excavation & filling', N'', N'', 0, N'00021', N'Excavate to reduce levels & remove spoil : to new areas', N'm3', 112.0000, 90.0000, 10080.0000, N'', 28),
    (N'bf-cw-029', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'D20', N'Excavation & filling', N'', N'', 0, N'00021', N'Excavate pad foundations & remove spoil', N'm3', 24.0000, 155.0000, 3720.0000, N'', 29),
    (N'bf-cw-030', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'D20', N'Excavation & filling', N'', N'', 0, N'00021', N'Excavate foundations 450 x 1000 mm & remove spoil', N'm3', 42.0000, 145.0000, 6090.0000, N'', 30),
    (N'bf-cw-031', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'D20', N'Excavation & filling', N'', N'', 0, N'00021', N'Excavate foundations 600 x 1000 mm & remove spoil', N'm3', 90.0000, 145.0000, 13050.0000, N'', 31),
    (N'bf-cw-032', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'P30', N'Trenches, pipeways for engineering services', N'', N'', 1, N'00121', N'All assosiated work - Provisional sum', N'item', 1.0000, 4500.0000, 4500.0000, N'', 32),
    (N'bf-cw-033', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'00024', N'Stub stack & durgo valve', N'nr', 5.0000, 130.0000, 650.0000, N'', 33),
    (N'bf-cw-034', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'00024', N'Soil vent pipework', N'item', 1.0000, 900.0000, 900.0000, N'', 34),
    (N'bf-cw-035', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'00024', N'Wastes connections', N'nr', 38.0000, 88.0000, 3344.0000, N'', 35),
    (N'bf-cw-036', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'00021', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 3750.0000, 3750.0000, N'', 36),
    (N'bf-cw-037', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'R11', N'Above ground foul drainage systems', N'', N'', 0, N'00024', N'New manhole', N'nr', 6.0000, 725.0000, 4350.0000, N'', 37),
    (N'bf-cw-038', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'00021', N'Excavate & lay new underground drainage runs', N'item', 1.0000, 3480.0000, 3480.0000, N'', 38),
    (N'bf-cw-039', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'00023', N'New soakaway', N'item', 2.0000, 1150.0000, 2300.0000, N'', 39),
    (N'bf-cw-040', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'R12', N'Below ground drainage systems', N'', N'', 0, N'00023', N'Make connection into existing runs', N'item', 1.0000, 500.0000, 500.0000, N'', 40),
    (N'bf-cw-041', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'D21', N'Ground gas collection & ventilated systems', N'', N'', 1, N'00022', N'Ground gas collection & ventilated systems - Provisional sum', N'item', 1.0000, 2500.0000, 2500.0000, N'', 41),
    (N'bf-cw-042', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'E10', N'In situ concrete', N'', N'', 0, N'00027', N'Concrete in strip & pad foundations', N'm3', 149.0000, 155.0000, 23095.0000, N'', 42),
    (N'bf-cw-043', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'E10', N'In situ concrete', N'', N'', 0, N'00027', N'150 mm hardcore blinded with sand', N'm2', 36.0000, 36.0000, 1296.0000, N'', 43),
    (N'bf-cw-044', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'E10', N'In situ concrete', N'', N'', 0, N'00027', N'150 mm bed of concrete to existing house & garage', N'm3', 36.0000, 285.0000, 10260.0000, N'', 44),
    (N'bf-cw-045', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'E10', N'In situ concrete', N'', N'', 0, N'00027', N'A252 mesh to concrete', N'm2', 36.0000, 28.0000, 1008.0000, N'', 45),
    (N'bf-cw-046', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'E60', N'Pre cast concrete floors', N'', N'', 0, N'00027', N'100 mm concrete oversite', N'm2', 322.0000, 32.0000, 10304.0000, N'', 46),
    (N'bf-cw-047', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'E60', N'Pre cast concrete floors', N'', N'', 0, N'00027', N'150 mm beam & block flooring', N'm2', 322.0000, 128.0000, 41216.0000, N'', 47),
    (N'bf-cw-048', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'C45', N'Damp proof course', N'', N'', 0, N'00081', N'Damp proof course', N'm', 138.0000, 16.0000, 2208.0000, N'', 48),
    (N'bf-cw-049', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F1', N'Masonry walling', N'', N'', 0, N'00081', N'Cavity walls below dpc in two skins of engineering brickwork & lean mix cavity fill', N'm2', 68.0000, 238.0000, 16184.0000, N'', 49),
    (N'bf-cw-050', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F1', N'Masonry walling', N'', N'', 0, N'00028', N'215 mm blockwork sleeper walls', N'm2', 32.0000, 106.0000, 3392.0000, N'', 50),
    (N'bf-cw-051', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F1', N'Masonry walling', N'', N'', 0, N'00057', N'Cavity wall in facing brickwork, 100 mm blockwork with 150 mm Celotex insulation to cavity', N'm2', 108.0000, 278.0000, 30024.0000, N'', 51),
    (N'bf-cw-052', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F1', N'Masonry walling', N'', N'', 0, N'00057', N'Cavity wall in two skins of 100 mm blockwork with 150 mm Celotex insulation to cavity', N'm2', 246.0000, 208.0000, 51168.0000, N'', 52),
    (N'bf-cw-053', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F1', N'Masonry walling', N'', N'', 0, N'00028', N'100 mm blockwork internal walls to GF', N'm2', 138.0000, 86.0000, 11868.0000, N'', 53),
    (N'bf-cw-054', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F1', N'Masonry walling', N'', N'', 0, N'00041', N'Timber dormer walls with plywood', N'm2', 6.0000, 102.0000, 612.0000, N'', 54),
    (N'bf-cw-055', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F30', N'Accessories', N'', N'', 0, N'00028', N'Alterations to existing openings & making good reveals', N'item', 1.0000, 900.0000, 900.0000, N'', 55),
    (N'bf-cw-056', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F30', N'Accessories', N'', N'', 0, N'00028', N'Wall extension profiles', N'm', 32.0000, 32.0000, 1024.0000, N'', 56),
    (N'bf-cw-057', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F30', N'Accessories', N'', N'', 0, N'00028', N'Thermabate cavity closers', N'm', 60.0000, 22.0000, 1320.0000, N'', 57),
    (N'bf-cw-058', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F30', N'Accessories', N'', N'', 0, N'00028', N'Catnic CG90/100 lintel & tray over new openings', N'm', 24.0000, 134.0000, 3216.0000, N'', 58),
    (N'bf-cw-059', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F30', N'Accessories', N'', N'', 0, N'00027', N'Precast concrete lintels over internal openings', N'm', 32.0000, 98.0000, 3136.0000, N'', 59),
    (N'bf-cw-060', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'F30', N'Accessories', N'', N'', 0, N'00028', N'Air vent bricks', N'item', 1.0000, 450.0000, 450.0000, N'', 60),
    (N'bf-cw-061', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G10', N'Structural steel framing', N'', N'', 0, N'00010', N'Structural steel beams & columns associated work', N'Tn', 18.9000, 7000.0000, 132300.0000, N'', 61),
    (N'bf-cw-062', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G10', N'Structural steel framing', N'', N'', 0, N'00010', N'D49 mesh wrapped around ground beam', N'item', 1.0000, 375.0000, 375.0000, N'', 62),
    (N'bf-cw-063', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G10', N'Structural steel framing', N'', N'', 0, N'00027', N'Cut out & cast concrete padstones', N'nr', 38.0000, 95.0000, 3610.0000, N'', 63),
    (N'bf-cw-064', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G10', N'Structural steel framing', N'', N'', 0, N'00010', N'Base plates & hold down brackets', N'nr', 22.0000, 60.0000, 1320.0000, N'', 64),
    (N'bf-cw-065', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G10', N'Structural steel framing', N'', N'', 0, N'00008', N'Fireline protection to steel beams', N'item', 1.0000, 3500.0000, 3500.0000, N'', 65),
    (N'bf-cw-066', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'00041', N'50 x 175 mm timber floor joists', N'm2', 256.0000, 88.0000, 22528.0000, N'', 66),
    (N'bf-cw-067', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'00039', N'50 x 150 mm timber roof rafters (to new pitched roof)', N'm2', 492.0000, 86.0000, 42312.0000, N'', 67),
    (N'bf-cw-068', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'00033', N'50 x 150 mm timber roof joists & plywood (to flat roof)', N'm2', 110.0000, 118.0000, 12980.0000, N'', 68),
    (N'bf-cw-069', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'00035', N'Plywood to fascia / soffit detail', N'm', 182.0000, 30.0000, 5460.0000, N'', 69),
    (N'bf-cw-070', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'00041', N'100 x 100 mm timber posts', N'm', 52.0000, 40.0000, 2080.0000, N'', 70),
    (N'bf-cw-071', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'00041', N'Joist hangers', N'nr', 720.0000, 7.0000, 5040.0000, N'', 71),
    (N'bf-cw-072', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'G20', N'Carpentry/timber framing/first fixing', N'', N'', 0, N'00041', N'Galvanised restaint straps', N'nr', 300.0000, 15.0000, 4500.0000, N'', 72),
    (N'bf-cw-073', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'K11', N'Rigid sheet flooring/sheathing/decking/sarking/linings/casings', N'', N'', 0, N'00041', N'12 mm plywood to dormers', N'm2', 6.0000, 24.0000, 144.0000, N'', 73),
    (N'bf-cw-074', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'K11', N'Rigid sheet flooring/sheathing/decking/sarking/linings/casings', N'', N'', 0, N'00041', N'18 mm T & G flooring to first / second floor', N'm2', 274.0000, 30.0000, 8220.0000, N'', 74),
    (N'bf-cw-075', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'P10', N'Sundry insulation', N'', N'', 0, N'00041', N'Plywood boxing & insulation to internal pipes', N'item', 1.0000, 800.0000, 800.0000, N'', 75),
    (N'bf-cw-076', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'P10', N'Sundry insulation', N'', N'', 0, N'00058', N'Mineral insulation to loft space', N'm2', 50.0000, 28.0000, 1400.0000, N'', 76),
    (N'bf-cw-077', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'H1', N'Roofing', N'', N'', 0, N'00027', N'Breatherable membrane, battens & concrete tiles', N'm2', 492.0000, 84.0000, 41328.0000, N'', 77),
    (N'bf-cw-078', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'H1', N'Roofing', N'', N'', 0, N'00039', N'100 mm Celotex insulation between rafters', N'm2', 302.0000, 40.0000, 12080.0000, N'', 78),
    (N'bf-cw-079', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'H1', N'Roofing', N'', N'', 0, N'00039', N'70 mm Celotex insulation over rafters', N'm2', 302.0000, 28.0000, 8456.0000, N'', 79),
    (N'bf-cw-080', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'H1', N'Roofing', N'', N'', 0, N'00030', N'Ridge / hip tiles', N'm', 48.0000, 55.0000, 2640.0000, N'', 80),
    (N'bf-cw-081', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'H1', N'Roofing', N'', N'', 0, N'00030', N'Separate cost for supply of tiles (60p each)', N'm2', 492.0000, 36.0000, 17712.0000, N'', 81),
    (N'bf-cw-082', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'H1', N'Roofing', N'', N'', 0, N'00030', N'Roof ventilators', N'nr', 25.0000, 85.0000, 2125.0000, N'', 82),
    (N'bf-cw-083', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'H1', N'Roofing', N'', N'', 0, N'00035', N'Fascia / soffit to new areas', N'm', 182.0000, 44.0000, 8008.0000, N'', 83),
    (N'bf-cw-084', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'H1', N'Roofing', N'', N'', 0, N'00034', N'Lead flashing / valley lining / soakers / aprons', N'item', 1.0000, 1200.0000, 1200.0000, N'', 84),
    (N'bf-cw-085', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'J42', N'Single layer polymeric sheet roof coverings', N'', N'', 0, N'00033', N'150 mm Celotex insulation to flat roof deck', N'm2', 110.0000, 48.0000, 5280.0000, N'', 85),
    (N'bf-cw-086', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'J42', N'Single layer polymeric sheet roof coverings', N'', N'', 0, N'00033', N'Sarnifli single ply membrane', N'm2', 110.0000, 144.0000, 15840.0000, N'', 86),
    (N'bf-cw-087', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'J42', N'Single layer polymeric sheet roof coverings', N'', N'', 0, N'00102', N'Canopys to rear extension', N'item', 1.0000, 5000.0000, 5000.0000, N'', 87),
    (N'bf-cw-088', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'J42', N'Single layer polymeric sheet roof coverings', N'', N'', 0, N'00033', N'Aluminium copping to parapet walls', N'm', 12.0000, 128.0000, 1536.0000, N'', 88),
    (N'bf-cw-089', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'R10', N'Rainwater drainage systems', N'', N'', 0, N'00024', N'New upvc guttering & rainwater pipework', N'm', 142.0000, 34.0000, 4828.0000, N'', 89),
    (N'bf-cw-090', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'R10', N'Rainwater drainage systems', N'', N'', 0, N'00024', N'Hopper heads', N'nr', 6.0000, 85.0000, 510.0000, N'', 90),
    (N'bf-cw-091', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'K1', N'Floors', N'', N'', 0, N'00059', N'100 mm Celotex insulation to new floor slab', N'm2', 322.0000, 40.0000, 12880.0000, N'', 91),
    (N'bf-cw-092', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'J40', N'Flexible sheet waterproofing', N'', N'', 0, N'00081', N'Damp proof membranes 1200 g', N'm2', 322.0000, 16.0000, 5152.0000, N'', 92),
    (N'bf-cw-093', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M10', N'Cement based levelling screeds', N'', N'', 0, N'00067', N'75 mm sand / cement floor screed', N'm2', 322.0000, 68.0000, 21896.0000, N'', 93),
    (N'bf-cw-094', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'00041', N'100 mm mineral insulation between floor joists', N'm2', 248.0000, 28.0000, 6944.0000, N'', 94),
    (N'bf-cw-095', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'00041', N'50 x 100 mm timber internal stud walls', N'm2', 146.0000, 62.0000, 9052.0000, N'', 95),
    (N'bf-cw-096', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'00041', N'50 mm rockwool insulation between stud walls', N'm2', 146.0000, 16.0000, 2336.0000, N'', 96),
    (N'bf-cw-097', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'00041', N'11 mm plywood to stud walls', N'm2', 80.0000, 18.0000, 1440.0000, N'', 97),
    (N'bf-cw-098', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'00060', N'12.5mm plasterboard to ceilings', N'm2', 532.0000, 20.0000, 10640.0000, N'', 98),
    (N'bf-cw-099', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'K10', N'Gypsum board dry linings/ partitions/ ceilings', N'', N'', 0, N'00060', N'12.5 mm plasterboard to studs', N'm2', 298.0000, 20.0000, 5960.0000, N'', 99),
    (N'bf-cw-100', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'00063', N'10 mm render to internal blockwork walls', N'm2', 617.0000, 20.0000, 12340.0000, N'', 100),
    (N'bf-cw-101', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'00063', N'3 mm skim to ceilings', N'm2', 532.0000, 20.0000, 10640.0000, N'', 101),
    (N'bf-cw-102', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M20', N'Plastered/ rendered/ roughcast coatings', N'', N'', 0, N'00063', N'3 mm skim to new walls', N'm2', 915.0000, 20.0000, 18300.0000, N'', 102),
    (N'bf-cw-103', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M21', N'Insulation with render finish', N'', N'', 0, N'00063', N'Webre multi coat render with insulation', N'm2', 165.0000, 135.0000, 22275.0000, N'', 103),
    (N'bf-cw-104', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'K20', N'Timber boarding', N'', N'', 0, N'00033', N'Breatherable membrane, battens & composite cladding', N'm2', 56.0000, 152.0000, 8512.0000, N'', 104),
    (N'bf-cw-105', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 1, N'00054', N'Aluminium windows - throughout', N'item', 1.0000, 35000.0000, 35000.0000, N'', 105),
    (N'bf-cw-106', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'L10', N'Windows/ rooflights/ screens/ louvres', N'', N'', 0, N'00054', N'Fix only - Window coverings', N'item', 1.0000, 2500.0000, 2500.0000, N'', 106),
    (N'bf-cw-107', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'L20', N'Doors', N'', N'', 1, N'00052', N'Aluminium external doors - throughout', N'item', 1.0000, 45000.0000, 45000.0000, N'', 107),
    (N'bf-cw-108', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'L20', N'Doors', N'', N'', 0, N'00123', N'926 mm Internal door lining & single door ( 200 supply )', N'nr', 26.0000, 455.0000, 11830.0000, N'', 108),
    (N'bf-cw-109', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'L20', N'Doors', N'', N'', 0, N'00123', N'926 mm Internal door lining & double door ( 450 supply )', N'nr', 2.0000, 820.0000, 1640.0000, N'', 109),
    (N'bf-cw-110', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'00042', N'MDF architraves to new door frames', N'm', 290.0000, 12.0000, 3480.0000, N'', 110),
    (N'bf-cw-111', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'00042', N'MDF skirting to walls to new walls (8/m supply)', N'm', 302.0000, 24.0000, 7248.0000, N'', 111),
    (N'bf-cw-112', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'P20', N'Unframed isolated trims/ skirtings/ sundry items', N'', N'', 0, N'00042', N'MDF window boards', N'm', 32.0000, 36.0000, 1152.0000, N'', 112),
    (N'bf-cw-113', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'S90', N'Hot and cold water supply systems', N'', N'', 0, N'00074', N'Hot & cold water supply', N'nr', 58.0000, 160.0000, 9280.0000, N'', 113),
    (N'bf-cw-114', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'S91', N'Test existing system', N'', N'', 0, N'00074', N'Test existing system', N'item', 1.0000, 1000.0000, 1000.0000, N'', 114),
    (N'bf-cw-115', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'T90', N'Heating systems', N'', N'', 0, N'00090', N'New central heating boiler & associated pipework', N'nr', 1.0000, 6540.0000, 6540.0000, N'', 115),
    (N'bf-cw-116', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'T90', N'Heating systems', N'', N'', 0, N'00074', N'Megaflo hot water cylinder', N'nr', 1.0000, 3480.0000, 3480.0000, N'', 116),
    (N'bf-cw-117', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'T90', N'Heating systems', N'', N'', 0, N'00093', N'Wet underfloor heating to ground floor', N'm2', 322.0000, 142.0000, 45724.0000, N'', 117),
    (N'bf-cw-118', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'T90', N'Heating systems', N'', N'', 0, N'00093', N'Manifolds / thermostats', N'item', 1.0000, 1200.0000, 1200.0000, N'', 118),
    (N'bf-cw-119', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'T90', N'Heating systems', N'', N'', 0, N'00074', N'Radiators with TRVs', N'nr', 18.0000, 195.0000, 3510.0000, N'', 119),
    (N'bf-cw-120', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'T90', N'Heating systems', N'', N'', 0, N'00074', N'Towel rails with TRVS', N'nr', 9.0000, 195.0000, 1755.0000, N'', 120),
    (N'bf-cw-121', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'U90', N'General ventilation', N'', N'', 0, N'00111', N'Extract fan', N'nr', 12.0000, 275.0000, 3300.0000, N'', 121),
    (N'bf-cw-122', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Consumer unit', N'nr', 1.0000, 1550.0000, 1550.0000, N'', 122),
    (N'bf-cw-123', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Double socket outlet', N'nr', 128.0000, 115.0000, 14720.0000, N'', 123),
    (N'bf-cw-124', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'External double socket', N'nr', 2.0000, 120.0000, 240.0000, N'', 124),
    (N'bf-cw-125', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Fused spurs', N'nr', 26.0000, 100.0000, 2600.0000, N'', 125),
    (N'bf-cw-126', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Cooker switch', N'nr', 2.0000, 120.0000, 240.0000, N'', 126),
    (N'bf-cw-127', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Shavers socket', N'nr', 7.0000, 95.0000, 665.0000, N'', 127),
    (N'bf-cw-128', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Recessed light fitting', N'nr', 204.0000, 108.0000, 22032.0000, N'', 128),
    (N'bf-cw-129', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'LED strip lighting', N'item', 1.0000, 3000.0000, 3000.0000, N'', 129),
    (N'bf-cw-130', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Pendant lights', N'nr', 3.0000, 78.0000, 234.0000, N'', 130),
    (N'bf-cw-131', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Wall lights', N'nr', 12.0000, 150.0000, 1800.0000, N'', 131),
    (N'bf-cw-132', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'External lights', N'nr', 20.0000, 175.0000, 3500.0000, N'', 132),
    (N'bf-cw-133', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Garage lights', N'nr', 6.0000, 298.0000, 1788.0000, N'', 133),
    (N'bf-cw-134', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Light switches', N'nr', 42.0000, 40.0000, 1680.0000, N'', 134),
    (N'bf-cw-135', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Smoke/heat detector', N'nr', 9.0000, 150.0000, 1350.0000, N'', 135),
    (N'bf-cw-136', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Carbon monoxide detector', N'nr', 2.0000, 118.0000, 236.0000, N'', 136),
    (N'bf-cw-137', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00087', N'Fix only - Intruder alarm', N'item', 1.0000, 900.0000, 900.0000, N'', 137),
    (N'bf-cw-138', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00099', N'Fix only - Hoist system', N'item', 1.0000, 1000.0000, 1000.0000, N'', 138),
    (N'bf-cw-139', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Builders work in connection with electrical installation', N'item', 1.0000, 500.0000, 500.0000, N'', 139),
    (N'bf-cw-140', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00072', N'Specialist fibre optic ceiling lights', N'item', 1.0000, 7500.0000, 7500.0000, N'', 140),
    (N'bf-cw-141', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'V90', N'Electrical systems', N'', N'', 0, N'00089', N'Air conditioning', N'nr', 1.0000, 20000.0000, 20000.0000, N'', 141),
    (N'bf-cw-142', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'W90', N'Communications & security systems', N'', N'', 0, N'00003', N'All associated works', N'nr', 1.0000, 5000.0000, 5000.0000, N'', 142),
    (N'bf-cw-143', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'P21', N'Door ironmongery', N'', N'', 0, N'00128', N'Fix only - Door Ironmongery', N'item', 1.0000, 3000.0000, 3000.0000, N'', 143),
    (N'bf-cw-144', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'L30', N'Stairs/ladders/walkways/handrails/balustrades', N'', N'', 0, N'00013', N'Fix only - Feature staircase', N'nr', 1.0000, 2500.0000, 2500.0000, N'', 144),
    (N'bf-cw-145', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'L30', N'Stairs/ladders/walkways/handrails/balustrades', N'', N'', 0, N'00013', N'New staircase from first to second - complete', N'nr', 1.0000, 4500.0000, 4500.0000, N'', 145),
    (N'bf-cw-146', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M40', N'Tiling', N'', N'', 0, N'00122', N'Fix only - Floor tiles to bathrooms', N'm2', 58.0000, 80.0000, 4640.0000, N'', 146),
    (N'bf-cw-147', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M40', N'Tiling', N'', N'', 0, N'00076', N'Fix only - Wall tiles to wet areas', N'm2', 60.0000, 80.0000, 4800.0000, N'', 147),
    (N'bf-cw-148', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M60', N'Painting', N'', N'', 0, N'00082', N'Mist & 2 coats of Dulux emulsion to ceilings', N'm2', 532.0000, 18.0000, 9576.0000, N'', 148),
    (N'bf-cw-149', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M60', N'Painting', N'', N'', 0, N'00082', N'Ditto walls', N'm2', 915.0000, 16.0000, 14640.0000, N'', 149),
    (N'bf-cw-150', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M60', N'Painting', N'', N'', 0, N'00050', N'Prepare & decorate doors', N'm2', 98.0000, 36.0000, 3528.0000, N'', 150),
    (N'bf-cw-151', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M60', N'Painting', N'', N'', 0, N'00042', N'Frames, architrave, window board & skirtings', N'm', 542.0000, 9.0000, 4878.0000, N'', 151),
    (N'bf-cw-152', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'Z10', N'Purpose made joinery', N'', N'', 0, N'00045', N'Fix only - Bespoke joinery', N'nr', 1.0000, 9000.0000, 9000.0000, N'', 152),
    (N'bf-cw-153', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M50', N'Rubber / cork / etc', N'', N'', 0, N'00070', N'Karndean flooring (50/m supply)', N'm2', 286.0000, 95.0000, 27170.0000, N'', 153),
    (N'bf-cw-154', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'M51', N'Carpet', N'', N'', 0, N'00069', N'Underlay & carpet (30/m supply)', N'm2', 248.0000, 52.0000, 12896.0000, N'', 154),
    (N'bf-cw-155', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'N10', N'General fixtures & fittings', N'', N'', 0, N'00082', N'Fix only - Decorative fire place', N'item', 1.0000, 1575.0000, 1575.0000, N'', 155),
    (N'bf-cw-156', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'Q10', N'Kerbs/ edgings/ channels/ paving accessories', N'', N'', 0, N'00113', N'Aco threshold drain', N'm', 24.0000, 145.0000, 3480.0000, N'', 156),
    (N'bf-cw-157', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'Q10', N'Kerbs/ edgings/ channels/ paving accessories', N'', N'', 0, N'00027', N'Concrete edging & widening to acess', N'item', 1.0000, 1500.0000, 1500.0000, N'', 157),
    (N'bf-cw-158', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'Q20', N'Granular sub-bases to roads/ pavings', N'', N'', 0, N'00027', N'Hardcore sub base to new patio area & front drive', N'm2', 448.0000, 28.0000, 12544.0000, N'', 158),
    (N'bf-cw-159', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'Q22', N'Asphalt roads', N'', N'', 0, N'00113', N'Tarmac to front drive', N'm2', 220.0000, 85.0000, 18700.0000, N'', 159),
    (N'bf-cw-160', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'Q25', N'Slab pavings to patios', N'', N'', 1, N'00113', N'600 x 600 mm stone paving slabs', N'm2', 228.0000, 145.0000, 33060.0000, N'', 160),
    (N'bf-cw-161', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'Q28', N'Topsoil', N'', N'', 1, N'00116', N'Topsoil for soft landscaping', N'item', 1.0000, 2000.0000, 2000.0000, N'', 161),
    (N'bf-cw-162', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'Q30', N'Seeding', N'', N'', 1, N'00116', N'Lawn seeding', N'item', 1.0000, 2500.0000, 2500.0000, N'', 162),
    (N'bf-cw-163', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'Q40', N'Fencing', N'', N'', 0, N'00115', N'New 6ft timber fencing', N'm', 80.0000, 120.0000, 9600.0000, N'', 163),
    (N'bf-cw-164', N'3490f944b29545c4b8d5a04130f42ab8', 0, N'Q50', N'Site/street furniture/equipment', N'', N'', 1, N'00115', N'Electric entrance gates', N'item', 1.0000, 15000.0000, 15000.0000, N'', 164),
    (N'bf-pc-01', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC01', N'Provisional Sums', N'', N'', 1, N'00076', N'Wall tiling (based on 212m2 @ 80.00/m2)', N'item', 1.0000, 18216.0000, 18216.0000, N'', 1),
    (N'bf-pc-02', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC02', N'Provisional Sums', N'', N'', 1, N'00076', N'Floor tiling (based on 37m2 @ 80.00/m2)', N'item', 1.0000, 3256.0000, 3256.0000, N'', 2),
    (N'bf-pc-03', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC03', N'Provisional Sums', N'', N'', 1, N'00128', N'Internal Door ironmongery', N'item', 1.0000, 2560.0000, 2560.0000, N'', 3),
    (N'bf-pc-04', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC04', N'Provisional Sums', N'', N'', 1, N'00103', N'Decorative Fireplaces for media walls', N'item', 1.0000, 7600.0000, 7600.0000, N'', 4),
    (N'bf-pc-05', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC05', N'Provisional Sums', N'', N'', 1, N'00122', N'Sanitary ware for Cloakroom', N'item', 1.0000, 1700.0000, 1700.0000, N'', 5),
    (N'bf-pc-06', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC06', N'Provisional Sums', N'', N'', 1, N'00122', N'Sanitary ware for Accessible wet room', N'item', 1.0000, 32900.0000, 32900.0000, N'', 6),
    (N'bf-pc-07', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC07', N'Provisional Sums', N'', N'', 1, N'00122', N'Sanitary ware for Guest and carers shower rooms', N'item', 1.0000, 6600.0000, 6600.0000, N'', 7),
    (N'bf-pc-08', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC08', N'Provisional Sums', N'', N'', 1, N'00125', N'Annex kitchen/Utility room', N'item', 1.0000, 6250.0000, 6250.0000, N'', 8),
    (N'bf-pc-09', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC09', N'Provisional Sums', N'', N'', 1, N'00122', N'Sanitary ware to first floor ensuite bathrooms', N'item', 1.0000, 12600.0000, 12600.0000, N'', 9),
    (N'bf-pc-10', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC10', N'Provisional Sums', N'', N'', 1, N'00122', N'Sanitary ware to first floor ensuite shower rooms', N'item', 1.0000, 11000.0000, 11000.0000, N'', 10),
    (N'bf-pc-11', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC11', N'Provisional Sums', N'', N'', 1, N'00048', N'Supply of feature staircase', N'item', 1.0000, 34500.0000, 34500.0000, N'', 11),
    (N'bf-pc-12', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC12', N'Provisional Sums', N'', N'', 1, N'00114', N'Removal of Asbestos', N'item', 1.0000, 5500.0000, 5500.0000, N'', 12),
    (N'bf-pc-13', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC13', N'Provisional Sums', N'', N'', 1, N'00023', N'Below ground drainage CCTV and remedial works', N'item', 1.0000, 5500.0000, 5500.0000, N'', 13),
    (N'bf-pc-14', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC14', N'Provisional Sums', N'', N'', 1, N'00050', N'Window coverings', N'item', 1.0000, 16500.0000, 16500.0000, N'', 14),
    (N'bf-pc-15', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC15', N'Provisional Sums', N'', N'', 1, N'00087', N'Intruder Alarm', N'item', 1.0000, 11000.0000, 11000.0000, N'', 15),
    (N'bf-pc-16', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC16', N'Provisional Sums', N'', N'', 1, N'00096', N'Fire and smoke alarm', N'item', 1.0000, 2750.0000, 2750.0000, N'', 16),
    (N'bf-pc-17', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC17', N'Provisional Sums', N'', N'', 1, N'00117', N'Soft landscaping (planting and beds etc)', N'item', 1.0000, 11000.0000, 11000.0000, N'', 17),
    (N'bf-pc-18', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC18', N'Provisional Sums', N'', N'', 1, N'00046', N'Dressing rooms, Wardrobes and fitted storage', N'item', 1.0000, 42000.0000, 42000.0000, N'', 18),
    (N'bf-pc-19', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC19', N'Provisional Sums', N'', N'', 1, N'00099', N'Ceiling track hoists', N'item', 1.0000, 22000.0000, 22000.0000, N'', 19),
    (N'bf-pc-20', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC20', N'Provisional Sums', N'', N'', 1, N'00102', N'Gazebo', N'item', 1.0000, 5000.0000, 5000.0000, N'', 20),
    (N'bf-pc-21', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC21', N'Provisional Sums', N'', N'', 1, N'00102', N'4 No Canopies', N'item', 1.0000, 17200.0000, 17200.0000, N'', 21),
    (N'bf-pc-22', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC22', N'Provisional Sums', N'', N'', 1, N'00117', N'Pond', N'item', 1.0000, 20000.0000, 20000.0000, N'', 22),
    (N'bf-pc-23', N'3490f944b29545c4b8d5a04130f42ab8', 1, N'PC23', N'Provisional Sums', N'', N'', 1, N'00072', N'Chandelier', N'item', 1.0000, 2250.0000, 2250.0000, N'', 23),
    (N'bf-cont-01', N'3490f944b29545c4b8d5a04130f42ab8', 2, N'', N'Contingency', N'', N'', 0, N'00134', N'Contingency Budget', N'item', 1.0000, 50000.0000, 50000.0000, N'', 1)
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

-- Sanity check: the three seeded blocks should reconcile to the workbook.
SELECT
    SUM(CASE WHEN ElementType = 0 THEN LineAmount ELSE 0 END) AS ContractWorks,  -- 1432573.00
    SUM(CASE WHEN ElementType = 1 THEN LineAmount ELSE 0 END) AS PcSums,         --  297882.00
    SUM(CASE WHEN ElementType = 2 THEN LineAmount ELSE 0 END) AS Contingency,    --   50000.00
    SUM(LineAmount) AS ContractSum                                               -- 1780455.00
FROM [dbo].[ValuationLineItems]
WHERE ProjectId = N'3490f944b29545c4b8d5a04130f42ab8' AND ElementType IN (0, 1, 2);
GO
